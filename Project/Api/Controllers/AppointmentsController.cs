using HealthcareSystem.Application.Dto.Appointments;
using HealthcareSystem.Application.DTOs.Appointment;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
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

        [HttpPost]
        [Authorize(Roles = "Patient,Doctor,Nurse,Receptionist,Admin")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = ModelState });
            }

            try
            {
                var createdBy = GetCurrentUserId();
                var response = await _appointmentService.CreateAppointmentAsync(request, createdBy);

                _logger.LogInformation("Appointment {AppointmentNumber} created by user {UserId}",
                    response.AppointmentNumber, createdBy);

                return CreatedAtAction(
                    nameof(GetAppointmentById),
                    new { id = response.Id },
                    response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, new { message = "An error occurred while creating the appointment" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentById(Guid id)
        {
            try
            {
                var response = await _appointmentService.GetAppointmentByIdAsync(id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the appointment" });
            }
        }

        [HttpGet("number/{appointmentNumber}")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentByNumber(string appointmentNumber)
        {
            try
            {
                var response = await _appointmentService.GetAppointmentByNumberAsync(appointmentNumber);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment {AppointmentNumber}", appointmentNumber);
                return StatusCode(500, new { message = "An error occurred while retrieving the appointment" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAppointments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] AppointmentStatus? status = null,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var response = await _appointmentService.GetAllAppointmentsAsync(page, pageSize, status, date);

                return Ok(new
                {
                    data = response,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = response.Count
                    },
                    filters = new { status, date }
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpGet("patient/{patientId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientAppointments(
            Guid patientId,
            [FromQuery] bool includeHistory = false)
        {
            try
            {
                var response = await _appointmentService.GetPatientAppointmentsAsync(patientId, includeHistory);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpGet("doctor/{doctorId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorAppointments(
            Guid doctorId,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var response = await _appointmentService.GetDoctorAppointmentsAsync(doctorId, date);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpGet("today")]
        [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodayAppointments([FromQuery] Guid? doctorId = null)
        {
            try
            {
                var response = await _appointmentService.GetTodayAppointmentsAsync(doctorId);
                return Ok(new
                {
                    date = DateTime.UtcNow.Date,
                    appointments = response,
                    count = response.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving today's appointments" });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAppointment(Guid id, [FromBody] UpdateAppointmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = ModelState });
            }

            try
            {
                var response = await _appointmentService.UpdateAppointmentAsync(id, request);
                _logger.LogInformation("Appointment {AppointmentId} updated", id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the appointment" });
            }
        }

        [HttpPut("{id}/reschedule")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RescheduleAppointment(
            Guid id,
            [FromQuery] DateTime newDate,
            [FromQuery] TimeSpan newStartTime)
        {
            try
            {
                var response = await _appointmentService.RescheduleAppointmentAsync(id, newDate, newStartTime);
                _logger.LogInformation("Appointment {AppointmentId} rescheduled", id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while rescheduling the appointment" });
            }
        }

        [HttpPut("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = ModelState });
            }

            try
            {
                await _appointmentService.CancelAppointmentAsync(id, request.CancellationReason);
                _logger.LogInformation("Appointment {AppointmentId} cancelled", id);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling the appointment" });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor,Nurse,Receptionist,Admin")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = ModelState });
            }

            try
            {
                var response = await _appointmentService.UpdateStatusAsync(id, request.Status, request.Notes);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while updating appointment status" });
            }
        }

        [HttpPut("{id}/check-in")]
        [Authorize(Roles = "Nurse,Receptionist,Admin")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckIn(Guid id)
        {
            try
            {
                var response = await _appointmentService.CheckInAsync(id);
                _logger.LogInformation("Patient checked in for appointment {AppointmentId}", id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred during check-in" });
            }
        }

        [HttpPut("{id}/start")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartConsultation(Guid id)
        {
            try
            {
                var response = await _appointmentService.StartConsultationAsync(id);
                _logger.LogInformation("Consultation started for appointment {AppointmentId}", id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting consultation for appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while starting consultation" });
            }
        }

        [HttpPut("{id}/complete")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompleteAppointment(Guid id)
        {
            try
            {
                var response = await _appointmentService.CompleteAppointmentAsync(id);
                _logger.LogInformation("Appointment {AppointmentId} completed", id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while completing the appointment" });
            }
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1).Date;
                var end = endDate ?? DateTime.UtcNow.Date;

                var response = await _appointmentService.GetAppointmentStatisticsAsync(start, end);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }
    }
}