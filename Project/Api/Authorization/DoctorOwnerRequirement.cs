using HealthcareSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Authorization
{
    /// <summary>
    /// Authorization requirement for doctor owner access
    /// </summary>
    public class DoctorOwnerRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Handles authorization for doctor owner or admin access
    /// Allows admins full access, or doctors to access only their own resources
    /// </summary>
    public class DoctorOwnerHandler : AuthorizationHandler<DoctorOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDoctorService _doctorService;
        private readonly ILogger<DoctorOwnerHandler> _logger;

        public DoctorOwnerHandler(
            IHttpContextAccessor httpContextAccessor,
            IDoctorService doctorService,
            ILogger<DoctorOwnerHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _doctorService = doctorService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DoctorOwnerRequirement requirement)
        {
            // Allow admins full access
            if (context.User.IsInRole("Admin"))
            {
                _logger.LogDebug("Admin access granted");
                context.Succeed(requirement);
                return;
            }

            // Check if user is a doctor
            if (!context.User.IsInRole("Doctor"))
            {
                _logger.LogWarning("User is not a doctor or admin");
                context.Fail();
                return;
            }

            // Get the doctor ID from route
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogError("HttpContext is null");
                context.Fail();
                return;
            }

            var routeData = httpContext.GetRouteData();
            if (!routeData.Values.TryGetValue("doctorId", out var doctorIdValue) &&
                !routeData.Values.TryGetValue("id", out doctorIdValue))
            {
                _logger.LogWarning("Doctor ID not found in route");
                context.Fail();
                return;
            }

            if (!Guid.TryParse(doctorIdValue?.ToString(), out var doctorId))
            {
                _logger.LogWarning("Invalid doctor ID format: {DoctorId}", doctorIdValue);
                context.Fail();
                return;
            }

            // Get current user ID
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("User ID not found in claims");
                context.Fail();
                return;
            }

            try
            {
                // Check if the doctor belongs to the current user
                var doctor = await _doctorService.GetDoctorByIdAsync(doctorId);

                if (doctor.UserId == userId)
                {
                    _logger.LogDebug("Doctor owner access granted for DoctorId: {DoctorId}, UserId: {UserId}",
                        doctorId, userId);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User {UserId} attempted to access doctor {DoctorId} without permission",
                        userId, doctorId);
                    context.Fail();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking doctor ownership for DoctorId: {DoctorId}", doctorId);
                context.Fail();
            }
        }
    }
}