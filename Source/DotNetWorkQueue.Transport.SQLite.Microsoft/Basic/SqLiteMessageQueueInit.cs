﻿using System;
using System.Reflection;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Decorator;
using DotNetWorkQueue.Transport.SQLite.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Decorator;
using Microsoft.Data.Sqlite;
using Polly;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <inheritdoc />
    public class SqLiteMessageQueueInit : SqLiteMessageQueueSharedInit
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            var myType = Assembly.GetAssembly(GetType());
            var baseType = Assembly.GetAssembly(typeof(IDbFactory));

            base.RegisterImplementations(container, registrationType, connection, queue, myType, baseType);

            container.Register<IDbDataSource, DbDataSource>(LifeStyles.Singleton);
            container.Register<IGetFileNameFromConnectionString, GetFileNameFromConnectionString>(LifeStyles.Singleton);
            container.Register<IDbFactory, DbFactory>(LifeStyles.Singleton);
            container.Register<IReaderAsync, ReaderAsync>(LifeStyles.Singleton);
            container.Register<DatabaseExists>(LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>),
                typeof(CreateJobTablesCommandDecorator), LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>),
                typeof(CreateQueueTablesAndSaveConfigurationDecorator), LifeStyles.Singleton);
        }

        /// <inheritdoc />
        protected override void SetupPolicy(IContainer container)
        {
            var policies = container.GetInstance<IPolicies>();
            var log = container.GetInstance<ILogFactory>().Create();

            var retrySql = Policy
                .Handle<SqliteException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.SqliteErrorCode))
                .WaitAndRetry(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                    });


            //BeginTransaction
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.BeginTransaction,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.BeginTransaction,
                    "A policy for retrying a BeginTransaction command. Sqlite will fail to start" +
                    "a transaction if another one is in progress. This behavior lets us wait a little" +
                    "bit and try agian"));
            policies.Registry[TransportPolicyDefinitions.BeginTransaction] = retrySql;
        }
    }
}
