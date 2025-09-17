using System.Data;
using Loudenvier.Utils.Dapper;
using Microsoft.Extensions.Caching.Memory;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Dapper;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/*public static class DapperCached
{
    public static TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(5);
    public static IEnumerable<T> GetFromCache<T>(CacheIdentity identity) 
        => MemoryCache.Default.Get(identity) as IEnumerable<T>;

    public static void AddToCache(CacheIdentity identity, object data, TimeSpan slidingExpiration) 
        => MemoryCache.Default.Set(identity, data, new CacheItemPolicy { SlidingExpiration = slidingExpiration });

    public static IEnumerable<T> QueryCached<T>(this IDbConnection conn, string sql, object param = null, bool skipCache = false, TimeSpan? slidingCacheExpiration = null) {
        CacheIdentity identity = null;
        if (!skipCache) {
            identity = new CacheIdentity(sql, param);
            var data = GetFromCache<T>(identity);
            if (data != null)
                return data;
        }
        var results = conn.Query<T>(sql, param);
        AddToCache(identity ?? new CacheIdentity(sql, param), results, slidingCacheExpiration ?? DefaultSlidingExpiration);
        return results;
    }
}*/

public static class DapperCached
{
    public static TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(5);

    static readonly Lazy<MemoryCache> lazyCache = new Lazy<MemoryCache>(() => new MemoryCache(new MemoryCacheOptions()));

    // Create a singleton MemoryCache instance
    private static MemoryCache Cache => lazyCache.Value;

    public static IEnumerable<T>? GetFromCache<T>(CacheIdentity identity) 
        => Cache.TryGetValue(identity, out IEnumerable<T>? cachedData) ? cachedData : null;

    public static void AddToCache(CacheIdentity identity, object data, TimeSpan slidingExpiration) {
        var options = new MemoryCacheEntryOptions {
            SlidingExpiration = slidingExpiration
        };
        Cache.Set(identity, data, options);
    }

    public static IEnumerable<T> QueryCached<T>(this IDbConnection conn, string sql, object? param = null, bool skipCache = false, TimeSpan? slidingCacheExpiration = null) {
        CacheIdentity? identity = null;

        if (!skipCache) {
            identity = new CacheIdentity(sql, param);
            var data = GetFromCache<T>(identity);
            if (data != null)
                return data;
        }

        var results = conn.Query<T>(sql, param);
        AddToCache(identity ?? new CacheIdentity(sql, param), results, slidingCacheExpiration ?? DefaultSlidingExpiration);
        return results;
    }
}