using HealthcareSystem.API.Attributes;
using HealthcareSystem.Application.DTOs.Prescription;
using HealthcareSystem.Application.Interfaces;
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
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<PrescriptionsController> _logger;

        public PrescriptionsController(
            IPrescriptionService prescriptionService,
            ILogger<PrescriptionsController> logger)
        {
            _prescriptionService = prescriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new prescription
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        [RateLimit(PermitLimit = 50, Window = 60)]
        [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreatePrescription([FromBody] CreatePrescriptionRequest request)
        {
            _logger.LogInformation("Creating prescription for medical record {MedicalRecordId}", request.MedicalRecordId);

            var response = await _prescriptionService.CreatePrescriptionAsync(request);

            return CreatedAtAction(nameof(GetPrescriptionById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Get prescription by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPrescriptionById(Guid id)
        {
            _logger.LogInformation("Retrieving prescription {PrescriptionId}", id);

            var response = await _prescriptionService.GetPrescriptionByIdAsync(id);

            return Ok(response);
        }

        /// <summary>
        /// Get prescription by prescription number
        /// </summary>
        [HttpGet("number/{prescriptionNumber}")]
        [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPrescriptionByNumber(string prescriptionNumber)
        {
            _logger.LogInformation("Retrieving prescription by number {PrescriptionNumber}", prescriptionNumber);

            var response = await _prescriptionService.GetPrescriptionByNumberAsync(prescriptionNumber);

            return Ok(response);
        }

        /// <summary>
        /// Get all prescriptions for a patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [ProducesResponseType(typeof(List<PrescriptionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientPrescriptions(Guid patientId)
        {
            _logger.LogInformation("Retrieving prescriptions for patient {PatientId}", patientId);

            var response = await _prescriptionService.GetPatientPrescriptionsAsync(patientId);

            return Ok(response);
        }

        /// <summary>
        /// Get prescriptions created by a doctor
        /// </summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(List<PrescriptionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorPrescriptions(
            Guid doctorId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            _logger.LogInformation("Retrieving prescriptions for doctor {DoctorId}", doctorId);

            var response = await _prescriptionService.GetDoctorPrescriptionsAsync(doctorId, fromDate, toDate);

            return Ok(response);
        }

        /// <summary>
        /// Generate prescription PDF (Will be implemented on Day 7)
        /// </summary>
        [HttpGet("{id}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public async Task<IActionResult> GeneratePrescriptionPdf(Guid id)
        {
            _logger.LogInformation("Generating PDF for prescription {PrescriptionId}", id);

            var pdfBytes = await _prescriptionService.GeneratePrescriptionPdfAsync(id);

            return File(pdfBytes, "application/pdf", $"prescription-{id}.pdf");
        }
        /// <summary>
        /// Get all prescriptions with pagination and search
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")] // Roles frontend requirements ke mutabik adjust kiye hain
        [ProducesResponseType(typeof(List<PrescriptionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPrescriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            _logger.LogInformation("Retrieving all prescriptions. Page: {Page}, Size: {PageSize}, Search: {Search}",
                page, pageSize, search);

            var response = await _prescriptionService.GetAllPrescriptionsAsync(page, pageSize, search);

            return Ok(response);
        }

        /// <summary>
        /// Get active prescriptions for a patient
        /// </summary>
        [HttpGet("patient/{patientId}/active")]
        [ProducesResponseType(typeof(List<PrescriptionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActivePatientPrescriptions(Guid patientId)
        {
            _logger.LogInformation("Retrieving active prescriptions for patient {PatientId}", patientId);

            // Note: Iske liye aapko service mein ek chota sa filter lagana hoga 
            // jo sirf 'ValidUntil > DateTime.Now' wale records laye.
            var response = await _prescriptionService.GetPatientPrescriptionsAsync(patientId);
            var activePrescriptions = response.Where(p => !p.ValidUntil.HasValue || p.ValidUntil > DateTime.UtcNow).ToList();

            return Ok(activePrescriptions);
        }

       
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Doctor,Admin")]
        //public async Task<IActionResult> CancelPrescription(Guid id, [FromBody] string cancellationReason)
        //{
        //    _logger.LogInformation("Cancelling prescription {Id}", id);
        //    // TODO: Implement Cancel logic (e.g., updating status enum)
        //    return Ok();
        //}

        /// <summary>
        /// Mark as completed (TanStack: useCompletePrescription)
        /// </summary>
        [HttpPut("{id}/complete")]
        [Authorize(Roles = "Pharmacist,Admin")]
        //public async Task<IActionResult> CompletePrescription(Guid id)
        //{
        //    _logger.LogInformation("Completing prescription {Id}", id);
        //    return Ok();
        //}

        /// <summary>
        /// Delete prescription (TanStack: useDeletePrescription)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePrescription(Guid id)
        {
            _logger.LogInformation("Deleting prescription {Id}", id);
            // await _prescriptionService.DeleteAsync(id);
            return NoContent();
        }

    }
}