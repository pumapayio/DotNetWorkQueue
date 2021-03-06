﻿using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.Factory
{
    /// <summary>
    /// Creates new instance of the options classes
    /// </summary>
    internal class SqLiteMessageQueueTransportOptionsFactory : ISqLiteMessageQueueTransportOptionsFactory
    {
        private readonly IQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>, SqLiteMessageQueueTransportOptions> _queryOptions;
        private readonly IConnectionInformation _connectionInformation;
        private readonly object _creator = new object();
        private SqLiteMessageQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueTransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="queryOptions">The query options.</param>
        public SqLiteMessageQueueTransportOptionsFactory(IConnectionInformation connectionInformation,
            IQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>, SqLiteMessageQueueTransportOptions> queryOptions)
        {
            Guard.NotNull(() => queryOptions, queryOptions);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _queryOptions = queryOptions;
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <returns></returns>
        public SqLiteMessageQueueTransportOptions Create()
        {
            if (string.IsNullOrEmpty(_connectionInformation.ConnectionString))
            {
                return new SqLiteMessageQueueTransportOptions();
            }

            if (_options != null) return _options;
            lock (_creator)
            {
                if (_options == null)
                {
                    _options = _queryOptions.Handle(new GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>());
                }
                if (_options == null) //does not exist in DB; return a new copy
                {
                    _options = new SqLiteMessageQueueTransportOptions();
                }
            }
            return _options;
        }
    }
}
