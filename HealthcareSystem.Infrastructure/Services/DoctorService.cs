using HealthcareSystem.Application.Dto.Doctor;
using HealthcareSystem.Application.DTOs.Doctor;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<DoctorService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<DoctorScheduleResponse> AddScheduleAsync(Guid doctorId, DoctorScheduleRequest request)
        {
            await CheckDoctorExistsAsync(doctorId);
            await CheckScheduleConflictAsync(doctorId, request);

            if (request.EndTime <= request.StartTime)
            {
                throw new ValidationException("End time must be greater than start time");
            }

            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                DayOfWeek = request.DayOfWeek,
                EndTime = request.EndTime,
                StartTime = request.StartTime,
                SlotDurationMinutes = request.SlotDurationMinutes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.DoctorSchedule.Add(schedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule created for Doctor {DoctorId} on {DayOfWeek}", doctorId, request.DayOfWeek);

            return MapToScheduleResponse(schedule);
        }

        private async Task CheckScheduleConflictAsync(Guid doctorId, DoctorScheduleRequest request)
        {
            var existingSchedules = await _context.DoctorSchedule
                .Where(s => s.DoctorId == doctorId && s.DayOfWeek == request.DayOfWeek && s.IsActive)
                .ToListAsync();

            foreach (var schedule in existingSchedules)
            {
                if (request.StartTime < schedule.EndTime && request.EndTime > schedule.StartTime)
                {
                    throw new ConflictException(
                        $"Schedule conflicts with existing schedule on {request.DayOfWeek} " +
                        $"from {schedule.StartTime:hh\\:mm} to {schedule.EndTime:hh\\:mm}");
                }
            }
        }

        public async Task<DoctorLeaveResponse> ApproveLeaveAsync(Guid leaveId, Guid approvedBy)
        {
            var leave = await _context.DoctorLeave
                .Include(l => l.Doctor)
                .ThenInclude(d => d.User)
                .Include(l => l.ApprovedByUser)
                .FirstOrDefaultAsync(l => l.Id == leaveId);

            if (leave == null)
            {
                throw new NotFoundException("DoctorLeave", leaveId);
            }

            if (leave.Status != LeaveStatus.Pending)
            {
                throw new BusinessException($"Leave request is already {leave.Status}. Only pending requests can be approved.");
            }

            leave.Status = LeaveStatus.Approved;
            leave.ApprovedBy = approvedBy;
            leave.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Leave {LeaveId} approved by {ApprovedBy}", leaveId, approvedBy);

            // Send approval email
            try
            {
                await _emailService.SendLeaveApprovedEmailAsync(
                    leave.Doctor.User.Email,
                    $"{leave.Doctor.User.FirstName} {leave.Doctor.User.LastName}",
                    leave.StartDate.ToString("MMMM dd, yyyy"),
                    leave.EndDate.ToString("MMMM dd, yyyy")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave approval email to {Email}", leave.Doctor.User.Email);
                // Don't throw - email failure shouldn't fail the operation
            }

            return MapToLeaveResponse(leave);
        }

        public async Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await GetUserByIdAsync(request.UserId);

                if (user.Doctor != null)
                {
                    throw new DuplicateException("Doctor profile already exists for this user");
                }

                await ValidateLicenseAsync(request.LicenseNumber);

                // Lock the Doctor table to prevent concurrent access
                var lastDoctor = await _context.Doctor
                    .FromSqlRaw("SELECT * FROM Doctors ORDER BY DoctorNumber DESC LIMIT 1 FOR UPDATE")
                    .FirstOrDefaultAsync();

                var doctorNumber = GenerateDoctorNumber(lastDoctor?.DoctorNumber);

                var doctor = new Doctor
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    DoctorNumber = doctorNumber,
                    LicenseNumber = request.LicenseNumber,
                    Qualification = request.Qualification,
                    ExperienceYears = request.ExperienceYears,
                    ConsultationFee = request.ConsultationFee,
                    Bio = request.Bio,
                    Specialization = request.Specialization,
                    IsAvailableForBooking = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Doctor.Add(doctor);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Doctor created with number {DoctorNumber} for user {UserId}",
                    doctorNumber, request.UserId);

                return await MapToDoctorResponse(doctor);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create doctor for user {UserId}", request.UserId);
                throw;
            }
        }

        private string GenerateDoctorNumber(string? lastDoctorNumber)
        {
            if (string.IsNullOrEmpty(lastDoctorNumber))
            {
                return "DR-001";
            }

            var parts = lastDoctorNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
            {
                return $"DR-{(lastNumber + 1):D3}";
            }

            _logger.LogWarning("Invalid doctor number format: {DoctorNumber}. Resetting to DR-001", lastDoctorNumber);
            return "DR-001";
        }

        private async Task<User> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            return user;
        }

        private async Task ValidateLicenseAsync(string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(licenseNumber))
            {
                throw new ValidationException("License number is required");
            }

            var exists = await _context.Doctor.AnyAsync(d => d.LicenseNumber == licenseNumber);
            if (exists)
            {
                throw new DuplicateException("Doctor", "LicenseNumber", licenseNumber);
            }
        }

        public async Task<bool> DeleteDoctorAsync(Guid doctorId)
        {
            var doctor = await _context.Doctor.FindAsync(doctorId);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", doctorId);
            }

            // Check if doctor has any appointments
            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled);

            if (hasAppointments)
            {
                throw new BusinessException("Cannot delete doctor with active appointments. Please cancel all appointments first.");
            }

            _context.Doctor.Remove(doctor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Doctor {DoctorId} deleted", doctorId);

            return true;
        }

        private async Task CheckDoctorExistsAsync(Guid doctorId)
        {
            var exists = await _context.Doctor.AnyAsync(d => d.Id == doctorId);
            if (!exists)
            {
                throw new NotFoundException("Doctor", doctorId);
            }
        }

        public async Task<bool> DeleteScheduleAsync(Guid scheduleId)
        {
            var schedule = await _context.DoctorSchedule.FindAsync(scheduleId);

            if (schedule == null)
            {
                throw new NotFoundException("DoctorSchedule", scheduleId);
            }

            schedule.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule {ScheduleId} marked as inactive", scheduleId);

            return true;
        }

        public async Task<List<DoctorResponse>> GetAllDoctorsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? specialization)
        {
            if (page < 1)
            {
                throw new ValidationException("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ValidationException("Page size must be between 1 and 100");
            }

            var query = _context.Doctor
                .Include(d => d.User)
                .Include(d => d.Schedules.Where(s => s.IsActive))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(d =>
                    d.DoctorNumber.Contains(searchTerm) ||
                    d.User.FirstName.Contains(searchTerm) ||
                    d.User.LastName.Contains(searchTerm) ||
                    d.Specialization.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(specialization))
            {
                query = query.Where(d => d.Specialization == specialization);
            }

            var doctors = await query
                .OrderBy(d => d.User.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappingTasks = doctors.Select(doctor => MapToDoctorResponse(doctor));
            var responses = await Task.WhenAll(mappingTasks);

            return responses.ToList();
        }

        public async Task<List<DoctorResponse>> GetAvailableDoctorsAsync(string? specialization)
        {
            var query = _context.Doctor
                .Include(d => d.User)
                .Include(d => d.Schedules.Where(s => s.IsActive))
                .Where(d => d.IsAvailableForBooking)
                .AsQueryable();

            if (!string.IsNullOrEmpty(specialization))
            {
                query = query.Where(d => d.Specialization == specialization);
            }

            var doctors = await query
                .OrderBy(d => d.Specialization)
                .ThenBy(d => d.User.FirstName)
                .ToListAsync();

            var mappingTasks = doctors.Select(doctor => MapToDoctorResponse(doctor));
            var responses = await Task.WhenAll(mappingTasks);

            return responses.ToList();
        }

        public async Task<List<TimeSlotResponse>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
        {
            await CheckDoctorExistsAsync(doctorId);

            if (date.Date < DateTime.UtcNow.Date)
            {
                throw new ValidationException("Cannot get slots for past dates");
            }

            var dayOfWeek = date.DayOfWeek;

            var schedule = await _context.DoctorSchedule
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek && s.IsActive);

            if (schedule == null)
            {
                return new List<TimeSlotResponse>();
            }

            var isOnLeave = await _context.DoctorLeave
                .AnyAsync(l => l.DoctorId == doctorId &&
                              l.Status == LeaveStatus.Approved &&
                              date.Date >= l.StartDate.Date &&
                              date.Date <= l.EndDate.Date);

            if (isOnLeave)
            {
                return new List<TimeSlotResponse>();
            }

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate.Date == date.Date &&
                           a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            var slots = new List<TimeSlotResponse>();
            var currentTime = schedule.StartTime;

            while (currentTime < schedule.EndTime)
            {
                var slotEnd = currentTime.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes));

                if (slotEnd > schedule.EndTime)
                    break;

                var isBooked = appointments.Any(a =>
                    a.StartTime < slotEnd && a.EndTime > currentTime);

                slots.Add(new TimeSlotResponse
                {
                    StartTime = currentTime,
                    EndTime = slotEnd,
                    IsAvailable = !isBooked
                });

                currentTime = slotEnd;
            }

            return slots;
        }

        public async Task<DoctorResponse> GetDoctorByIdAsync(Guid doctorId)
        {
            var doctor = await _context.Doctor
                .Include(d => d.User)
                .Include(d => d.Schedules.Where(s => s.IsActive))
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", doctorId);
            }

            return await MapToDoctorResponse(doctor);
        }

        public async Task<DoctorResponse> GetDoctorByNumberAsync(string doctorNumber)
        {
            if (string.IsNullOrWhiteSpace(doctorNumber))
            {
                throw new ValidationException("Doctor number is required");
            }

            var doctor = await _context.Doctor
                .Include(d => d.User)
                .Include(d => d.Schedules.Where(s => s.IsActive))
                .FirstOrDefaultAsync(d => d.DoctorNumber == doctorNumber);

            if (doctor == null)
            {
                throw new NotFoundException($"Doctor with number '{doctorNumber}' not found");
            }

            return await MapToDoctorResponse(doctor);
        }

        public async Task<List<DoctorLeaveResponse>> GetDoctorLeavesAsync(Guid doctorId)
        {
            await CheckDoctorExistsAsync(doctorId);

            var leaves = await _context.DoctorLeave
                .Include(l => l.Doctor)
                .ThenInclude(d => d.User)
                .Include(l => l.ApprovedByUser)
                .Where(l => l.DoctorId == doctorId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return leaves.Select(MapToLeaveResponse).ToList();
        }

        public async Task<List<DoctorScheduleResponse>> GetDoctorSchedulesAsync(Guid doctorId)
        {
            await CheckDoctorExistsAsync(doctorId);

            var schedules = await _context.DoctorSchedule
                .Where(s => s.DoctorId == doctorId && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ToListAsync();

            return schedules.Select(MapToScheduleResponse).ToList();
        }

        public async Task<List<DoctorLeaveResponse>> GetPendingLeavesAsync()
        {
            var leaves = await _context.DoctorLeave
                .Include(l => l.Doctor)
                .ThenInclude(d => d.User)
                .Where(l => l.Status == LeaveStatus.Pending)
                .OrderBy(l => l.StartDate)
                .ToListAsync();

            return leaves.Select(MapToLeaveResponse).ToList();
        }

        public async Task<DoctorLeaveResponse> RejectLeaveAsync(Guid leaveId, Guid rejectedBy)
        {
            var leave = await _context.DoctorLeave
                .Include(l => l.Doctor)
                .ThenInclude(d => d.User)
                .Include(l => l.ApprovedByUser)
                .FirstOrDefaultAsync(l => l.Id == leaveId);

            if (leave == null)
            {
                throw new NotFoundException("DoctorLeave", leaveId);
            }

            if (leave.Status != LeaveStatus.Pending)
            {
                throw new BusinessException($"Leave request is already {leave.Status}. Only pending requests can be rejected.");
            }

            leave.Status = LeaveStatus.Rejected;
            leave.ApprovedBy = rejectedBy;
            leave.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Leave {LeaveId} rejected by {RejectedBy}", leaveId, rejectedBy);

            try
            {
                await _emailService.SendLeaveRejectedEmailAsync(
                    leave.Doctor.User.Email,
                    $"{leave.Doctor.User.FirstName} {leave.Doctor.User.LastName}",
                    leave.StartDate.ToString("MMMM dd, yyyy"),
                    leave.EndDate.ToString("MMMM dd, yyyy"),
                    "Please contact administration for more details"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave rejection email to {Email}", leave.Doctor.User.Email);
            }

            return MapToLeaveResponse(leave);
        }

        public async Task<DoctorLeaveResponse> RequestLeaveAsync(DoctorLeaveRequest request)
        {
            if (request.StartDate < DateTime.UtcNow.Date)
            {
                throw new ValidationException("Leave start date cannot be in the past");
            }

            if (request.EndDate < request.StartDate)
            {
                throw new ValidationException("Leave end date must be after or equal to start date");
            }

            var doctor = await _context.Doctor
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", request.DoctorId);
            }

            // Check for overlapping leaves
            var overlappingLeave = await _context.DoctorLeave
                .AnyAsync(l => l.DoctorId == request.DoctorId &&
                              l.Status != LeaveStatus.Rejected &&
                              ((request.StartDate >= l.StartDate && request.StartDate <= l.EndDate) ||
                               (request.EndDate >= l.StartDate && request.EndDate <= l.EndDate) ||
                               (request.StartDate <= l.StartDate && request.EndDate >= l.EndDate)));

            if (overlappingLeave)
            {
                throw new ConflictException("Leave request overlaps with an existing leave");
            }

            var leave = new DoctorLeave
            {
                Id = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason,
                Status = LeaveStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.DoctorLeave.Add(leave);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Leave requested for Doctor {DoctorId} from {StartDate} to {EndDate}",
                request.DoctorId, request.StartDate, request.EndDate);

            return new DoctorLeaveResponse
            {
                Id = leave.Id,
                DoctorId = leave.DoctorId,
                DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}",
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                Status = leave.Status,
                CreatedAt = leave.CreatedAt
            };
        }

        public async Task<DoctorResponse> UpdateDoctorAsync(Guid doctorId, UpdateDoctorRequest request)
        {
            var doctor = await _context.Doctor
                .Include(d => d.User)
                .Include(d => d.Schedules.Where(s => s.IsActive))
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", doctorId);
            }

            if (request.Specialization != null)
                doctor.Specialization = request.Specialization;

            if (request.Qualification != null)
                doctor.Qualification = request.Qualification;

            if (request.ExperienceYears.HasValue)
            {
                if (request.ExperienceYears.Value < 0)
                {
                    throw new ValidationException("Experience years cannot be negative");
                }
                doctor.ExperienceYears = request.ExperienceYears;
            }

            if (request.ConsultationFee.HasValue)
            {
                if (request.ConsultationFee.Value < 0)
                {
                    throw new ValidationException("Consultation fee cannot be negative");
                }
                doctor.ConsultationFee = request.ConsultationFee.Value;
            }

            if (request.Bio != null)
                doctor.Bio = request.Bio;

            if (request.IsAvailableForBooking.HasValue)
                doctor.IsAvailableForBooking = request.IsAvailableForBooking.Value;

            doctor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Doctor {DoctorId} updated", doctorId);

            return await MapToDoctorResponse(doctor);
        }

        public async Task<DoctorResponse> GetDoctorByUserIdAsync(Guid userId)
        {
            var doctor = await _context.Doctor
                .Include(d => d.User)
                .Include(d => d.Schedules.Where(s => s.IsActive))
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
            {
                throw new NotFoundException($"No doctor found for user with ID '{userId}'");
            }

            return await MapToDoctorResponse(doctor);
        }

        // Helper Methods
        private async Task<DoctorResponse> MapToDoctorResponse(Doctor doctor)
        {
            return new DoctorResponse
            {
                Id = doctor.Id,
                UserId = doctor.UserId,
                DoctorNumber = doctor.DoctorNumber,
                Email = doctor.User.Email,
                FirstName = doctor.User.FirstName,
                LastName = doctor.User.LastName,
                PhoneNumber = doctor.User.PhoneNumber,
                Specialization = doctor.Specialization,
                LicenseNumber = doctor.LicenseNumber,
                Qualification = doctor.Qualification,
                ExperienceYears = doctor.ExperienceYears,
                ConsultationFee = doctor.ConsultationFee,
                Bio = doctor.Bio,
                IsAvailableForBooking = doctor.IsAvailableForBooking,
                CreatedAt = doctor.CreatedAt,
                Schedules = doctor.Schedules?.Select(MapToScheduleResponse).ToList()
                    ?? new List<DoctorScheduleResponse>()
            };
        }

        private DoctorScheduleResponse MapToScheduleResponse(DoctorSchedule schedule)
        {
            return new DoctorScheduleResponse
            {
                Id = schedule.Id,
                DoctorId = schedule.DoctorId,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                SlotDurationMinutes = schedule.SlotDurationMinutes,
                IsActive = schedule.IsActive
            };
        }

        private DoctorLeaveResponse MapToLeaveResponse(DoctorLeave leave)
        {
            return new DoctorLeaveResponse
            {
                Id = leave.Id,
                DoctorId = leave.DoctorId,
                DoctorName = $"{leave.Doctor.User.FirstName} {leave.Doctor.User.LastName}",
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                Status = leave.Status,
                CreatedAt = leave.CreatedAt,
                ApprovedByName = leave.ApprovedByUser != null
                    ? $"{leave.ApprovedByUser.FirstName} {leave.ApprovedByUser.LastName}"
                    : null,
                ApprovedAt = leave.ApprovedAt
            };
        }
    }
}