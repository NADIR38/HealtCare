using HealthcareSystem.Application.DTOs.Prescription;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IPrescriptionService
    {
        /// <summary>
        /// TODO: Create a prescription
        /// - Validate medical record exists
        /// - Get patient and doctor from medical record
        /// - Generate prescription number (RX-YYYY-00001)
        /// - Create prescription entity
        /// - Create prescription items
        /// - Save to database
        /// - Send email to patient with prescription details
        /// - Return response
        /// </summary>
        Task<PrescriptionResponse> CreatePrescriptionAsync(CreatePrescriptionRequest request);

        /// <summary>
        /// TODO: Get prescription by ID
        /// - Fetch with includes (Patient, Doctor, Items)
        /// - Throw NotFoundException if not found
        /// - Map to response
        /// </summary>
        Task<PrescriptionResponse> GetPrescriptionByIdAsync(Guid prescriptionId);

        /// <summary>
        /// TODO: Get prescription by number
        /// - Search by prescription number
        /// - Include all related entities
        /// - Map to response
        /// </summary>
        Task<PrescriptionResponse> GetPrescriptionByNumberAsync(string prescriptionNumber);

        /// <summary>
        /// TODO: Get all prescriptions for a patient
        /// - Validate patient exists
        /// - Fetch prescriptions ordered by date descending
        /// - Map to responses
        /// </summary>
        Task<List<PrescriptionResponse>> GetPatientPrescriptionsAsync(Guid patientId);

        /// <summary>
        /// TODO: Get prescriptions by doctor
        /// - Validate doctor exists
        /// - Apply date filters if provided
        /// - Fetch with pagination
        /// - Map to responses
        /// </summary>
        Task<List<PrescriptionResponse>> GetDoctorPrescriptionsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// TODO: Generate prescription PDF
        /// - Fetch prescription with all details
        /// - Use a PDF library (we'll add this on Day 7)
        /// - Generate PDF with prescription details
        /// - Return PDF as byte array
        /// For now, just throw NotImplementedException - we'll implement on Day 7
        /// </summary>
        Task<byte[]> GeneratePrescriptionPdfAsync(Guid prescriptionId);
        Task<List<PrescriptionResponse>> GetAllPrescriptionsAsync(int page, int pageSize, string? search);
    }
}