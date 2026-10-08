using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Pursuit.Infrastructure.Caching;
using StackExchange.Redis;

namespace Pursuit.IntegrationTests.Caching;

public class RedisCacheUnavailableTests
{
    [Fact]
    public async Task DisconnectedRedis_DoesNotDelayCacheFallback()
    {
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectRetry = 0,
            ConnectTimeout = 200,
            AsyncTimeout = 5000
        };
        options.EndPoints.Add("127.0.0.1", 1);
        using var connection = ConnectionMultiplexer.Connect(options);
        connection.IsConnected.Should().BeFalse();
        var cache = new RedisCacheService(connection, NullLogger<RedisCacheService>.Instance);

        var elapsed = Stopwatch.StartNew();
        var value = await cache.GetAsync<string>("missing");
        await cache.SetAsync("missing", "value", TimeSpan.FromMinutes(1));
        elapsed.Stop();

        value.Should().BeNull();
        elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
