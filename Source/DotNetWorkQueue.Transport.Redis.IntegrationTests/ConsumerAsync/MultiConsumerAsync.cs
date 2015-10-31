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

using System.Threading.Tasks;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("Redis Consumer Tests")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 400, 10, 5, 5),
         InlineData(35, 5, 200, 10, 1, 2),
         InlineData(10, 8, 180, 7, 1, 1),
         InlineData(100, 0, 180, 10, 5, 0)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize)
        {
            var factory = SimpleConsumerAsync.CreateFactory(workerCount, queueSize);
            var task1 =
                Task.Factory.StartNew(
                    () =>
                        RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                            // ReSharper disable once AccessToDisposedClosure
                            queueSize, 1, factory));

            var task2 =
                Task.Factory.StartNew(
                    () =>
                        RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                            // ReSharper disable once AccessToDisposedClosure
                            queueSize, 2, factory));

            var task3 =
                Task.Factory.StartNew(
                    () =>
                        RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                            // ReSharper disable once AccessToDisposedClosure
                            queueSize, 3, factory));

            Task.WaitAll(task1, task2, task3);
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, ITaskFactory factory)
        {
            var queue = new SimpleConsumerAsync();
            queue.Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, factory);
        }
    }
}