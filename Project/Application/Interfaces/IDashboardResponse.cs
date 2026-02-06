using HealthcareSystem.Application.DTOs.Dashboard;
using System;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardResponse> GetAdminDashboardAsync();
        Task<DoctorDashboardResponse> GetDoctorDashboardAsync(Guid doctorId);
        Task<PatientDashboardResponse> GetPatientDashboardAsync(Guid patientId);
    }
}