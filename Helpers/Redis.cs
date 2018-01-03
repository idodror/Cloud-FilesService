using Microsoft.Extensions.Options;
using System;
using StackExchange.Redis;

namespace FilesService.Helpers
{
    public interface IRedisConnectionFactory
    {
        ConnectionMultiplexer Connection();
    }

    public class RedisConnectionFactory : IRedisConnectionFactory
    {
        /// <summary>
        ///     The _connection.
        /// </summary>
        private readonly Lazy<ConnectionMultiplexer> _connection;
        
        private readonly IOptions<ConfigurationOptions> redis;

        public RedisConnectionFactory(IOptions<ConfigurationOptions> redis)
        {
            this._connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("sl-eu-lon-2-portal.7.dblayer.com:22610,password=TCZIXBZFJOVSHALB"));
        }

        public ConnectionMultiplexer Connection()
        {
            return this._connection.Value;
        }
    
    }
}