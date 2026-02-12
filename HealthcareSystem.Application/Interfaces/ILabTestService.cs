using HealthcareSystem.Application.Dto.MedicalRecords;
using HealthcareSystem.Application.DTOs.LabTest;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface ILabTestService
    {
        /// <summary>
        /// Order a new lab test
        /// </summary>
        Task<LabTestResponse> OrderLabTestAsync(CreateLabTestRequest request);

        /// <summary>
        /// Get lab test by ID
        /// </summary>
        Task<LabTestResponse> GetLabTestByIdAsync(Guid labTestId);

        /// <summary>
        /// Get patient's lab tests with optional status filter
        /// </summary>
        Task<List<LabTestResponse>> GetPatientLabTestsAsync(Guid patientId, LabTestStatus? status);

        /// <summary>
        /// Get doctor's ordered lab tests with optional date range
        /// </summary>
        Task<List<LabTestResponse>> GetDoctorLabTestsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// Get all lab tests with pagination and search
        /// </summary>
        Task<List<LabTestResponse>> GetAllLabTestsAsync(int page, int pageSize, string? search);

        /// <summary>
        /// Collect sample for lab test
        /// Updates status from Ordered to SampleCollected
        /// Frontend: useCollectSample hook
        /// </summary>
        Task<LabTestResponse> CollectSampleAsync(Guid labTestId);

        /// <summary>
        /// Start processing lab test
        /// Updates status from SampleCollected to InProgress
        /// Frontend: useStartProcessing hook
        /// </summary>
        Task<LabTestResponse> StartProcessingAsync(Guid labTestId);

        /// <summary>
        /// Update lab test result with all result fields
        /// Updates status to Completed and sends notifications
        /// Frontend: useUpdateLabTestResult hook (MAIN METHOD)
        /// </summary>
        Task<LabTestResponse> UpdateLabTestResultAsync(Guid labTestId, UpdateLabTestResultRequest request);

        /// <summary>
        /// Complete lab test (mark as completed)
        /// Frontend: useCompleteLabTest hook
        /// </summary>
        Task<LabTestResponse> CompleteLabTestAsync(Guid labTestId);

        /// <summary>
        /// Cancel lab test with reason
        /// Frontend: useCancelLabTest hook (PUT with body)
        /// </summary>
        Task<LabTestResponse> CancelLabTestWithReasonAsync(Guid labTestId, string cancellationReason);

        /// <summary>
        /// Cancel lab test (legacy method without reason)
        /// </summary>
        Task<bool> CancelLabTestAsync(Guid labTestId);

        /// <summary>
        /// Update lab test status and basic info
        /// </summary>
        Task<LabTestResponse> UpdateLabTestAsync(Guid labTestId, UpdateLabTestRequest request);

        /// <summary>
        /// Upload lab test result file (PDF, image, etc.)
        /// </summary>
        Task<LabTestResponse> UploadLabTestResultAsync(Guid labTestId, byte[] fileContent, string fileName);
    }
}