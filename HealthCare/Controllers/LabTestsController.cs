using HealthcareSystem.API.Attributes;
using HealthcareSystem.Application.DTOs.LabTest;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HealthcareSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LabTestsController : ControllerBase
    {
        private readonly ILabTestService _labTestService;
        private readonly ILogger<LabTestsController> _logger;

        public LabTestsController(
            ILabTestService labTestService,
            ILogger<LabTestsController> logger)
        {
            _labTestService = labTestService;
            _logger = logger;
        }

        /// <summary>
        /// Order a new lab test
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        [RateLimit(PermitLimit = 50, Window = 60)]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OrderLabTest([FromBody] CreateLabTestRequest request)
        {
            _logger.LogInformation("Ordering lab test {TestName} for patient {PatientId}",
                request.TestName, request.PatientId);

            var response = await _labTestService.OrderLabTestAsync(request);

            return CreatedAtAction(nameof(GetLabTestById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Get lab test by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLabTestById(Guid id)
        {
            _logger.LogInformation("Retrieving lab test {LabTestId}", id);

            var response = await _labTestService.GetLabTestByIdAsync(id);

            return Ok(response);
        }

        /// <summary>
        /// Get all lab tests for a patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [ProducesResponseType(typeof(List<LabTestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientLabTests(
            Guid patientId,
            [FromQuery] LabTestStatus? status = null)
        {
            _logger.LogInformation("Retrieving lab tests for patient {PatientId} with status filter {Status}",
                patientId, status);

            var response = await _labTestService.GetPatientLabTestsAsync(patientId, status);

            return Ok(response);
        }

        /// <summary>
        /// Get lab tests ordered by a doctor
        /// </summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(List<LabTestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorLabTests(
            Guid doctorId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            _logger.LogInformation("Retrieving lab tests for doctor {DoctorId}", doctorId);

            var response = await _labTestService.GetDoctorLabTestsAsync(doctorId, fromDate, toDate);

            return Ok(response);
        }

        /// <summary>
        /// Update lab test status and results
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,LabTechnician,Admin")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLabTest(
            Guid id,
            [FromBody] UpdateLabTestRequest request)
        {
            _logger.LogInformation("Updating lab test {LabTestId}", id);

            var response = await _labTestService.UpdateLabTestAsync(id, request);

            return Ok(response);
        }

        /// <summary>
        /// Upload lab test result file (PDF, image, etc.)
        /// </summary>
        [HttpPost("{id}/upload")]
        [Authorize(Roles = "Doctor,LabTechnician,Admin")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadLabTestResult(
            Guid id,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size must not exceed 10MB" });
            }

            // Validate file type (PDF, PNG, JPG, JPEG)
            var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Only PDF, PNG, JPG, and JPEG files are allowed" });
            }

            _logger.LogInformation("Uploading result file for lab test {LabTestId}: {FileName}",
                id, file.FileName);

            // Read file content
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            var response = await _labTestService.UploadLabTestResultAsync(id, fileContent, file.FileName);

            return Ok(response);
        }

        /// <summary>
        /// Download lab test result file
        /// </summary>
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadLabTestResult(Guid id)
        {
            _logger.LogInformation("Downloading result file for lab test {LabTestId}", id);

            var labTest = await _labTestService.GetLabTestByIdAsync(id);

            if (string.IsNullOrEmpty(labTest.ResultFileUrl))
            {
                return NotFound(new { message = "No result file available for this lab test" });
            }

            if (!System.IO.File.Exists(labTest.ResultFileUrl))
            {
                _logger.LogError("Result file not found on disk: {FilePath}", labTest.ResultFileUrl);
                return NotFound(new { message = "Result file not found" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(labTest.ResultFileUrl);
            var fileName = Path.GetFileName(labTest.ResultFileUrl);
            var contentType = GetContentType(fileName);

            return File(fileBytes, contentType, fileName);
        }

        /// <summary>
        /// Cancel a lab test
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelLabTest(Guid id)
        {
            _logger.LogInformation("Cancelling lab test {LabTestId}", id);

            var result = await _labTestService.CancelLabTestAsync(id);

            return Ok(new { message = "Lab test cancelled successfully", cancelled = result });
        }

        /// <summary>
        /// Get lab test statistics for a doctor
        /// </summary>
        [HttpGet("doctor/{doctorId}/statistics")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLabTestStatistics(
            Guid doctorId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            _logger.LogInformation("Retrieving lab test statistics for doctor {DoctorId}", doctorId);

            var tests = await _labTestService.GetDoctorLabTestsAsync(doctorId, fromDate, toDate);

            var statistics = new
            {
                totalTests = tests.Count,
                ordered = tests.Count(t => t.Status == LabTestStatus.Ordered),
                sampleCollected = tests.Count(t => t.Status == LabTestStatus.SampleCollected),
                inProgress = tests.Count(t => t.Status == LabTestStatus.InProgress),
                completed = tests.Count(t => t.Status == LabTestStatus.Completed),
                cancelled = tests.Count(t => t.Status == LabTestStatus.Cancelled),
                completionRate = tests.Count > 0
                    ? Math.Round((double)tests.Count(t => t.Status == LabTestStatus.Completed) / tests.Count * 100, 2)
                    : 0
            };

            return Ok(statistics);
        }

        // Helper method to get content type based on file extension
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}