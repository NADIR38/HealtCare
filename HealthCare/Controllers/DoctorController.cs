using HealthcareSystem.API.Attributes;
using HealthcareSystem.Application.Dto.Doctor;
using HealthcareSystem.Application.DTOs.Doctor;
using HealthcareSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    /// <summary>
    /// Manages doctor profiles, schedules, and leave requests
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly ILogger<DoctorsController> _logger;
        private readonly ICacheInvalidationService _cacheInvalidation;

        public DoctorsController(
            IDoctorService doctorService,
            ILogger<DoctorsController> logger,
            ICacheInvalidationService cacheInvalidation)
        {
            _doctorService = doctorService;
            _logger = logger;
            _cacheInvalidation = cacheInvalidation;
        }

        #region Helper Methods

        /// <summary>
        /// Extracts the current user's ID from JWT claims
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("User ID not found in authentication token");
                throw new UnauthorizedAccessException("User ID not found in authentication token");
            }

            return userId;
        }

        #endregion

        #region Doctor Management

        /// <summary>
        /// Create a new doctor profile
        /// </summary>
        /// <param name="request">Doctor creation details</param>
        /// <returns>Created doctor profile</returns>
        /// <response code="201">Doctor created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="409">Doctor already exists for this user</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [RateLimit(PermitLimit = 10, Window = 60)]
        [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
        {
            _logger.LogInformation("Creating doctor for user {UserId}", request.UserId);

            var response = await _doctorService.CreateDoctorAsync(request);

            // Invalidate all doctor-related caches
            await _cacheInvalidation.InvalidateDoctorCachesAsync();

            _logger.LogInformation("Doctor created successfully with ID {DoctorId}", response.Id);

            return CreatedAtAction(nameof(GetDoctorById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Get doctor by ID
        /// </summary>
        /// <param name="id">Doctor ID</param>
        /// <returns>Doctor profile</returns>
        /// <response code="200">Doctor found</response>
        /// <response code="404">Doctor not found</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [RateLimit(PermitLimit = 100, Window = 60)]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "id" })]
        [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorById(Guid id)
        {
            _logger.LogDebug("Retrieving doctor {DoctorId}", id);

            var response = await _doctorService.GetDoctorByIdAsync(id);
            return Ok(response);
        }

        /// <summary>
        /// Get current logged-in doctor's profile
        /// </summary>
        /// <returns>Current doctor's profile</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Doctor profile not found</response>
        [HttpGet("me")]
        [Authorize(Roles = "Doctor")]
        [RateLimit(PermitLimit = 50, Window = 60)]
        [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("Retrieving profile for user {UserId}", userId);

            var response = await _doctorService.GetDoctorByUserIdAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// Get all doctors with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
        /// <param name="searchTerm">Search in name, doctor number, or specialization</param>
        /// <param name="specialization">Filter by specialization</param>
        /// <returns>Paginated list of doctors</returns>
        /// <response code="200">Doctors retrieved successfully</response>
        /// <response code="400">Invalid pagination parameters</response>
        [HttpGet]
        [AllowAnonymous]
        [RateLimit(PermitLimit = 100, Window = 60)]
        [ResponseCache(Duration = 180, VaryByQueryKeys = new[] { "page", "pageSize", "searchTerm", "specialization" })]
        [ProducesResponseType(typeof(PaginatedResponse<DoctorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllDoctors(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? specialization = null)
        {
            _logger.LogDebug("Retrieving doctors - Page: {Page}, PageSize: {PageSize}, Search: {Search}, Specialization: {Specialization}",
                page, pageSize, searchTerm, specialization);

            var response = await _doctorService.GetAllDoctorsAsync(page, pageSize, searchTerm, specialization);

            return Ok(new PaginatedResponse<DoctorResponse>
            {
                Data = response,
                Page = page,
                PageSize = pageSize,
                TotalItems = response.Count
            });
        }

        /// <summary>
        /// Get available doctors for booking
        /// </summary>
        /// <param name="specialization">Filter by specialization (optional)</param>
        /// <returns>List of available doctors</returns>
        /// <response code="200">Available doctors retrieved</response>
        [HttpGet("available")]
        [AllowAnonymous]
        [RateLimit(PermitLimit = 100, Window = 60)]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "specialization" })]
        [ProducesResponseType(typeof(List<DoctorResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableDoctors([FromQuery] string? specialization = null)
        {
            _logger.LogDebug("Retrieving available doctors - Specialization: {Specialization}", specialization);

            var response = await _doctorService.GetAvailableDoctorsAsync(specialization);
            return Ok(response);
        }

        /// <summary>
        /// Update doctor profile
        /// </summary>
        /// <param name="id">Doctor ID</param>
        /// <param name="request">Updated doctor details</param>
        /// <returns>Updated doctor profile</returns>
        /// <response code="200">Doctor updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Forbidden - Not authorized to update this doctor</response>
        /// <response code="404">Doctor not found</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "DoctorOwnerOrAdmin")]
        [RateLimit(PermitLimit = 20, Window = 60)]
        [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] UpdateDoctorRequest request)
        {
            _logger.LogInformation("Updating doctor {DoctorId}", id);

            var response = await _doctorService.UpdateDoctorAsync(id, request);

            // Invalidate caches
            await _cacheInvalidation.InvalidateDoctorCachesAsync(id);

            _logger.LogInformation("Doctor {DoctorId} updated successfully", id);

            return Ok(response);
        }

        /// <summary>
        /// Delete a doctor profile
        /// </summary>
        /// <param name="id">Doctor ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Doctor deleted successfully</response>
        /// <response code="400">Cannot delete doctor with active appointments</response>
        /// <response code="404">Doctor not found</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [RateLimit(PermitLimit = 10, Window = 60)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDoctor(Guid id)
        {
            _logger.LogWarning("Deleting doctor {DoctorId}", id);

            await _doctorService.DeleteDoctorAsync(id);

            // Invalidate caches
            await _cacheInvalidation.InvalidateDoctorCachesAsync(id);

            _logger.LogInformation("Doctor {DoctorId} deleted successfully", id);

            return NoContent();
        }

        #endregion

        #region Schedule Management

        /// <summary>
        /// Add schedule for a doctor
        /// </summary>
        /// <param name="doctorId">Doctor ID</param>
        /// <param name="request">Schedule details</param>
        /// <returns>Created schedule</returns>
        /// <response code="201">Schedule created successfully</response>
        /// <response code="400">Invalid schedule data</response>
        /// <response code="403">Forbidden - Not authorized to add schedule for this doctor</response>
        /// <response code="404">Doctor not found</response>
        /// <response code="409">Schedule conflicts with existing schedule</response>
        [HttpPost("{doctorId}/schedules")]
        [Authorize(Policy = "DoctorOwnerOrAdmin")]
        [RateLimit(PermitLimit = 20, Window = 60)]
        [ProducesResponseType(typeof(DoctorScheduleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddSchedule(Guid doctorId, [FromBody] DoctorScheduleRequest request)
        {
            _logger.LogInformation("Adding schedule for doctor {DoctorId}", doctorId);

            var response = await _doctorService.AddScheduleAsync(doctorId, request);

            // Invalidate schedule-related caches
            await _cacheInvalidation.InvalidateScheduleCachesAsync(doctorId);

            _logger.LogInformation("Schedule created for doctor {DoctorId}", doctorId);

            return CreatedAtAction(nameof(GetDoctorSchedules), new { doctorId }, response);
        }

        /// <summary>
        /// Get doctor's schedules
        /// </summary>
        /// <param name="doctorId">Doctor ID</param>
        /// <returns>List of schedules</returns>
        /// <response code="200">Schedules retrieved successfully</response>
        /// <response code="404">Doctor not found</response>
        [HttpGet("{doctorId}/schedules")]
        [AllowAnonymous]
        [RateLimit(PermitLimit = 100, Window = 60)]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "doctorId" })]
        [ProducesResponseType(typeof(List<DoctorScheduleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorSchedules(Guid doctorId)
        {
            _logger.LogDebug("Retrieving schedules for doctor {DoctorId}", doctorId);

            var response = await _doctorService.GetDoctorSchedulesAsync(doctorId);
            return Ok(response);
        }

        /// <summary>
        /// Delete a schedule
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Schedule deleted successfully</response>
        /// <response code="404">Schedule not found</response>
        [HttpDelete("schedules/{scheduleId}")]
        [Authorize(Policy = "DoctorOwnerOrAdmin")]
        [RateLimit(PermitLimit = 20, Window = 60)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
        {
            _logger.LogInformation("Deleting schedule {ScheduleId}", scheduleId);

            await _doctorService.DeleteScheduleAsync(scheduleId);

            // Invalidate all schedule caches (we don't know doctorId here)
            await _cacheInvalidation.InvalidateAllScheduleCachesAsync();

            _logger.LogInformation("Schedule {ScheduleId} deleted successfully", scheduleId);

            return NoContent();
        }

        /// <summary>
        /// Get available time slots for a doctor on a specific date
        /// </summary>
        /// <param name="doctorId">Doctor ID</param>
        /// <param name="date">Date to check availability</param>
        /// <returns>Available time slots</returns>
        /// <response code="200">Slots retrieved successfully</response>
        /// <response code="400">Invalid date</response>
        /// <response code="404">Doctor not found</response>
        [HttpGet("{doctorId}/available-slots")]
        [AllowAnonymous]
        [RateLimit(PermitLimit = 50, Window = 60)]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "doctorId", "date" })]
        [ProducesResponseType(typeof(AvailableSlotsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableSlots(Guid doctorId, [FromQuery] DateTime date)
        {
            _logger.LogDebug("Retrieving available slots for doctor {DoctorId} on {Date}", doctorId, date.Date);

            var slots = await _doctorService.GetAvailableSlotsAsync(doctorId, date);

            var response = new AvailableSlotsResponse
            {
                DoctorId = doctorId,
                Date = date.Date,
                DayOfWeek = date.DayOfWeek.ToString(),
                Slots = slots,
                TotalSlots = slots.Count,
                AvailableSlots = slots.Count(s => s.IsAvailable),
                BookedSlots = slots.Count(s => !s.IsAvailable)
            };

            return Ok(response);
        }

        #endregion

        #region Leave Management

        /// <summary>
        /// Request leave for a doctor
        /// </summary>
        /// <param name="request">Leave request details</param>
        /// <returns>Created leave request</returns>
        /// <response code="201">Leave request created successfully</response>
        /// <response code="400">Invalid leave request data</response>
        /// <response code="403">Forbidden - Not authorized to request leave for this doctor</response>
        /// <response code="404">Doctor not found</response>
        /// <response code="409">Leave overlaps with existing leave</response>
        [HttpPost("leaves")]
        [Authorize(Roles = "Doctor")]
        [RateLimit(PermitLimit = 10, Window = 60)]
        [ProducesResponseType(typeof(DoctorLeaveResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RequestLeave([FromBody] DoctorLeaveRequest request)
        {
            var userId = GetCurrentUserId();
            var doctor = await _doctorService.GetDoctorByUserIdAsync(userId);

            if (doctor.Id != request.DoctorId)
            {
                _logger.LogWarning("User {UserId} attempted to request leave for doctor {DoctorId}", userId, request.DoctorId);
                return Forbid();
            }

            _logger.LogInformation("Requesting leave for doctor {DoctorId} from {StartDate} to {EndDate}",
                request.DoctorId, request.StartDate, request.EndDate);

            var response = await _doctorService.RequestLeaveAsync(request);

            // Invalidate leave caches
            await _cacheInvalidation.InvalidateLeaveCachesAsync(request.DoctorId);

            _logger.LogInformation("Leave request created with ID {LeaveId}", response.Id);

            return CreatedAtAction(nameof(GetDoctorLeaves), new { doctorId = request.DoctorId }, response);
        }

        /// <summary>
        /// Approve a leave request
        /// </summary>
        /// <param name="leaveId">Leave request ID</param>
        /// <returns>Approved leave request</returns>
        /// <response code="200">Leave approved successfully</response>
        /// <response code="400">Leave cannot be approved (already processed)</response>
        /// <response code="404">Leave request not found</response>
        [HttpPut("leaves/{leaveId}/approve")]
        [Authorize(Roles = "Admin")]
        [RateLimit(PermitLimit = 30, Window = 60)]
        [ProducesResponseType(typeof(DoctorLeaveResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveLeave(Guid leaveId)
        {
            var approvedBy = GetCurrentUserId();

            _logger.LogInformation("Approving leave {LeaveId} by user {ApprovedBy}", leaveId, approvedBy);

            var response = await _doctorService.ApproveLeaveAsync(leaveId, approvedBy);

            // Invalidate caches
            await _cacheInvalidation.InvalidateLeaveCachesAsync(response.DoctorId);

            _logger.LogInformation("Leave {LeaveId} approved successfully", leaveId);

            return Ok(response);
        }

        /// <summary>
        /// Reject a leave request
        /// </summary>
        /// <param name="leaveId">Leave request ID</param>
        /// <returns>Rejected leave request</returns>
        /// <response code="200">Leave rejected successfully</response>
        /// <response code="400">Leave cannot be rejected (already processed)</response>
        /// <response code="404">Leave request not found</response>
        [HttpPut("leaves/{leaveId}/reject")]
        [Authorize(Roles = "Admin")]
        [RateLimit(PermitLimit = 30, Window = 60)]
        [ProducesResponseType(typeof(DoctorLeaveResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectLeave(Guid leaveId)
        {
            var rejectedBy = GetCurrentUserId();

            _logger.LogInformation("Rejecting leave {LeaveId} by user {RejectedBy}", leaveId, rejectedBy);

            var response = await _doctorService.RejectLeaveAsync(leaveId, rejectedBy);

            // Invalidate caches
            await _cacheInvalidation.InvalidateLeaveCachesAsync(response.DoctorId);

            _logger.LogInformation("Leave {LeaveId} rejected successfully", leaveId);

            return Ok(response);
        }

        /// <summary>
        /// Get leave requests for a specific doctor
        /// </summary>
        /// <param name="doctorId">Doctor ID</param>
        /// <returns>List of leave requests</returns>
        /// <response code="200">Leave requests retrieved successfully</response>
        /// <response code="403">Forbidden - Not authorized to view this doctor's leaves</response>
        /// <response code="404">Doctor not found</response>
        [HttpGet("{doctorId}/leaves")]
        [Authorize(Policy = "DoctorOwnerOrAdmin")]
        [RateLimit(PermitLimit = 50, Window = 60)]
        [ResponseCache(Duration = 180, VaryByQueryKeys = new[] { "doctorId" })]
        [ProducesResponseType(typeof(List<DoctorLeaveResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorLeaves(Guid doctorId)
        {
            _logger.LogDebug("Retrieving leaves for doctor {DoctorId}", doctorId);

            var response = await _doctorService.GetDoctorLeavesAsync(doctorId);
            return Ok(response);
        }

        /// <summary>
        /// Get current doctor's leave requests
        /// </summary>
        /// <returns>List of leave requests</returns>
        /// <response code="200">Leave requests retrieved successfully</response>
        /// <response code="404">Doctor profile not found</response>
        [HttpGet("leaves/my-leaves")]
        [Authorize(Roles = "Doctor")]
        [RateLimit(PermitLimit = 50, Window = 60)]
        [ProducesResponseType(typeof(List<DoctorLeaveResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //rabbit
        public async Task<IActionResult> GetMyLeaves()
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("Retrieving leaves for user {UserId}", userId);

            var doctor = await _doctorService.GetDoctorByUserIdAsync(userId);
            var response = await _doctorService.GetDoctorLeavesAsync(doctor.Id);

            return Ok(response);
        }

        /// <summary>
        /// Get all pending leave requests
        /// </summary>
        /// <returns>List of pending leave requests</returns>
        /// <response code="200">Pending leave requests retrieved successfully</response>
        [HttpGet("leaves/pending")]
        [Authorize(Roles = "Admin")]
        [RateLimit(PermitLimit = 100, Window = 60)]
        [ResponseCache(Duration = 60)]
        [ProducesResponseType(typeof(PendingLeavesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingLeaves()
        {
            _logger.LogDebug("Retrieving pending leave requests");

            var response = await _doctorService.GetPendingLeavesAsync();

            return Ok(new PendingLeavesResponse
            {
                PendingLeaves = response,
                Count = response.Count
            });
        }

        #endregion
    }

    #region Response Models

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }

    public class AvailableSlotsResponse
    {
        public Guid DoctorId { get; set; }
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; }
        public List<TimeSlotResponse> Slots { get; set; }
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int BookedSlots { get; set; }
    }

    public class PendingLeavesResponse
    {
        public List<DoctorLeaveResponse> PendingLeaves { get; set; }
        public int Count { get; set; }
    }

    #endregion
}