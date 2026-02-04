using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Helpers
{
    public class DatabaseHelpers
    {
        private readonly ApplicationDbContext _context;

        public DatabaseHelpers(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<T> CheckEntityExists<T>(Guid Id) where T : class
        {
            var entity = await _context.Set<T>().FindAsync(Id);
            if (entity == null)
            {
                throw new NotFoundException(typeof(T).Name, Id);
            }
            return entity;
        }

        public async Task<Patient> CheckPatientExist(Guid Id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(d => d.Id == Id);

            if (patient == null)
            {
                throw new NotFoundException("Patient", Id);
            }
            return patient;
        }

        public async Task<bool> CheckDoctorOnLeave(Guid Id, DateTime StartDate)
        {
            var isOnLeave = await _context.DoctorLeave.AnyAsync(
                l => l.DoctorId == Id &&
                l.Status == LeaveStatus.Approved &&
                StartDate >= l.StartDate.Date &&
                StartDate <= l.EndDate.Date);

            return isOnLeave;
        }

        public async Task<Doctor> DoctorExistsAndAvailable(Guid Id)
        {
            var doctor = await _context.Doctor
                .Include(d => d.User)
                .Include(s => s.Schedules.Where(s => s.IsActive))
                .FirstOrDefaultAsync(d => d.Id == Id);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", Id);
            }
            return doctor;
        }

        public async Task<Doctor> CheckDoctorExists(Guid doctorId)
        {
            var doctor = await _context.Doctor
                .Include(u => u.User)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                throw new NotFoundException("Doctor", doctorId);
            }
            return doctor;
        }

        public async Task<MedicalRecord> GetMedicalRecord(Guid medicalRecordId)
        {
            var medicalRecord = await _context.MedicalRecord
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)
                .Include(m => m.VitalSigns)
                .Include(m => m.Appointment)
                .FirstOrDefaultAsync(m => m.Id == medicalRecordId);

            if (medicalRecord == null)
            {
                throw new NotFoundException("Medical record", medicalRecordId);
            }
            return medicalRecord;
        }
    }
}