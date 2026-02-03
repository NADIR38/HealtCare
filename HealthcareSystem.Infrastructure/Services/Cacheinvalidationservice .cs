using HealthcareSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    /// <summary>
    /// Redis-based cache invalidation service with pattern matching support
    /// </summary>
    public class RedisCacheInvalidationService : ICacheInvalidationService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheInvalidationService> _logger;

        public RedisCacheInvalidationService(
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            ILogger<RedisCacheInvalidationService> logger)
        {
            _cache = cache;
            _redis = redis;
            _logger = logger;
        }

        public async Task InvalidateDoctorCachesAsync(Guid? doctorId = null)
        {
            try
            {
                if (doctorId.HasValue)
                {
                    // Invalidate specific doctor caches
                    await InvalidateCacheByPatternAsync($"*doctors/{doctorId}*");
                    await InvalidateCacheByPatternAsync($"*doctor_{doctorId}*");
                }

                // Invalidate list caches
                await InvalidateCacheByPatternAsync("*doctors?*");
                await InvalidateCacheByPatternAsync("*doctors/available*");

                _logger.LogInformation("Doctor caches invalidated for DoctorId: {DoctorId}", doctorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating doctor caches");
                // Don't throw - cache invalidation failures shouldn't break the application
            }
        }

        public async Task InvalidateScheduleCachesAsync(Guid doctorId)
        {
            try
            {
                await InvalidateCacheByPatternAsync($"*doctors/{doctorId}/schedules*");
                await InvalidateCacheByPatternAsync($"*doctors/{doctorId}/available-slots*");

                _logger.LogInformation("Schedule caches invalidated for DoctorId: {DoctorId}", doctorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating schedule caches");
            }
        }

        public async Task InvalidateAllScheduleCachesAsync()
        {
            try
            {
                await InvalidateCacheByPatternAsync("*/schedules*");
                await InvalidateCacheByPatternAsync("*/available-slots*");

                _logger.LogInformation("All schedule caches invalidated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating all schedule caches");
            }
        }

        public async Task InvalidateLeaveCachesAsync(Guid doctorId)
        {
            try
            {
                await InvalidateCacheByPatternAsync($"*doctors/{doctorId}/leaves*");
                await InvalidateCacheByPatternAsync("*leaves/pending*");
                await InvalidateCacheByPatternAsync("*leaves/my-leaves*");

                _logger.LogInformation("Leave caches invalidated for DoctorId: {DoctorId}", doctorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating leave caches");
            }
        }

        public async Task InvalidateCacheByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                var keys = server.Keys(pattern: pattern);

                foreach (var key in keys)
                {
                    await _cache.RemoveAsync(key.ToString());
                    _logger.LogDebug("Cache key removed: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache by pattern: {Pattern}", pattern);
            }
        }
    }

    /// <summary>
    /// In-memory cache invalidation service (for development/testing)
    /// Limited functionality - cannot do pattern matching
    /// </summary>
    public class InMemoryCacheInvalidationService : ICacheInvalidationService
    {
        private readonly ILogger<InMemoryCacheInvalidationService> _logger;

        public InMemoryCacheInvalidationService(ILogger<InMemoryCacheInvalidationService> logger)
        {
            _logger = logger;
        }

        public Task InvalidateDoctorCachesAsync(Guid? doctorId = null)
        {
            _logger.LogWarning("InMemory cache invalidation is limited. Consider using Redis for production.");
            // InMemory cache doesn't support pattern-based removal
            // This is a limitation - use Redis in production
            return Task.CompletedTask;
        }

        public Task InvalidateScheduleCachesAsync(Guid doctorId)
        {
            _logger.LogWarning("InMemory cache invalidation is limited. Consider using Redis for production.");
            return Task.CompletedTask;
        }

        public Task InvalidateAllScheduleCachesAsync()
        {
            _logger.LogWarning("InMemory cache invalidation is limited. Consider using Redis for production.");
            return Task.CompletedTask;
        }

        public Task InvalidateLeaveCachesAsync(Guid doctorId)
        {
            _logger.LogWarning("InMemory cache invalidation is limited. Consider using Redis for production.");
            return Task.CompletedTask;
        }

        public Task InvalidateCacheByPatternAsync(string pattern)
        {
            _logger.LogWarning("Pattern-based cache invalidation not supported in InMemory cache");
            return Task.CompletedTask;
        }
    }
}