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
namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Defines additional context information for an RPC message
    /// </summary>
    public class RpcContext : IRpcContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcContext"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The time out.</param>
        public RpcContext(IMessageId messageId, TimeSpan? timeOut)
        {
            Guard.NotNull(() => messageId, messageId);
            MessageId = messageId;
            Timeout = timeOut;
        }
        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public IMessageId MessageId { get; }
        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public TimeSpan? Timeout { get; }
    }
}