﻿using System;
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    internal class CreateDequeueStatement
    {
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqlServerCommandStringCache _commandCache;

        private const string RpcdequeueKey = "dequeueCommandRpc";
        private const string DequeueKey = "dequeueCommand";

        public CreateDequeueStatement(ISqlServerMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper,
            SqlServerCommandStringCache commandCache)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => commandCache, commandCache);

            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _commandCache = commandCache;
        }

        /// <summary>
        /// Gets the de queue command.
        /// </summary>
        /// <param name="forRpc">if set to <c>true</c> [for RPC].</param>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        public string GetDeQueueCommand(bool forRpc, List<string> routes = null)
        {
            if (routes == null || routes.Count == 0)
            {
                if (forRpc && _commandCache.Contains(RpcdequeueKey))
                {
                    return _commandCache.Get(RpcdequeueKey).CommandText;
                }
                if (_commandCache.Contains(DequeueKey))
                {
                    return _commandCache.Get(DequeueKey).CommandText;
                }
            }

            var sb = new StringBuilder();

            //NOTE - this could be optimized a little bit. We are always using a CTE, but that's not necessary if the queue is 
            //setup as a pure FIFO queue.

            sb.AppendLine("declare @Queue1 table ");
            sb.AppendLine("( ");
            sb.AppendLine("QueueID bigint, ");
            sb.AppendLine("CorrelationID uniqueidentifier ");
            sb.AppendLine("); ");
            sb.AppendLine("with cte as ( ");
            sb.AppendLine("select top(1)  ");
            sb.AppendLine(_tableNameHelper.MetaDataName + ".QueueID, CorrelationID ");

            if (_options.Value.EnableStatus)
            {
                sb.Append(", [status] ");
            }
            if (_options.Value.EnableHeartBeat)
            {
                sb.Append(", HeartBeat ");
            }

            sb.AppendLine($"from {_tableNameHelper.MetaDataName} with (updlock, readpast, rowlock) ");

            //calculate where clause...
            var needWhere = true;
            if (_options.Value.EnableStatus && _options.Value.EnableDelayedProcessing)
            {
                sb.AppendFormat(" WHERE [Status] = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
                sb.AppendLine("and QueueProcessTime < getutcdate() ");
                needWhere = false;
            }
            else if (_options.Value.EnableStatus)
            {
                sb.AppendFormat("WHERE [Status] = {0}  ", Convert.ToInt16(QueueStatuses.Waiting));
                needWhere = false;
            }
            else if (_options.Value.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (QueueProcessTime < getutcdate()) ");
                needWhere = false;
            }

            if (_options.Value.EnableRoute && routes != null && routes.Count > 0)
            {
                if (needWhere)
                {
                    sb.AppendLine("where Route IN ( ");
                    needWhere = false;
                }
                else
                {
                    sb.AppendLine("AND Route IN ( ");
                }

                for (var i = 1; i - 1 < routes.Count; i++)
                {
                    sb.Append("@Route" + i);
                    if (i != routes.Count)
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(") ");
            }

            if (forRpc)
            {
                if (needWhere)
                {
                    sb.AppendLine("Where SourceQueueID = @QueueID ");
                    needWhere = false;
                }
                else
                {
                    sb.AppendLine("AND SourceQueueID = @QueueID ");
                }
            }


            if (_options.Value.EnableMessageExpiration || _options.Value.QueueType == QueueTypes.RpcReceive || _options.Value.QueueType == QueueTypes.RpcSend)
            {
                sb.AppendLine(needWhere
                    ? "where ExpirationTime > getutcdate() "
                    : "AND ExpirationTime > getutcdate() ");
            }

            //determine order by looking at the options
            var bNeedComma = false;
            sb.Append(" Order by ");
            if (_options.Value.EnableStatus)
            {
                sb.Append(" [status] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnablePriority)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.Append(" [priority] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" [QueueProcessTime] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnableMessageExpiration)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" [ExpirationTime] asc ");
                bNeedComma = true;
            }

            if (bNeedComma)
            {
                sb.Append(", ");
            }
            sb.AppendLine(" [QueueID] asc ) ");

            //determine if performing update or delete...
            if (_options.Value.EnableStatus && !_options.Value.EnableHoldTransactionUntilMessageCommitted)
            { //update

                sb.AppendFormat("update cte set status = {0} ", Convert.ToInt16(QueueStatuses.Processing));
                if (_options.Value.EnableHeartBeat)
                {
                    sb.AppendLine(", HeartBeat = GetUTCDate() ");
                }
                sb.AppendLine("output inserted.QueueID, inserted.CorrelationID into @Queue1 ");
            }
            else if (_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                sb.AppendLine("update cte set queueid = QueueID ");
                sb.AppendLine("output inserted.QueueID, inserted.CorrelationID into @Queue1 ");
            }
            else
            { //delete - note even if heartbeat is enabled, there is no point in setting it

                //a delete here if not using transactions will actually remove the record from the queue
                //it's up to the caller to handle error conditions in this case.
                sb.AppendLine("delete from cte ");
                sb.AppendLine("output deleted.QueueID, deleted.CorrelationID into @Queue1 ");
            }

            //grab the rest of the data - this is all standard
            sb.AppendLine("select q.queueid, qm.body, qm.Headers, q.CorrelationID from @Queue1 q ");
            sb.AppendLine($"INNER JOIN {_tableNameHelper.QueueName} qm with (nolock) "); //a dirty read on the data here should be ok, since we have exclusive access to the queue record on the meta data table
            sb.AppendLine("ON q.QueueID = qm.QueueID  ");

            //if we are holding transactions, we can't update the status table as part of this query - has to be done after de-queue instead
            if (_options.Value.EnableStatusTable && !_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                sb.AppendFormat("update {0} set status = {1} where {0}.QueueID = (select q.queueid from @Queue1 q)", _tableNameHelper.StatusName, Convert.ToInt16(QueueStatuses.Processing));
            }

            if (routes != null && routes.Count > 0)
            { //TODO - cache based on route
                return sb.ToString();
            }
            return _commandCache.Add(forRpc ? RpcdequeueKey : DequeueKey, sb.ToString());
        }
    }
}
