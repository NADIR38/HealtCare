using HealthcareSystem.Application.DTOs.Prescription;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PrescriptionService> _logger;
        private readonly DatabaseHelpers _helper;
        private readonly IEmailService _emailService;
        private readonly IPdfService _service;
        private readonly INotificationService _notificationService;
        public PrescriptionService(
            ApplicationDbContext context,
            ILogger<PrescriptionService> logger,
            DatabaseHelpers helper,
            IEmailService emailService,
            IPdfService service,INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _helper = helper;
            _emailService = emailService;
            _service = service;
            _notificationService = notificationService;
        }

        public async Task<PrescriptionResponse> CreatePrescriptionAsync(CreatePrescriptionRequest request)
        {
            // Validate medical record exists and load related data
            var medicalRecord = await _context.MedicalRecord
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == request.MedicalRecordId);

            if (medicalRecord == null)
            {
                throw new NotFoundException("Medical record", request.MedicalRecordId);
            }

            var patient = medicalRecord.Patient;
            var doctor = medicalRecord.Doctor;

            // Generate prescription number
            var prescriptionNumber = await GeneratePrescriptionNumberAsync();

            // Create prescription
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PrescriptionNumber = prescriptionNumber,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                MedicalRecordId = medicalRecord.Id,
                PrescriptionDate = DateTime.UtcNow,
                ValidUntil = request.ValidUntil,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
                
            };

            // Create prescription items
            var prescriptionItems = new List<PrescriptionItem>();
            foreach (var item in request.Items)
            {
                prescriptionItems.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    MedicineName = item.MedicineName,
                    Dosage = item.Dosage,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Quantity = item.Quantity,
                    Instructions = item.Instructions
                });
            }

            prescription.Items = prescriptionItems;

            // Save to database
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Prescription {PrescriptionNumber} created for Patient {PatientId} by Doctor {DoctorId}",
                prescriptionNumber, patient.Id, doctor.Id);
            try
            {
                var pdfBytes = await _service.GeneratePrescriptionPdfAsync(prescription.Id);

                // Send email with PDF attachment
                var emailMessage = new EmailMessage
                {
                    To = new List<string> { patient.User.Email },
                    Subject = "Your Prescription - Healthcare System",
                    Body = $@"
        <h2>Prescription Issued</h2>
        <p>Dear {patient.User.FirstName} {patient.User.LastName},</p>
        <p>Your prescription has been issued by Dr. {doctor.User.FirstName} {doctor.User.LastName}.</p>
        <p><strong>Prescription Number:</strong> {prescriptionNumber}</p>
        <p>Please find your prescription attached as a PDF.</p>
        <p>Best regards,<br/>Healthcare System</p>
    ",
                    IsHtml = true
                };

                await _emailService.SendEmailWithAttachmentAsync(
                    emailMessage,
                    pdfBytes,
                    $"prescription-{prescriptionNumber}.pdf"
                );
                await _notificationService.SendNotificationAsync(
    prescription.PatientId,
    NotificationType.PrescriptionIssued,
    "Prescription Issued",
    $"Dr. {doctor.User.FirstName} {doctor.User.LastName} has issued a new prescription for you",
    $"/prescriptions/{prescription.Id}",
    prescription.Id.ToString()
);
            }
           catch(Exception e)
            {
                _logger.LogError(e, "Failed to send prescription email for {PrescriptionNumber}. Prescription was created successfully.", prescriptionNumber);

            }

            // Reload with all data for response
            var createdPrescription = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.Items)
                .FirstAsync(p => p.Id == prescription.Id);

            return MapToPrescriptionResponse(createdPrescription);
        }

        public async Task<PrescriptionResponse> GetPrescriptionByIdAsync(Guid prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                throw new NotFoundException("Prescription", prescriptionId);
            }

            return MapToPrescriptionResponse(prescription);
        }

        public async Task<PrescriptionResponse> GetPrescriptionByNumberAsync(string prescriptionNumber)
        {
            if (string.IsNullOrWhiteSpace(prescriptionNumber))
            {
                throw new ValidationException("Prescription number is required");
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionNumber == prescriptionNumber);

            if (prescription == null)
            {
                throw new NotFoundException($"Prescription with number '{prescriptionNumber}' not found");
            }

            return MapToPrescriptionResponse(prescription);
        }

        public async Task<List<PrescriptionResponse>> GetPatientPrescriptionsAsync(Guid patientId)
        {
            // Validate patient exists
            var patient = await _helper.CheckPatientExist(patientId);

            // Fetch prescriptions
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.Items)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return prescriptions.Select(MapToPrescriptionResponse).ToList();
        }

        public async Task<List<PrescriptionResponse>> GetDoctorPrescriptionsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate)
        {
            // Validate doctor exists
            var doctor = await _helper.CheckDoctorExists(doctorId);

            // Build query
            var query = _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.Items)
                .Where(p => p.DoctorId == doctorId)
                .AsQueryable();

            // Apply date filters if provided
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.PrescriptionDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.PrescriptionDate <= toDate.Value);
            }

            var prescriptions = await query
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return prescriptions.Select(MapToPrescriptionResponse).ToList();
        }

        public async Task<byte[]> GeneratePrescriptionPdfAsync(Guid prescriptionId)
        {

           return await _service.GeneratePrescriptionPdfAsync(prescriptionId);
        }

        // Private helper methods

        private async Task<string> GeneratePrescriptionNumberAsync()
        {
            var year = DateTime.UtcNow.Year;

            var lastPrescription = await _context.Prescriptions
                .FromSqlRaw("SELECT * FROM Prescriptions WHERE PrescriptionNumber LIKE {0} ORDER BY PrescriptionNumber DESC LIMIT 1 FOR UPDATE",
                    $"RX-{year}-%")
                .FirstOrDefaultAsync();

            if (lastPrescription == null)
            {
                return $"RX-{year}-00001";
            }

            var parts = lastPrescription.PrescriptionNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                return $"RX-{year}-{(lastNumber + 1):D5}";
            }

            _logger.LogWarning("Invalid prescription number format: {PrescriptionNumber}. Resetting to RX-{Year}-00001",
                lastPrescription.PrescriptionNumber, year);
            return $"RX-{year}-00001";
        }

        private async Task SendPrescriptionEmailAsync(Prescription prescription, Patient patient, Doctor doctor)
        {
            var medicinesList = string.Join("\n", prescription.Items.Select(item =>
                $"• {item.MedicineName} - {item.Dosage}, {item.Frequency} for {item.Duration}"));

            var emailBody = $@"
        Dear {patient.User.FirstName} {patient.User.LastName},

        Your prescription has been issued by Dr. {doctor.User.FirstName} {doctor.User.LastName}.

        Prescription Number: {prescription.PrescriptionNumber}
        Date: {prescription.PrescriptionDate:MMMM dd, yyyy}
        Valid Until: {(prescription.ValidUntil.HasValue ? prescription.ValidUntil.Value.ToString("MMMM dd, yyyy") : "Not specified")}

        Medications:
        {medicinesList}

        {(string.IsNullOrEmpty(prescription.Notes) ? "" : $"Notes: {prescription.Notes}")}

        Please follow the prescribed medication schedule carefully.

        Best regards,
        Healthcare System";

            var message = new EmailMessage
            {
                To = new List<string> { patient.User.Email },
                Subject = "Prescription Issued",
                Body = emailBody
            };

            await _emailService.SendEmailAsync(message);
        }

        private PrescriptionResponse MapToPrescriptionResponse(Prescription prescription)
        {
            return new PrescriptionResponse
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,

                PatientId = prescription.PatientId,
                PatientName = $"{prescription.Patient.User.FirstName} {prescription.Patient.User.LastName}",

                DoctorId = prescription.DoctorId,
                DoctorName = $"Dr. {prescription.Doctor.User.FirstName} {prescription.Doctor.User.LastName}",

                PrescriptionDate = prescription.PrescriptionDate,
                ValidUntil = prescription.ValidUntil,
                Notes = prescription.Notes,

                Items = prescription.Items?.Select(item => new PrescriptionItemResponse
                {
                    Id = item.Id,
                    MedicineName = item.MedicineName,
                    Dosage = item.Dosage,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Quantity = item.Quantity,
                    Instructions = item.Instructions
                }).ToList() ?? new List<PrescriptionItemResponse>(),

                CreatedAt = prescription.CreatedAt
            };
        }
    }
}