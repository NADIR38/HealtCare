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
        /// TODO: Order a lab test
        /// - Validate patient and doctor exist
        /// - If medicalRecordId provided, validate it
        /// - Create lab test with status "Ordered"
        /// - Set ordered date to now
        /// - Save to database
        /// - Return response
        /// </summary>
        Task<LabTestResponse> OrderLabTestAsync(CreateLabTestRequest request);

        /// <summary>
        /// TODO: Get lab test by ID
        /// - Fetch with includes
        /// - Map to response
        /// </summary>
        Task<LabTestResponse> GetLabTestByIdAsync(Guid labTestId);

        /// <summary>
        /// TODO: Get patient's lab tests
        /// - Validate patient exists
        /// - Fetch tests ordered by ordered date descending
        /// - Optionally filter by status
        /// - Map to responses
        /// </summary>
        Task<List<LabTestResponse>> GetPatientLabTestsAsync(Guid patientId, LabTestStatus? status);

        /// <summary>
        /// TODO: Get doctor's ordered lab tests
        /// - Validate doctor exists
        /// - Fetch tests with date filters
        /// - Map to responses
        /// </summary>
        Task<List<LabTestResponse>> GetDoctorLabTestsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// TODO: Update lab test status
        /// - Fetch test
        /// - Update status and related dates based on new status:
        ///   - If SampleCollected, set SampleCollectedDate
        ///   - If Completed, set ResultDate
        /// - Update results if provided
        /// - Save changes
        /// - If status is Completed, send email notification to patient
        /// - Return response
        /// </summary>
        Task<LabTestResponse> UpdateLabTestAsync(Guid labTestId, UpdateLabTestRequest request);

        /// <summary>
        /// TODO: Upload lab test result file
        /// - Validate test exists
        /// - Save file to storage (file system/blob storage)
        /// - Update test with file URL
        /// - Update status to Completed if not already
        /// - Send email to patient
        /// - Return response
        /// For now, accept file as byte[] and filename, store locally
        /// </summary>
        Task<LabTestResponse> UploadLabTestResultAsync(Guid labTestId, byte[] fileContent, string fileName);

        /// <summary>
        /// TODO: Cancel lab test
        /// - Fetch test
        /// - Validate it's not already completed
        /// - Set status to Cancelled
        /// - Save changes
        /// - Return success
        /// </summary>
        Task<bool> CancelLabTestAsync(Guid labTestId);
    }
}