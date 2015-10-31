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

using System;
using System.Threading;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Heartbeat notifications for queued / processing messages
    /// </summary>
    public class WorkerHeartBeatNotificationNoOp : IWorkerHeartBeatNotification, INoOperation
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public IHeartBeatStatus Status { get; set; }
        /// <summary>
        /// This token will be tripped if an exception occuring trying to update the heartbeat.
        /// </summary>
        /// <value>
        /// The exception has occured.
        /// </value>
        public CancellationToken ExceptionHasOccured { get; set; }
        /// <summary>
        /// Sets the error.
        /// </summary>
        /// <param name="error">The error.</param>
        public void SetError(Exception error)
        {
            
        }
        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error => null;

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        public long ErrorCount => 0;
    }
}