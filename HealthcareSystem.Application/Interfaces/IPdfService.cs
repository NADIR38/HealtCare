using System;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IPdfService
    {
        /// <summary>
        /// Generate prescription PDF with doctor details, patient info, and medications
        /// </summary>
        Task<byte[]> GeneratePrescriptionPdfAsync(Guid prescriptionId);

        /// <summary>
        /// Generate medical report PDF with patient history, vital signs, diagnosis
        /// </summary>
        Task<byte[]> GenerateMedicalReportPdfAsync(Guid medicalRecordId);

        /// <summary>
        /// Generate invoice PDF with line items and payment details
        /// (Will implement on Day 11)
        /// </summary>
        Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);

        /// <summary>
        /// Generate lab test report PDF
        /// </summary>
        Task<byte[]> GenerateLabTestReportPdfAsync(Guid labTestId);
    }
}