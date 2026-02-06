using HealthcareSystem.Application.Dto.Patient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IPatientService
    {
        Task<PatientResponse> CreatePatientAsync(CreatePatientRequest request);
        Task<PatientResponse> GetPatientByIdAsync(Guid patientId);
        Task<PatientResponse> GetPatientByUserIdAsync(Guid userId);
        Task<PatientResponse> GetPatientByNumberAsync(string patientNumber);
        Task<List<PatientResponse>> GetAllPatientsAsync(int page, int pageSize, string? searchTerm);
        Task<PatientResponse> UpdatePatientAsync(Guid patientId, UpdatePatientRequest request);
        Task<bool> DeletePatientAsync(Guid patientId);

        Task<MedicalHistoryResponse> CreateOrUpdateMedicalHistoryAsync(Guid patientId, MedicalHistoryRequest request);
        Task<MedicalHistoryResponse> GetMedicalHistoryAsync(Guid patientId);
    }
}
