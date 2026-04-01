using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using QueryNest.BLL.Interfaces;

namespace QueryNest.BLL.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(cached, JsonOptions);
                if (value is not null)
                {
                    return value;
                }
            }
            catch
            {
            }
        }

        var created = await factory(cancellationToken);
        var payload = JsonSerializer.Serialize(created, JsonOptions);
        await _cache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        return created;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _cache.RemoveAsync(key, cancellationToken);
    }
}
