﻿using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Rpc;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.rpc
{
    [Collection("SQLite")]
    public class SimpleRpc
    {
        [Theory]
        [InlineData(50, 1, 200, 3, false, true),
         InlineData(50, 1, 200, 3, true, true),
         InlineData(30, 0, 240, 3, false, false),
         InlineData(30, 0, 240, 3, true, false)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool async, bool inMemoryDb)
        {
            var queueNameSend = GenerateQueueName.Create();
            var queueNameReceive = GenerateQueueName.Create();
            var logProviderSend = LoggerShared.Create(queueNameSend, GetType().Name);
            var logProviderReceive = LoggerShared.Create(queueNameReceive, GetType().Name);

            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                using (var queueCreatorReceive =
                new QueueCreationContainer<SqLiteMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProviderReceive, LifeStyles.Singleton)))
                {
                    using (var queueCreatorSend =
                        new QueueCreationContainer<SqLiteMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProviderSend, LifeStyles.Singleton)))
                    {
                        try
                        {

                            using (
                                var oCreationReceive =
                                    queueCreatorReceive.GetQueueCreation<SqLiteMessageQueueCreation>(queueNameReceive,
                                        connectionInfo.ConnectionString)
                                )
                            {

                                oCreationReceive.Options.EnableDelayedProcessing = true;
                                oCreationReceive.Options.EnableHeartBeat = true;
                                oCreationReceive.Options.EnableStatus = true;
                                oCreationReceive.Options.EnableStatusTable = true;
                                oCreationReceive.Options.QueueType = QueueTypes.RpcReceive;

                                var resultReceive = oCreationReceive.CreateQueue();
                                Assert.True(resultReceive.Success, resultReceive.ErrorMessage);

                                using (
                                    var oCreation =
                                        queueCreatorSend.GetQueueCreation<SqLiteMessageQueueCreation>(queueNameSend,
                                            connectionInfo.ConnectionString)
                                    )
                                {
                                    oCreation.Options.EnableDelayedProcessing = true;
                                    oCreation.Options.EnableHeartBeat = true;
                                    oCreation.Options.EnableStatus = true;
                                    oCreation.Options.EnableStatusTable = true;
                                    oCreation.Options.QueueType = QueueTypes.RpcSend;

                                    var result = oCreation.CreateQueue();
                                    Assert.True(result.Success, result.ErrorMessage);

                                    var rpc =
                                        new RpcShared
                                            <SqLiteMessageQueueInit, FakeResponse, FakeMessage, SqLiteRpcConnection>();

                                    rpc.Run(queueNameReceive, queueNameSend, connectionInfo.ConnectionString,
                                        connectionInfo.ConnectionString, logProviderReceive, logProviderSend,
                                        runtime, messageCount, workerCount, timeOut, async,
                                        new SqLiteRpcConnection(connectionInfo.ConnectionString, queueNameSend,
                                            connectionInfo.ConnectionString, queueNameReceive, new DbDataSource()),
                                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)");

                                    new VerifyQueueRecordCount(queueNameSend, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                                    new VerifyQueueRecordCount(queueNameReceive, connectionInfo.ConnectionString, oCreationReceive.Options).Verify(0, false,
                                        false);
                                }
                            }
                        }
                        finally
                        {
                            using (
                                var oCreation =
                                    queueCreatorSend.GetQueueCreation<SqLiteMessageQueueCreation>(queueNameSend,
                                        connectionInfo.ConnectionString)
                                )
                            {
                                oCreation.RemoveQueue();
                            }

                            using (
                                var oCreation =
                                    queueCreatorReceive.GetQueueCreation<SqLiteMessageQueueCreation>(queueNameReceive,
                                        connectionInfo.ConnectionString)
                                )
                            {
                                oCreation.RemoveQueue();
                            }
                        }
                    }
                }
            }
        }
    }
}
