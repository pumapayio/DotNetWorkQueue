﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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

using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Dequeues the next record for a Rpc
    /// </summary>
    internal class DequeueRpcLua: BaseLua
    {
        public DequeueRpcLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script= @"local count = redis.call('LREM', @pendingkey, 1, @uuid) 
                    if (count==0) then 
                        return nil;
                    end                   
                    local expireScore = redis.call('zscore', @expirekey, @uuid)
                    redis.call('zadd', @workingkey, @timestamp, @uuid) 
                    local message = redis.call('hget', @valueskey, @uuid) 
                    redis.call('hset', @StatusKey, @uuid, '1') 
                    local headers = redis.call('hget', @headerskey, @uuid)
                    return {@uuid, message, headers, expireScore}";
        }
        /// <summary>
        /// Dequeues the next record for a Rpc.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        public RedisValue[] Execute(string messageid, long unixTime, List<string> routes)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            if (routes == null || routes.Count == 0)
                return (RedisValue[])db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageid, unixTime, null));

            foreach (var route in routes)
            {
                var result = db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageid, unixTime, route));
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
            }
            return null;
        }
        /// <summary>
        /// Dequeues the next record for a Rpc.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        public async Task<RedisValue[]> ExecuteAsync(string messageid, long unixTime, List<string> routes)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            if (routes == null || routes.Count == 0)
            {
                var result =
                    await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageid, unixTime, null))
                        .ConfigureAwait(false);
                return (RedisValue[]) result;
            }

            foreach (var route in routes)
            {
                var result =
                    await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageid, unixTime, route)).ConfigureAwait(false);
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
            }
            return null;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(string messageid, long unixTime, string route)
        {
            var pendingKey = !string.IsNullOrEmpty(route) ? RedisNames.PendingRoute(route) : RedisNames.Pending;
            return new
            {
                pendingkey = (RedisKey)pendingKey,
                workingkey = (RedisKey)RedisNames.Working,
                timestamp = unixTime,
                headerskey = (RedisKey)RedisNames.Headers,
                valueskey = (RedisKey)RedisNames.Values,
                expirekey = (RedisKey)RedisNames.Expiration,
                StatusKey = (RedisKey)RedisNames.Status,
                uuid = messageid
            };
        }
    }
}
