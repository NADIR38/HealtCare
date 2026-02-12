using HealthcareSystem.Application.Dto.MedicalRecords;
using HealthcareSystem.Application.DTOs.LabTest;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Application.Models;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class LabTestService : ILabTestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LabTestService> _logger;
        private readonly DatabaseHelpers _helper;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public LabTestService(
            ApplicationDbContext context,
            ILogger<LabTestService> logger,
            DatabaseHelpers helper,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _helper = helper;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<LabTestResponse> OrderLabTestAsync(CreateLabTestRequest request)
        {
            var doctor = await _helper.CheckDoctorExists(request.DoctorId);
            var patient = await _helper.CheckPatientExist(request.PatientId);

            var labTest = new LabTest
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                MedicalRecordId = request.MedicalRecordId,
                TestName = request.TestName,
                TestType = request.TestType,
                OrderedDate = DateTime.UtcNow,
                Status = LabTestStatus.Ordered,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.LabTests.Add(labTest);
            await _context.SaveChangesAsync();

            var response = new LabTestResponse
            {
                Id = labTest.Id,
                PatientId = labTest.PatientId,
                DoctorId = labTest.DoctorId,
                DoctorName = doctor.User.FirstName,
                PatientName = patient.User.FirstName,
                TestName = labTest.TestName,
                TestType = labTest.TestType,
                OrderedDate = labTest.OrderedDate,
                Status = labTest.Status,
                CreatedAt = labTest.CreatedAt,
                UpdatedAt = labTest.UpdatedAt,
            };

            return response;
        }

        public async Task<LabTestResponse> GetLabTestByIdAsync(Guid labTestId)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("No test found for this id", labTestId);
            }

            return MapToResponse(labTest);
        }

        public async Task<List<LabTestResponse>> GetPatientLabTestsAsync(Guid patientId, LabTestStatus? status)
        {
            var patient = await _helper.CheckPatientExist(patientId);

            var labTest = _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .Where(u => u.PatientId == patientId)
                .AsQueryable();

            if (status.HasValue)
            {
                labTest = labTest.Where(u => u.Status == status.Value);
            }

            var tests = await labTest.OrderByDescending(u => u.OrderedDate).ToListAsync();
            var response = new List<LabTestResponse>();

            foreach (var item in tests)
            {
                response.Add(MapToResponse(item));
            }

            return response;
        }

        public async Task<List<LabTestResponse>> GetDoctorLabTestsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate)
        {
            var labTest = _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .Where(u => u.DoctorId == doctorId)
                .AsQueryable();

            if (labTest == null)
            {
                throw new NotFoundException("No Test for this Id", doctorId);
            }

            var doctor = await _helper.CheckDoctorExists(doctorId);

            if (fromDate != null && toDate != null)
            {
                labTest = labTest.Where(u => u.CreatedAt > fromDate && u.CreatedAt < toDate);
            }

            var tests = await labTest.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var response = new List<LabTestResponse>();

            foreach (var item in tests)
            {
                response.Add(MapToResponse(item));
            }

            return response;
        }

        public async Task<List<LabTestResponse>> GetAllLabTestsAsync(int page, int pageSize, string? search)
        {
            // Pagination Validation
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Query with all necessary Includes
            var query = _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .AsQueryable();

            // Search Logic
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(t =>
                    t.TestName.ToLower().Contains(search) ||
                    t.TestType.ToLower().Contains(search) ||
                    (t.Patient.User.FirstName + " " + t.Patient.User.LastName).ToLower().Contains(search) ||
                    (t.Doctor.User.FirstName + " " + t.Doctor.User.LastName).ToLower().Contains(search)
                );
            }

            // Execution
            var tests = await query
                .OrderByDescending(u => u.OrderedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Mapping
            return tests.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Collect sample - Updates status from Ordered to SampleCollected
        /// Frontend: useCollectSample hook
        /// </summary>
        public async Task<LabTestResponse> CollectSampleAsync(Guid labTestId)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("Lab test", labTestId);
            }

            // Validate status
            if (labTest.Status != LabTestStatus.Ordered)
            {
                throw new BusinessException($"Cannot collect sample for lab test with status: {labTest.Status}");
            }

            // Update status
            labTest.Status = LabTestStatus.SampleCollected;
            labTest.SampleCollectedDate = DateTime.UtcNow;
            labTest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Sample collected for lab test {LabTestId}", labTestId);

            return MapToResponse(labTest);
        }

        /// <summary>
        /// Start processing - Updates status from SampleCollected to InProgress
        /// Frontend: useStartProcessing hook
        /// </summary>
        public async Task<LabTestResponse> StartProcessingAsync(Guid labTestId)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("Lab test", labTestId);
            }

            // Validate status
            if (labTest.Status != LabTestStatus.SampleCollected)
            {
                throw new BusinessException(
                    $"Cannot start processing lab test with status: {labTest.Status}. Sample must be collected first.");
            }

            // Update status
            labTest.Status = LabTestStatus.InProgress;
            labTest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Lab test {LabTestId} status updated to InProgress", labTestId);

            return MapToResponse(labTest);
        }

        /// <summary>
        /// Update lab test result - Main method for entering results
        /// Frontend: useUpdateLabTestResult hook
        /// </summary>
        public async Task<LabTestResponse> UpdateLabTestResultAsync(Guid labTestId, UpdateLabTestResultRequest request)
        {
            // 1. Fetch lab test with all necessary includes
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("Lab test", labTestId);
            }

            // 2. Validate current status
            if (labTest.Status != LabTestStatus.InProgress && labTest.Status != LabTestStatus.SampleCollected)
            {
                throw new BusinessException($"Cannot update results for lab test with status: {labTest.Status}");
            }

            // 3. Update all result fields
            labTest.Results = request.Result;              // Main result text
            labTest.ResultValue = request.ResultValue;      // Numeric value
            labTest.ResultUnit = request.ResultUnit;        // Unit
            labTest.ReferenceRange = request.ReferenceRange; // Reference range
            labTest.Notes = request.Notes;                  // Notes
            labTest.AbnormalFlag = request.AbnormalFlag;    // Abnormal flag

            // 4. Update status to Completed
            labTest.Status = LabTestStatus.Completed;
            labTest.ResultDate = DateTime.UtcNow;
            labTest.UpdatedAt = DateTime.UtcNow;

            // 5. Save changes
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Lab test {LabTestId} results updated. Status: Completed, Abnormal: {Abnormal}",
                labTestId,
                labTest.AbnormalFlag);

            // 6. Send notifications asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    // Send in-app notification
                    await _notificationService.SendNotificationAsync(
                        labTest.PatientId,
                        NotificationType.LabTestCompleted,
                        "Lab Test Results Available",
                        $"Your {labTest.TestName} results are now available. {(labTest.AbnormalFlag ? "Please review with your doctor." : "")}",
                        $"/lab-tests/{labTestId}",
                        labTestId.ToString());

                    // Send email notification
                    await _emailService.SendEmailAsync(new EmailMessage
                    {
                        To = new List<string> { labTest.Patient.User.Email },
                        Subject = $"Lab Test Results: {labTest.TestName}",
                        Body = BuildResultEmailBody(labTest),
                        IsHtml = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notifications for lab test {Id}", labTestId);
                }
            });

            // 7. Return mapped response
            return MapToResponse(labTest);
        }

        /// <summary>
        /// Complete lab test (mark as completed without entering results)
        /// Frontend: useCompleteLabTest hook
        /// </summary>
        public async Task<LabTestResponse> CompleteLabTestAsync(Guid labTestId)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("Lab test", labTestId);
            }

            // Validate that test has results
            if (string.IsNullOrWhiteSpace(labTest.Results))
            {
                throw new BusinessException(
                    "Cannot complete lab test without results. Please enter results first.");
            }

            // Update status if not already completed
            if (labTest.Status != LabTestStatus.Completed)
            {
                labTest.Status = LabTestStatus.Completed;
                labTest.ResultDate = labTest.ResultDate ?? DateTime.UtcNow;
                labTest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Lab test {LabTestId} marked as completed", labTestId);
            }

            return MapToResponse(labTest);
        }

        /// <summary>
        /// Cancel lab test with reason
        /// Frontend: useCancelLabTest hook
        /// </summary>
        public async Task<LabTestResponse> CancelLabTestWithReasonAsync(Guid labTestId, string cancellationReason)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("No test found for this id", labTestId);
            }

            // Validate can be cancelled
            if (labTest.Status == LabTestStatus.Completed)
            {
                throw new BusinessException("Cannot cancel completed test");
            }

            if (labTest.Status == LabTestStatus.Cancelled)
            {
                throw new BusinessException("Test is already cancelled");
            }

            // Update status
            labTest.Status = LabTestStatus.Cancelled;
            labTest.Notes = string.IsNullOrEmpty(labTest.Notes)
                ? $"Cancelled: {cancellationReason}"
                : $"{labTest.Notes}\nCancelled: {cancellationReason}";
            labTest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Lab test {LabTestId} cancelled. Reason: {Reason}",
                labTestId,
                cancellationReason);

            // Send email notification
            try
            {
                await _emailService.SendEmailAsync(new EmailMessage
                {
                    Subject = $"Cancellation of Test: {labTest.TestName}",
                    Body = $"Your lab test has been cancelled. Reason: {cancellationReason}",
                    To = new List<string> { labTest.Patient.User.Email },
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation email for lab test {Id}", labTestId);
            }

            return MapToResponse(labTest);
        }

        public async Task<bool> CancelLabTestAsync(Guid labTestId)
        {
            return (await CancelLabTestWithReasonAsync(labTestId, "Cancelled by user")).Id != Guid.Empty;
        }

        public async Task<LabTestResponse> UpdateLabTestAsync(Guid labTestId, UpdateLabTestRequest request)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("No test for this id", labTestId);
            }

            if (request.Status != null)
            {
                labTest.Status = (LabTestStatus)request.Status;
            }

            labTest.Results = request.Results;
            labTest.Notes = request.Notes;

            await _context.SaveChangesAsync();

            if (request.Status == LabTestStatus.Completed)
            {
                await _emailService.SendEmailAsync(new EmailMessage
                {
                    Body = $"The Lab Test {labTest.TestName} is Completed on Date {labTest.ResultDate}",
                    Subject = $"Status Update regarding your Test about {labTest.TestName}",
                    To = new List<string> { labTest.Patient.User.Email },
                });

                await _notificationService.SendNotificationAsync(
                    labTest.PatientId,
                    NotificationType.LabTestCompleted,
                    "Lab test Completed",
                    $"Your {labTest.TestName} results are now available",
                    $"/lab-tests/{labTestId}",
                    labTestId.ToString());
            }

            return MapToResponse(labTest);
        }

        public async Task<LabTestResponse> UploadLabTestResultAsync(Guid labTestId, byte[] fileContent, string fileName)
        {
            var labTest = await _context.LabTests
                .Include(u => u.Doctor).ThenInclude(u => u.User)
                .Include(p => p.Patient).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(u => u.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("No test for this id", labTestId);
            }

            var uploadsFolder = Path.Combine("uploads", "lab_results");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            await File.WriteAllBytesAsync(filePath, fileContent);

            labTest.Status = LabTestStatus.Completed;
            labTest.ResultDate = DateTime.UtcNow;
            labTest.ResultFileUrl = filePath;

            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(new EmailMessage
            {
                Subject = $"Upload of report Of test {labTest.TestName}",
                Body = $"The test is Completed and report is Uploaded on Website",
                To = new List<string> { labTest.Patient.User.Email },
            });

            return MapToResponse(labTest);
        }

        private LabTestResponse MapToResponse(LabTest labTest)
        {
            var response = new LabTestResponse
            {
                Id = labTest.Id,
                DoctorName = labTest.Doctor.User.FirstName,
                PatientId = labTest.Patient.Id,
                DoctorId = labTest.Doctor.Id,
                PatientName = labTest.Patient.User.FirstName,
                TestName = labTest.TestName,
                TestType = labTest.TestType,
                Status = labTest.Status,
                OrderedDate = labTest.OrderedDate,
                SampleCollectedDate = labTest.SampleCollectedDate,
                ResultDate = labTest.ResultDate,
                Results = labTest.Results,
                ResultFileUrl = labTest.ResultFileUrl,
                Notes = labTest.Notes,
                CreatedAt = labTest.CreatedAt,
                UpdatedAt = labTest.UpdatedAt,
               ResultValue= labTest.ResultValue,
                ResultUnit = labTest.ResultUnit,
                ReferenceRange = labTest.ReferenceRange,
                AbnormalFlag = labTest.AbnormalFlag
            };

            return response;
        }

        private string BuildResultEmailBody(LabTest labTest)
        {
            var abnormalWarning = labTest.AbnormalFlag
                ? "<p style='color: #ef4444; font-weight: bold;'>⚠️ Abnormal Result - Please consult with your doctor</p>"
                : "";

            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #1e40af;'>Lab Test Results Available</h2>
                    <p>Dear {labTest.Patient.User.FirstName},</p>
                    <p>Your lab test results are now available:</p>
                    
                    <div style='background-color: #f3f4f6; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0;'>{labTest.TestName}</h3>
                        <p><strong>Test Type:</strong> {labTest.TestType}</p>
                        <p><strong>Result Date:</strong> {labTest.ResultDate?.ToString("MMMM dd, yyyy")}</p>
                        {abnormalWarning}
                        <p><strong>Result:</strong> {labTest.Results}</p>
                        {(!string.IsNullOrEmpty(labTest.ResultValue) ? $"<p><strong>Value:</strong> {labTest.ResultValue} {labTest.ResultUnit}</p>" : "")}
                        {(!string.IsNullOrEmpty(labTest.ReferenceRange) ? $"<p><strong>Reference Range:</strong> {labTest.ReferenceRange}</p>" : "")}
                        {(!string.IsNullOrEmpty(labTest.Notes) ? $"<p><strong>Notes:</strong> {labTest.Notes}</p>" : "")}
                    </div>
                    
                    <p>Please login to the portal to view detailed results and download the report.</p>
                    <p style='color: #6b7280; font-size: 12px; margin-top: 30px;'>
                        This is an automated message. Please do not reply to this email.
                    </p>
                </div>
            ";
        }
    }
}