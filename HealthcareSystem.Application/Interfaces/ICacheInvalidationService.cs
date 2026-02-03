using System;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    /// <summary>
    /// Service for managing cache invalidation across the application
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// Invalidate all doctor-related caches
        /// </summary>
        Task InvalidateDoctorCachesAsync(Guid? doctorId = null);

        /// <summary>
        /// Invalidate schedule-related caches for a specific doctor
        /// </summary>
        Task InvalidateScheduleCachesAsync(Guid doctorId);

        /// <summary>
        /// Invalidate all schedule caches (when we don't know the doctor ID)
        /// </summary>
        Task InvalidateAllScheduleCachesAsync();

        /// <summary>
        /// Invalidate leave-related caches for a specific doctor
        /// </summary>
        Task InvalidateLeaveCachesAsync(Guid doctorId);

        /// <summary>
        /// Invalidate specific cache by pattern
        /// </summary>
        Task InvalidateCacheByPatternAsync(string pattern);
    }
}