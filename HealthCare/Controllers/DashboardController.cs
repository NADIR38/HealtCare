using HealthcareSystem.Application.DTOs.Dashboard;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return userId;
        }

        /// <summary>
        /// Get admin dashboard with comprehensive statistics
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdminDashboard()
        {
            try
            {
                _logger.LogInformation("Retrieving admin dashboard");
                var dashboard = await _dashboardService.GetAdminDashboardAsync();
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard");
                return StatusCode(500, new { message = "Error retrieving dashboard data" });
            }
        }

        /// <summary>
        /// Get doctor dashboard with today's schedule and statistics
        /// </summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(DoctorDashboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorDashboard(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("Retrieving doctor dashboard for {DoctorId}", doctorId);
                var dashboard = await _dashboardService.GetDoctorDashboardAsync(doctorId);
                return Ok(dashboard);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor dashboard");
                return StatusCode(500, new { message = "Error retrieving dashboard data" });
            }
        }

        /// <summary>
        /// Get patient dashboard with appointments and health summary
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Patient,Admin")]
        [ProducesResponseType(typeof(PatientDashboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientDashboard(Guid patientId)
        {
            try
            {
                _logger.LogInformation("Retrieving patient dashboard for {PatientId}", patientId);
                var dashboard = await _dashboardService.GetPatientDashboardAsync(patientId);
                return Ok(dashboard);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient dashboard");
                return StatusCode(500, new { message = "Error retrieving dashboard data" });
            }
        }
    }
}