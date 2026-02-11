using HealthcareSystem.Application.Dto.Patient;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<MedicalHistoryResponse> CreateOrUpdateMedicalHistoryAsync(Guid patientId, MedicalHistoryRequest request)
        {
            var patient = await _context.Patients
    .Include(p => p.MedicalHistory)
    .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                throw new Exception("Patient not found");
            }

            if (patient.MedicalHistory == null)
            {
                // Create new medical history
                var medicalHistory = new MedicalHistory
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    ChronicConditions = request.ChronicConditions,
                    Allergies = request.Allergies,
                    PastSurgeries = request.PastSurgeries,
                    FamilyHistory = request.FamilyHistory,
                    CurrentMedications = request.CurrentMedications,
                    SmokingStatus = request.SmokingStatus,
                    AlcoholConsumption = request.AlcoholConsumption,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MedicalHistory.Add(medicalHistory);
                await _context.SaveChangesAsync();

                return MapToMedicalHistoryResponse(medicalHistory);
            }
            else
            {
                // Update existing medical history
                patient.MedicalHistory.ChronicConditions = request.ChronicConditions;
                patient.MedicalHistory.Allergies = request.Allergies;
                patient.MedicalHistory.PastSurgeries = request.PastSurgeries;
                patient.MedicalHistory.FamilyHistory = request.FamilyHistory;
                patient.MedicalHistory.CurrentMedications = request.CurrentMedications;
                patient.MedicalHistory.SmokingStatus = request.SmokingStatus;
                patient.MedicalHistory.AlcoholConsumption = request.AlcoholConsumption;
                patient.MedicalHistory.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return MapToMedicalHistoryResponse(patient.MedicalHistory);
            }

        }

        public async Task<PatientResponse> CreatePatientAsync(CreatePatientRequest request)
        {
            var user=await _context.Users.Include(u=>u.Patient).FirstOrDefaultAsync(u=>u.Id==request.UserId);
            if (user == null)
            {
                throw new Exception("User doesnot exists");
            }
            if (user.Patient != null)
            {
                throw new Exception("Patient Record Already Exists");
            }
            var year=DateTime.UtcNow.Year;
            var lastPatient = await _context.Patients
                        .Where(p => p.PatientNumber.StartsWith($"PT-{year}-"))
                        .OrderByDescending(p => p.PatientNumber)
                        .FirstOrDefaultAsync();
            int nextNumber = 1;
            if (lastPatient != null)
            {
                var lastNumberStr = lastPatient.PatientNumber.Split('-').Last();
                nextNumber = int.Parse(lastNumberStr) + 1;
            }
            var patientNumber = $"PT-{year}-{nextNumber:D4}";
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                UserId= request.UserId,
                Address= request.Address,
                BloodGroup= request.BloodGroup,
                Height= request.Height,
                EmergencyContactName= request.EmergencyContactName,
                EmergencyContactPhone= request.EmergencyContactPhone,
                EmergencyContactRelation= request.EmergencyContactRelation,
                PatientNumber = patientNumber,
                Weight = request.Weight,
                City = request.City,
                ZipCode= request.ZipCode,
                State=request.State,
                InsurancePolicyNumber= request.InsurancePolicyNumber,
                InsuranceProvider= request.InsuranceProvider,
                CreatedAt=DateTime.UtcNow,
                UpdatedAt=DateTime.UtcNow
              


                
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return await MapToPatientResponse(patient);

        }

        public async Task<bool> DeletePatientAsync(Guid patientId)
        {
            var patient= await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null)
            {
                throw new Exception("no Patient with this Id");
            }
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return true;    
        }

        public async Task<List<PatientResponse>> GetAllPatientsAsync(int page, int pageSize, string? searchTerm)
        {
            var query = _context.Patients.Include(u => u.User).AsQueryable();
            if(!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.PatientNumber.Contains(searchTerm) ||
                    p.User.FirstName.Contains(searchTerm) ||
                    p.User.LastName.Contains(searchTerm) ||
                    p.User.Email.Contains(searchTerm));
            }
            var patients=await query.OrderByDescending(p=>p.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
            var response = new List<PatientResponse>();
            foreach (var item in patients)
            {
                response.Add(await MapToPatientResponse(item));
            }
            return response;
        }

        public async Task<MedicalHistoryResponse> GetMedicalHistoryAsync(Guid patientId)
        {
            var history=await _context.MedicalHistory.Include(p=>p.Patient).FirstOrDefaultAsync(u=>u.PatientId==patientId);
            if (history == null)
            {
                throw new KeyNotFoundException($"No medical history found for Patient ID: {patientId}");
            }
            return  MapToMedicalHistoryResponse (history);
        }

        public async Task<PatientResponse> GetPatientByIdAsync(Guid patientId)
        {
            var patient=_context.Patients.Include(p=>p.User).FirstOrDefault(u=>u.Id==patientId);
            if (patient == null)
            {
                throw new Exception("No patient");
            }
            return await MapToPatientResponse(patient);
        }

        public async Task<PatientResponse> GetPatientByNumberAsync(string patientNumber)
        {
           var patient =_context.Patients.Include(u=>u.User).FirstOrDefault(u=>u.PatientNumber==patientNumber);
            if (patient == null)
            {
                throw new Exception("No patient with this Number");
            }
            return await MapToPatientResponse(patient);
        }

        public async Task<PatientResponse> GetPatientByUserIdAsync(Guid userId)
        {
            var patient=_context.Patients.Include(p=>p.User).FirstOrDefault(p=>p.UserId == userId);
            if (patient == null)
            {
                throw new Exception("No patient with this ID");
            }
            return await MapToPatientResponse(patient);
        }

        public async Task<PatientResponse> UpdatePatientAsync(Guid patientId, UpdatePatientRequest request)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                throw new Exception("Patient not found");
            }

            // Update BloodGroup (convert string to enum)
            if (!string.IsNullOrEmpty(request.BloodGroup))
            {
                if (Enum.TryParse<BloodGroup>(request.BloodGroup.Replace("+", "Plus").Replace("-", "Minus"), out var bloodGroupEnum))
                {
                    patient.BloodGroup = bloodGroupEnum;
                }
            }

            // Update other fields only if they're provided
            if (request.Height.HasValue)
                patient.Height = request.Height.Value;

            if (request.Weight.HasValue)
                patient.Weight = request.Weight.Value;

            if (!string.IsNullOrEmpty(request.EmergencyContactName))
                patient.EmergencyContactName = request.EmergencyContactName;

            if (!string.IsNullOrEmpty(request.EmergencyContactPhone))
                patient.EmergencyContactPhone = request.EmergencyContactPhone;

            if (!string.IsNullOrEmpty(request.EmergencyContactRelation))
                patient.EmergencyContactRelation = request.EmergencyContactRelation;

            if (!string.IsNullOrEmpty(request.Address))
                patient.Address = request.Address;

            if (!string.IsNullOrEmpty(request.City))
                patient.City = request.City;

            if (!string.IsNullOrEmpty(request.State))
                patient.State = request.State;

            if (!string.IsNullOrEmpty(request.ZipCode))
                patient.ZipCode = request.ZipCode;

            if (!string.IsNullOrEmpty(request.InsuranceProvider))
                patient.InsuranceProvider = request.InsuranceProvider;

            if (!string.IsNullOrEmpty(request.InsurancePolicyNumber))
                patient.InsurancePolicyNumber = request.InsurancePolicyNumber;

            patient.UpdatedAt = DateTime.UtcNow;

            // Update or create medical history if allergies or chronic conditions provided
            if (!string.IsNullOrEmpty(request.Allergies) || !string.IsNullOrEmpty(request.ChronicConditions))
            {
                var medicalHistory = await _context.MedicalHistory
                    .FirstOrDefaultAsync(m => m.PatientId == patientId);

                if (medicalHistory == null)
                {
                    // Create new medical history
                    medicalHistory = new MedicalHistory
                    {
                        Id = Guid.NewGuid(),
                        PatientId = patientId,
                        ChronicConditions = !string.IsNullOrEmpty(request.ChronicConditions)
                            ? request.ChronicConditions.Split(',').Select(c => c.Trim()).ToList()
                            : new List<string>(),
                        Allergies = !string.IsNullOrEmpty(request.Allergies)
                            ? request.Allergies.Split(',').Select(a => a.Trim()).ToList()
                            : new List<string>(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.MedicalHistory.Add(medicalHistory);
                }
                else
                {
                    // Update existing medical history
                    if (!string.IsNullOrEmpty(request.ChronicConditions))
                    {
                        medicalHistory.ChronicConditions = request.ChronicConditions.Split(',').Select(c => c.Trim()).ToList();
                    }

                    if (!string.IsNullOrEmpty(request.Allergies))
                    {
                        medicalHistory.Allergies = request.Allergies.Split(',').Select(a => a.Trim()).ToList();
                    }

                    medicalHistory.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return await MapToPatientResponse(patient);
        }

        private async Task<PatientResponse> MapToPatientResponse(Domain.Entities.Patient patient)
        {
            // Get medical history if exists
            var medicalHistory = await _context.MedicalHistory
                .FirstOrDefaultAsync(m => m.PatientId == patient.Id);

            return new PatientResponse
            {
                Id = patient.Id,
                UserId = patient.UserId,
                PatientNumber = patient.PatientNumber,
                Email = patient.User.Email,
                FirstName = patient.User.FirstName,
                LastName = patient.User.LastName,
                PhoneNumber = patient.User.PhoneNumber,
                DateOfBirth = patient.User.DateOfBirth,
                Gender = patient.User.Gender,
                BloodGroup = patient.BloodGroup?.ToString().Replace("Plus", "+").Replace("Minus", "-"),
                Height = patient.Height,
                Weight = patient.Weight,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactRelation = patient.EmergencyContactRelation,
                Address = patient.Address,
                City = patient.City,
                State = patient.State,
                ZipCode = patient.ZipCode,
                InsuranceProvider = patient.InsuranceProvider,
                InsurancePolicyNumber = patient.InsurancePolicyNumber,
                Allergies = medicalHistory != null ? string.Join(", ", medicalHistory.Allergies) : null,
                ChronicConditions = medicalHistory != null ? string.Join(", ", medicalHistory.ChronicConditions) : null,
                CreatedAt = patient.CreatedAt
            };
        }

        private MedicalHistoryResponse MapToMedicalHistoryResponse(MedicalHistory medicalHistory)
        {
            return new MedicalHistoryResponse
            {
                Id = medicalHistory.Id,
                PatientId = medicalHistory.PatientId,
                ChronicConditions = medicalHistory.ChronicConditions,
                Allergies = medicalHistory.Allergies,
                PastSurgeries = medicalHistory.PastSurgeries,
                FamilyHistory = medicalHistory.FamilyHistory,
                CurrentMedications = medicalHistory.CurrentMedications,
                SmokingStatus = medicalHistory.SmokingStatus,
                AlcoholConsumption = medicalHistory.AlcoholConsumption,
                CreatedAt = medicalHistory.CreatedAt,
                UpdatedAt = medicalHistory.UpdatedAt
            };
        }
    }
}
