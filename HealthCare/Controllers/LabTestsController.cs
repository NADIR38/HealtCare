using HealthcareSystem.API.Attributes;
using HealthcareSystem.Application.Dto.MedicalRecords;
using HealthcareSystem.Application.DTOs.LabTest;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Helpers;
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
        /// Get all lab tests with pagination and search (Admin/Staff only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,LabTechnician")]
        public async Task<IActionResult> GetAllLabTests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            _logger.LogInformation("Retrieving all lab tests. Page: {Page}, Search: {Search}", page, search);

            var response = await _labTestService.GetAllLabTestsAsync(page, pageSize, search);

            return Ok(response);
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

        /// <summary>
        /// Mark lab test sample as collected
        /// Frontend: useCollectSample mutation
        /// </summary>
        [HttpPut("{id}/collect-sample")]
        [Authorize(Roles = "LabTechnician,Nurse,Admin")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CollectSample(Guid id)
        {
            _logger.LogInformation("Collecting sample for lab test {Id}", id);

            var response = await _labTestService.CollectSampleAsync(id);

            return Ok(response);
        }

        /// <summary>
        /// Start processing lab test
        /// Frontend: useStartProcessing mutation
        /// </summary>
        [HttpPut("{id}/start-processing")]
        [Authorize(Roles = "LabTechnician,Admin")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartProcessing(Guid id)
        {
            _logger.LogInformation("Starting processing for lab test {Id}", id);

            var response = await _labTestService.StartProcessingAsync(id);

            return Ok(response);
        }

        /// <summary>
        /// Update lab test results (TanStack: useUpdateLabTestResult)
        /// Frontend: PUT /api/labtests/{id}/result
        /// </summary>
        [HttpPut("{id}/result")]
        [Authorize(Roles = "LabTechnician,Admin")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLabTestResult(Guid id, [FromBody] UpdateLabTestResultRequest request)
        {
            _logger.LogInformation("Updating results for lab test {Id}", id);

            //// Validate required field
            //if (string.IsNullOrWhiteSpace(request.Result))
            //{
            //    return BadRequest(new { message = "Result field is required" });
            //}

            var response = await _labTestService.UpdateLabTestResultAsync(id, request);

            return Ok(response);
        }

        /// <summary>
        /// Complete test (mark as completed)
        /// Frontend: useCompleteLabTest mutation
        /// </summary>
        [HttpPut("{id}/complete")]
        [Authorize(Roles = "LabTechnician,Admin")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete(Guid id)
        {
            _logger.LogInformation("Completing lab test {Id}", id);

            var response = await _labTestService.CompleteLabTestAsync(id);

            return Ok(response);
        }

        /// <summary>
        /// Cancel lab test
        /// Frontend: useCancelLabTest mutation (PUT request with body)
        /// </summary>
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Doctor,Admin,LabTechnician")]
        [ProducesResponseType(typeof(LabTestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelLabTestRequest request)
        {
            _logger.LogInformation("Cancelling lab test {Id} with reason: {Reason}", id, request.CancellationReason);

            var response = await _labTestService.CancelLabTestWithReasonAsync(id, request.CancellationReason);

            return Ok(response);
        }

        /// <summary>
        /// Update lab test status and basic info
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

            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size must not exceed 10MB" });
            }

            var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Only PDF, PNG, JPG, and JPEG files are allowed" });
            }

            _logger.LogInformation("Uploading result file for lab test {LabTestId}: {FileName}",
                id, file.FileName);

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
        /// Download lab test report PDF
        /// </summary>
        [HttpGet("{id}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadLabTestReportPdf(Guid id, [FromServices] IPdfService pdfService)
        {
            try
            {
                _logger.LogInformation("Generating lab test report PDF for test {LabTestId}", id);

                var pdfBytes = await pdfService.GenerateLabTestReportPdfAsync(id);

                return File(pdfBytes, "application/pdf", $"lab-test-report-{id}.pdf");
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lab test report PDF");
                return StatusCode(500, new { message = "Error generating PDF" });
            }
        }

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

    /// <summary>
    /// Request model for cancelling a lab test
    /// </summary>
    public class CancelLabTestRequest
    {
        public string CancellationReason { get; set; } = string.Empty;
    }
}