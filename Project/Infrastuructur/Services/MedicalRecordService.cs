using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HealthcareSystem.Application.Dto.MedicalRecords;
using HealthcareSystem.Application.DTOs.MedicalRecord;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthcareSystem.Infrastructure.Services
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MedicalRecordService> _logger;
        private readonly DatabaseHelpers _helper;
        public MedicalRecordService(ApplicationDbContext context, ILogger<MedicalRecordService> logger,DatabaseHelpers helper)
        {
            _context = context;
            _logger = logger;
            _helper= helper;
        }
        public async Task<MedicalRecordResponse> AddOrUpdateVitalSignsAsync(Guid medicalRecordId, VitalSignsRequest request)
        {
            var medicalRecord = await _context.MedicalRecord
                .Include(m => m.VitalSigns)  // ✅ Include VitalSigns
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Include(m => m.Prescriptions)
                .Include(m => m.LabTests)
                .Include(m => m.Appointment)
                .FirstOrDefaultAsync(m => m.Id == medicalRecordId);

            if (medicalRecord == null)
            {
                throw new NotFoundException("Medical record", medicalRecordId);
            }

            if (medicalRecord.VitalSigns == null)
            {
                medicalRecord.VitalSigns = new VitalSigns
                {
                    Id = Guid.NewGuid(),
                    MedicalRecordId = medicalRecordId,
                    RecordedAt = DateTime.UtcNow
                };
            }

            // Update vital signs
            medicalRecord.VitalSigns.BloodPressureSystolic = request.BloodPressureSystolic;
            medicalRecord.VitalSigns.BloodPressureDiastolic = request.BloodPressureDiastolic;
            medicalRecord.VitalSigns.Temperature = request.Temperature;
            medicalRecord.VitalSigns.HeartRate = request.HeartRate;
            medicalRecord.VitalSigns.RespiratoryRate = request.RespiratoryRate;
            medicalRecord.VitalSigns.OxygenSaturation = request.OxygenSaturation;
            medicalRecord.VitalSigns.Weight = request.Weight;
            medicalRecord.VitalSigns.Height = request.Height;
            medicalRecord.VitalSigns.Notes = request.Notes;

            if (request.Height.HasValue && request.Weight.HasValue &&
                request.Height.Value > 0 && request.Weight.Value > 0)
            {
                medicalRecord.VitalSigns.BMI = CalculateBMI(request.Height.Value, request.Weight.Value);
            }

            medicalRecord.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MaptoMedicalRecordResponse(medicalRecord);
        }

        public async Task<MedicalRecordResponse> CreateMedicalRecordAsync(CreateMedicalRecordRequest request)
        {
            var patient = await _helper.CheckPatientExist(request.PatientId);
            var doctor = await _helper.CheckDoctorExists(request.DoctorId);
            VitalSigns vitalSigns = null;

            if (request.VitalSigns != null)
            {
                vitalSigns = new VitalSigns
                {
                    Id = Guid.NewGuid(),
                    BloodPressureSystolic = request.VitalSigns.BloodPressureSystolic,
                    BloodPressureDiastolic = request.VitalSigns.BloodPressureDiastolic,
                    Temperature = request.VitalSigns.Temperature,
                    HeartRate = request.VitalSigns.HeartRate,
                    RespiratoryRate = request.VitalSigns.RespiratoryRate,
                    OxygenSaturation = request.VitalSigns.OxygenSaturation,
                    Weight = request.VitalSigns.Weight,
                    Height = request.VitalSigns.Height,
                    Notes = request.VitalSigns.Notes,
                    RecordedAt = DateTime.UtcNow
                };

                if (request.VitalSigns.Height > 0 && request.VitalSigns.Weight > 0)
                {
                    vitalSigns.BMI = CalculateBMI(request.VitalSigns.Height.Value, request.VitalSigns.Weight.Value);
                }
            }
            Appointment appointment = null;
            if (request.AppointmentId.HasValue)
            {
                appointment = await ValidateAppointment(request.AppointmentId.Value, request.DoctorId, request.PatientId);
            }
            var medicalrecord = new MedicalRecord {
                Id=Guid.NewGuid(),
            PatientId= request.PatientId,
            DoctorId=request.DoctorId,
            AppointmentId=request.AppointmentId,
            VisitDate=request.VisitDate,
            VitalSigns=vitalSigns,
            ChiefComplaint=request.ChiefComplaint,
            Diagnosis=request.Diagnosis,
            PhysicalExamination=request.PhysicalExamination,
            TreatmentPlan=request.TreatmentPlan,
            Notes=request.Notes,
            Symptoms=request.Symptoms,
            CreatedAt=DateTime.UtcNow,
            UpdatedAt=DateTime.UtcNow
            
           

            
            };
            _context.MedicalRecord.Add(medicalrecord);
            await _context.SaveChangesAsync();

            var createdRecord = await _context.MedicalRecord
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)
                .Include(m => m.VitalSigns)
                .Include(m => m.Appointment)
                .Include(m => m.Prescriptions)
                .Include(m => m.LabTests)
                .FirstAsync(m => m.Id == medicalrecord.Id);

            return MaptoMedicalRecordResponse(createdRecord);





        }

        private MedicalRecordResponse MaptoMedicalRecordResponse(MedicalRecord medicalrecord)
        {
            string appointmentNumber = medicalrecord.Appointment?.AppointmentNumber;

            var response = new MedicalRecordResponse
            {
                Id = medicalrecord.Id,
                PatientId = medicalrecord.PatientId,
                PatientName = $"{medicalrecord.Patient.User.FirstName} {medicalrecord.Patient.User.LastName}",
                PatientNumber = medicalrecord.Patient.PatientNumber,

                DoctorId = medicalrecord.DoctorId,
                DoctorName = $"Dr. {medicalrecord.Doctor.User.FirstName} {medicalrecord.Doctor.User.LastName}",
                DoctorSpecialization = medicalrecord.Doctor.Specialization,

                AppointmentId = medicalrecord.AppointmentId,
                AppointmentNumber = appointmentNumber,

                VisitDate = medicalrecord.VisitDate,
                ChiefComplaint = medicalrecord.ChiefComplaint,
                Symptoms = medicalrecord.Symptoms,
                Diagnosis = medicalrecord.Diagnosis,
                PhysicalExamination = medicalrecord.PhysicalExamination,
                TreatmentPlan = medicalrecord.TreatmentPlan,
                Notes = medicalrecord.Notes,

                CreatedAt = medicalrecord.CreatedAt,
                UpdatedAt = medicalrecord.UpdatedAt,

                VitalSigns = medicalrecord.VitalSigns != null ? new VitalSignsResponse
                {
                    Id = medicalrecord.VitalSigns.Id,
                    BloodPressure = $"{medicalrecord.VitalSigns.BloodPressureSystolic}/{medicalrecord.VitalSigns.BloodPressureDiastolic}",
                    Temperature = medicalrecord.VitalSigns.Temperature,
                    HeartRate = medicalrecord.VitalSigns.HeartRate,
                    RespiratoryRate = medicalrecord.VitalSigns.RespiratoryRate,
                    OxygenSaturation = medicalrecord.VitalSigns.OxygenSaturation,
                    Weight = medicalrecord.VitalSigns.Weight,
                    Height = medicalrecord.VitalSigns.Height,
                    BMI = medicalrecord.VitalSigns.BMI,
                    Notes = medicalrecord.VitalSigns.Notes,
                    RecordedAt = medicalrecord.VitalSigns.RecordedAt
                } : null,

                Prescriptions = medicalrecord.Prescriptions?.Select(p => new PrescriptionSummary
                {
                    Id = p.Id,
                    PrescriptionNumber = p.PrescriptionNumber,
                    PrescriptionDate = p.PrescriptionDate,
                    ItemCount = p.Items?.Count ?? 0
                }).ToList() ?? new List<PrescriptionSummary>(),

                LabTests = medicalrecord.LabTests?.Select(l => new LabTestSummary
                {
                    Id = l.Id,
                    TestName = l.TestName,
                    Status = l.Status.ToString(),
                    OrderedDate = l.OrderedDate
                }).ToList() ?? new List<LabTestSummary>()
            };

            return response;
        }

        private async Task<Appointment> ValidateAppointment(Guid appointmentId, Guid doctorId, Guid patientId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId &&
                                         a.PatientId == patientId &&
                                         a.DoctorId == doctorId);

            if (appointment == null)
            {
                throw new NotFoundException("Valid appointment not found for the specified patient and doctor.", appointmentId);
            }

            return appointment;
        }

        public async Task<bool> DeleteMedicalRecordAsync(Guid medicalRecordId)
        {
            var medicalRecord = await _context.MedicalRecord
                .Include(m => m.Prescriptions)
                .Include(m => m.LabTests)
                .FirstOrDefaultAsync(m => m.Id == medicalRecordId);

            if (medicalRecord == null)
            {
                throw new NotFoundException("Medical record", medicalRecordId);
            }

            if (medicalRecord.Prescriptions?.Any() == true || medicalRecord.LabTests?.Any() == true)  
            {
                throw new BusinessException("Cannot delete medical record with associated prescriptions or lab tests");
            }

            _context.MedicalRecord.Remove(medicalRecord);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MedicalRecordResponse>> GetDoctorMedicalRecordsAsync(Guid doctorId, DateTime? fromDate, DateTime? toDate)
        {
            var doctor = await _helper.CheckDoctorExists(doctorId);

            var query = _context.MedicalRecord
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User) 
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)  
                .Include(m => m.VitalSigns)    
                .Include(m => m.Appointment)  
                .Include(m => m.Prescriptions)
                .Include(m => m.LabTests)
                .Where(m => m.DoctorId == doctorId)
                .AsQueryable();

            if (fromDate != null && toDate != null)
            {
                query = query.Where(m => m.CreatedAt > fromDate && m.CreatedAt < toDate);
            }

            var medicalRecords = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

            var response = new List<MedicalRecordResponse>();
            foreach (var item in medicalRecords)
            {
                response.Add(MaptoMedicalRecordResponse(item));
            }
            return response;
        }


        public async Task<MedicalRecordResponse> GetMedicalRecordByIdAsync(Guid medicalRecordId)
        {
            var medicalRecord=await _context.MedicalRecord.Include(u=>u.Doctor).Include(p=>p.Patient).Include(p=>p.Prescriptions).Include(l=>l.LabTests).FirstOrDefaultAsync(u=>u.Id==medicalRecordId);
            if (medicalRecord == null)
            {
                throw new NotFoundException("No record with this Id", medicalRecordId);
            }
            return MaptoMedicalRecordResponse(medicalRecord);
        }

        public async Task<List<MedicalRecordResponse>> GetPatientMedicalRecordsAsync(Guid patientId)
        {
            var patient = await _helper.CheckPatientExist(patientId);

            var medicalRecords = await _context.MedicalRecord
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)  
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)  
                .Include(m => m.VitalSigns)    
                .Include(m => m.Appointment)   
                .Include(m => m.Prescriptions)
                .Include(m => m.LabTests)
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var response = new List<MedicalRecordResponse>();
            foreach (var item in medicalRecords)
            {
                response.Add(MaptoMedicalRecordResponse(item));
            }
            return response;
        }
        public async Task<MedicalRecordResponse> UpdateMedicalRecordAsync(Guid medicalRecordId, UpdateMedicalRecordRequest request)
        {
            var medicalRecord = await _context.MedicalRecord
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)  
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)  
                .Include(m => m.VitalSigns)    
                .Include(m => m.Appointment)   
                .Include(m => m.Prescriptions)
                .Include(m => m.LabTests)      
                .FirstOrDefaultAsync(m => m.Id == medicalRecordId);

            if (medicalRecord == null)
            {
                throw new NotFoundException("Medical record", medicalRecordId);
            }

            if ((DateTime.UtcNow.Date - medicalRecord.CreatedAt.Date).TotalDays >= 30)
            {
                throw new BusinessException("Medical records older than 30 days cannot be updated");
            }

            medicalRecord.Notes = request.Notes;
            medicalRecord.ChiefComplaint = request.ChiefComplaint;
            medicalRecord.TreatmentPlan = request.TreatmentPlan;
            medicalRecord.UpdatedAt = DateTime.UtcNow;
            medicalRecord.Diagnosis = request.Diagnosis;
            medicalRecord.PhysicalExamination = request.PhysicalExamination;
            medicalRecord.Symptoms = request.Symptoms;

            await _context.SaveChangesAsync();

            return MaptoMedicalRecordResponse(medicalRecord);
        }
        private decimal CalculateBMI(decimal heightCm, decimal weightKg)
        {
            if (heightCm <= 0 || weightKg <= 0) return 0;

            var heightInMeters = heightCm / 100;
            var bmi = weightKg / (heightInMeters * heightInMeters);

            return Math.Round(bmi, 2);
        }
    }
}
