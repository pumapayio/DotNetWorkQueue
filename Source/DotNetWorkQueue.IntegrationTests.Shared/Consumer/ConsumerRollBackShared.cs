﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Metrics.Net;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerRollBackShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            int workerCount,
            ILogProvider logProvider,
            int timeOut,
            int runTime,
            long messageCount)
            where TTransportInit : ITransportInit, new()
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var processedCount = new IncrementWrapper();
                var haveIProcessedYouBefore = new ConcurrentDictionary<string, string>();
                var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics);
                using (
                    var queue =
                        creator.CreateConsumer(queueName,
                            connectionString))
                {
                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount);
                    var waitForFinish = new ManualResetEventSlim(false);
                    waitForFinish.Reset();

                    //start looking for work
                    queue.Start<TMessage>(((message, notifications) =>
                    {
                        MessageHandlingShared.HandleFakeMessagesRollback(message, runTime, processedCount,
                            messageCount,
                            waitForFinish, haveIProcessedYouBefore);
                    }));

                    waitForFinish.Wait(timeOut*1000);
                }
                Assert.Equal(messageCount, processedCount.ProcessedCount);
                VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 1, 0);
                LoggerShared.CheckForErrors(queueName);
            }
        }
    }
}