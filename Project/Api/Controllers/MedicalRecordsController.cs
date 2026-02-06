using HealthcareSystem.API.Attributes;
using HealthcareSystem.Application.Dto.MedicalRecords;
using HealthcareSystem.Application.DTOs.MedicalRecord;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MedicalRecordsController : ControllerBase
    {
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly ILogger<MedicalRecordsController> _logger;

        public MedicalRecordsController(
            IMedicalRecordService medicalRecordService,
            ILogger<MedicalRecordsController> logger)
        {
            _medicalRecordService = medicalRecordService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new medical record
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        [RateLimit(PermitLimit = 100, Window = 60)]
        [ProducesResponseType(typeof(MedicalRecordResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateMedicalRecord([FromBody] CreateMedicalRecordRequest request)
        {
            _logger.LogInformation("Creating medical record for patient {PatientId}", request.PatientId);

            var response = await _medicalRecordService.CreateMedicalRecordAsync(request);

            return CreatedAtAction(nameof(GetMedicalRecordById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Get medical record by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MedicalRecordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMedicalRecordById(Guid id)
        {
            _logger.LogInformation("Retrieving medical record {RecordId}", id);

            var response = await _medicalRecordService.GetMedicalRecordByIdAsync(id);

            return Ok(response);
        }

        /// <summary>
        /// Get all medical records for a patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [ProducesResponseType(typeof(List<MedicalRecordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientMedicalRecords(Guid patientId)
        {
            _logger.LogInformation("Retrieving medical records for patient {PatientId}", patientId);

            var response = await _medicalRecordService.GetPatientMedicalRecordsAsync(patientId);

            return Ok(response);
        }

        /// <summary>
        /// Get medical records created by a doctor
        /// </summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(List<MedicalRecordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorMedicalRecords(
            Guid doctorId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            _logger.LogInformation("Retrieving medical records for doctor {DoctorId}", doctorId);

            var response = await _medicalRecordService.GetDoctorMedicalRecordsAsync(doctorId, fromDate, toDate);

            return Ok(response);
        }

        /// <summary>
        /// Update an existing medical record
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(MedicalRecordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMedicalRecord(
            Guid id,
            [FromBody] UpdateMedicalRecordRequest request)
        {
            _logger.LogInformation("Updating medical record {RecordId}", id);

            var response = await _medicalRecordService.UpdateMedicalRecordAsync(id, request);

            return Ok(response);
        }

        /// <summary>
        /// Add or update vital signs for a medical record
        /// </summary>
        [HttpPut("{id}/vital-signs")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        [ProducesResponseType(typeof(MedicalRecordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddOrUpdateVitalSigns(
            Guid id,
            [FromBody] VitalSignsRequest request)
        {
            _logger.LogInformation("Updating vital signs for medical record {RecordId}", id);

            var response = await _medicalRecordService.AddOrUpdateVitalSignsAsync(id, request);

            return Ok(response);
        }

        /// <summary>
        /// Delete a medical record (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMedicalRecord(Guid id)
        {
            _logger.LogInformation("Deleting medical record {RecordId}", id);

            await _medicalRecordService.DeleteMedicalRecordAsync(id);

            return NoContent();
        }
        /// <summary>
        /// Download medical report as PDF
        /// </summary>
        [HttpGet("{id}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadMedicalReportPdf(Guid id, [FromServices] IPdfService pdfService)
        {
            try
            {
                _logger.LogInformation("Generating medical report PDF for record {RecordId}", id);

                var pdfBytes = await pdfService.GenerateMedicalReportPdfAsync(id);

                return File(pdfBytes, "application/pdf", $"medical-report-{id}.pdf");
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating medical report PDF");
                return StatusCode(500, new { message = "Error generating PDF" });
            }
        }
    }
    }
