﻿using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("PostgreSQL")]
    public class SimpleConsumerMethodAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(50, 5, 200, 10, 1, 2, false, 1, LinqMethodTypes.Compiled),
#if NETFULL
        InlineData(50, 5, 200, 10, 1, 2, false, 1, LinqMethodTypes.Dynamic),
         InlineData(10, 5, 180, 7, 1, 2, true, 1, LinqMethodTypes.Dynamic),
#endif
         InlineData(10, 5, 180, 7, 1, 2, true, 1, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, LinqMethodTypes linqMethodTypes)
        {
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize);
            }

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        var id = Guid.NewGuid();
                        if (messageType == 1)
                        {
                            var producer = new ProducerMethodAsyncShared();
                            producer.RunTestAsync<PostgreSqlMessageQueueInit>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope).Wait(timeOut);

                            var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)");
                        }
                        else if (messageType == 2)
                        {
                            var producer = new ProducerMethodAsyncShared();
                            producer.RunTestAsync<PostgreSqlMessageQueueInit>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope).Wait(timeOut);

                            var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)");
                        }
                        else if (messageType == 3)
                        {
                            var producer = new ProducerMethodAsyncShared();
                            producer.RunTestAsync<PostgreSqlMessageQueueInit>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope).Wait(timeOut);

                            var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)");
                        }

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            bool useTransactions, int messageType, ITaskFactory factory, LinqMethodTypes linqMethodTypes)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions, messageType, linqMethodTypes);
        }


        public static ITaskFactory CreateFactory(int maxThreads, int maxQueueSize)
        {
            var schedulerCreator = new SchedulerContainer();
            var taskScheduler = schedulerCreator.CreateTaskScheduler();

            taskScheduler.Configuration.MaximumThreads = maxThreads;
            taskScheduler.Configuration.MaxQueueSize = maxQueueSize;

            taskScheduler.Start();
            return schedulerCreator.CreateTaskFactory(taskScheduler);
        }
    }
}
