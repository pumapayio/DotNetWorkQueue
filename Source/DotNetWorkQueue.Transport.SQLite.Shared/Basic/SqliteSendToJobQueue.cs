﻿using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Sends a job to a SQLite db.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ASendJobToQueue" />
    public class SqliteSendToJobQueue: ASendJobToQueue
    {
        private readonly IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> _doesJobExist;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;
        private readonly IQueryHandler<GetJobIdQuery, long> _getJobId;
        private readonly CreateJobMetaData _createJobMetaData;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteSendToJobQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="createJobMetaData">The create job meta data.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SqliteSendToJobQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> doesJobExist,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand,
            IQueryHandler<GetJobIdQuery, long> getJobId, CreateJobMetaData createJobMetaData,
            IGetTimeFactory getTimeFactory): base(queue, getTimeFactory)
        {
            _doesJobExist = doesJobExist;
            _deleteMessageCommand = deleteMessageCommand;
            _getJobId = getJobId;
            _createJobMetaData = createJobMetaData;
        }

        /// <summary>
        /// Returns the status of the job based on name and scheduled time.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        protected override QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime)
        {
           return _doesJobExist.Handle(new DoesJobExistQuery<IDbConnection, IDbTransaction>(name, scheduledTime));
        }

        /// <summary>
        /// Deletes the job based on the job name.
        /// </summary>
        /// <param name="name">The name.</param>
        protected override void DeleteJob(string name)
        {
            _deleteMessageCommand.Handle(new DeleteMessageCommand(_getJobId.Handle(new GetJobIdQuery(name))));
        }

        /// <summary>
        /// Return true if the exception indicates that the job already exists.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns></returns>
        /// <remarks>
        /// Used to determine if we should return specific error messages
        /// </remarks>
        protected override bool JobAlreadyExistsError(Exception error)
        {
            var message = error.Message.Replace(Environment.NewLine, " ");
            return message.Contains("constraint failed UNIQUE constraint failed:") && message.Contains("JobName") ||
                    message.Contains("Failed to insert record - the job has already been queued or processed");
        }

        /// <summary>
        /// Sets the specified meta data on the messageData context
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <param name="messageData">The message data.</param>
        protected override void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime, string route,
            IAdditionalMessageData messageData)
        {
            _createJobMetaData.Create(jobName, scheduledTime, eventTime,  messageData, route);
        }
    }
}
