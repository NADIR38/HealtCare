using HealthcareSystem.Application.Dto.Doctor;
using HealthcareSystem.Application.DTOs.Doctor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IDoctorService
    {
        Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request);
        Task<DoctorResponse> GetDoctorByIdAsync(Guid doctorId);
        Task<DoctorResponse> GetDoctorByUserIdAsync(Guid userId);
        Task<DoctorResponse> GetDoctorByNumberAsync(string doctorNumber);
        Task<List<DoctorResponse>> GetAllDoctorsAsync(int page, int pageSize, string? searchTerm, string? specialization);
        Task<List<DoctorResponse>> GetAvailableDoctorsAsync(string? specialization);
        Task<DoctorResponse> UpdateDoctorAsync(Guid doctorId, UpdateDoctorRequest request);
        Task<bool> DeleteDoctorAsync(Guid doctorId);

        Task<DoctorScheduleResponse> AddScheduleAsync(Guid doctorId, DoctorScheduleRequest request);
        Task<List<DoctorScheduleResponse>> GetDoctorSchedulesAsync(Guid doctorId);
        Task<bool> DeleteScheduleAsync(Guid scheduleId);
        Task<List<TimeSlotResponse>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);

        Task<DoctorLeaveResponse> RequestLeaveAsync(DoctorLeaveRequest request);
        Task<DoctorLeaveResponse> ApproveLeaveAsync(Guid leaveId, Guid approvedBy);
        Task<DoctorLeaveResponse> RejectLeaveAsync(Guid leaveId, Guid rejectedBy);
        Task<List<DoctorLeaveResponse>> GetDoctorLeavesAsync(Guid doctorId);
        Task<List<DoctorLeaveResponse>> GetPendingLeavesAsync();
    }
}
