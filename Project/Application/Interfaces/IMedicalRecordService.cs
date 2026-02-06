using HealthcareSystem.Application.Dto.MedicalRecords;
using HealthcareSystem.Application.DTOs.MedicalRecord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IMedicalRecordService
    {
        /// <summary>
        /// TODO: Create a new medical record
        /// - Validate patient and doctor exist
        /// - If appointmentId provided, validate it exists and belongs to patient/doctor
        /// - Calculate BMI if height and weight provided in vital signs
        /// - Create MedicalRecord entity
        /// - If VitalSigns provided, create VitalSigns entity
        /// - Save to database
        /// - Return mapped response
        /// </summary>
        Task<MedicalRecordResponse> CreateMedicalRecordAsync(CreateMedicalRecordRequest request);

        /// <summary>
        /// TODO: Get medical record by ID
        /// - Fetch medical record with all includes (Patient, Doctor, VitalSigns, Prescriptions, LabTests)
        /// - Throw NotFoundException if not found
        /// - Map to response
        /// </summary>
        Task<MedicalRecordResponse> GetMedicalRecordByIdAsync(Guid medicalRecordId);

        /// <summary>
        /// TODO: Get all medical records for a patient
        /// - Validate patient exists
        /// - Fetch all medical records for patient ordered by visit date descending
        /// - Include related entities
        /// - Map to list of responses
        /// </summary>
        Task<List<MedicalRecordResponse>> GetPatientMedicalRecordsAsync(Guid patientId);

        /// <summary>
        /// TODO: Get medical records by doctor
        /// - Validate doctor exists
        /// - Apply date filter if provided
        /// - Fetch records with pagination
        /// - Map to responses
        /// </summary>
        Task<List<MedicalRecordResponse>> GetDoctorMedicalRecordsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// TODO: Update medical record
        /// - Fetch existing record
        /// - Validate it's not too old (e.g., can only edit within 30 days)
        /// - Update editable fields
        /// - Save changes
        /// - Return updated response
        /// </summary>
        Task<MedicalRecordResponse> UpdateMedicalRecordAsync(Guid medicalRecordId, UpdateMedicalRecordRequest request);

        /// <summary>
        /// TODO: Add or update vital signs for a medical record
        /// - Fetch medical record
        /// - Calculate BMI if height and weight provided
        /// - If vital signs exist, update them; otherwise create new
        /// - Save changes
        /// - Return updated medical record response
        /// </summary>
        Task<MedicalRecordResponse> AddOrUpdateVitalSignsAsync(Guid medicalRecordId, VitalSignsRequest request);

        /// <summary>
        /// TODO: Delete medical record (soft delete or hard delete - your choice)
        /// - Fetch record
        /// - Check if it has prescriptions or lab tests (maybe prevent deletion)
        /// - Delete or mark as deleted
        /// - Return success
        /// </summary>
        Task<bool> DeleteMedicalRecordAsync(Guid medicalRecordId);
    }
}