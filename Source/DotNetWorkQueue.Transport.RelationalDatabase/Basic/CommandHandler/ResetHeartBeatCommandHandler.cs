﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <summary>
    /// Resets the status for a specific record
    /// </summary>
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand, long>
    {
        private readonly ITransactionFactory _transactionFactory;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IPrepareCommandHandler<ResetHeartBeatCommand> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public ResetHeartBeatCommandHandler(ITransactionFactory transactionFactory,
            IDbConnectionFactory connectionFactory,
            IPrepareCommandHandler<ResetHeartBeatCommand> prepareCommand)
        {
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => connectionFactory, connectionFactory);
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _transactionFactory = transactionFactory;
            _connectionFactory = connectionFactory;
            _prepareCommand = prepareCommand;
        }
        /// <summary>
        /// Resets the status for a specific record, if the status is currently 1
        /// </summary>
        /// <param name="inputCommand">The input command.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public long Handle(ResetHeartBeatCommand inputCommand)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = trans;
                        _prepareCommand.Handle(inputCommand, command, CommandStringTypes.ResetHeartbeat);
                        var result = command.ExecuteNonQuery();
                        if (result > 0)
                        {
                            trans.Commit();
                        }
                        return result;
                    }
                }
            }
        }
    }
}
