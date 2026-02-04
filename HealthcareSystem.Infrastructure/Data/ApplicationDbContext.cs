using HealthcareSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Diagnostics;

namespace HealthcareSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens {  get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<MedicalHistory> MedicalHistory { get; set; }
        public DbSet<Doctor> Doctor { get;set; }    
        public DbSet<DoctorLeave> DoctorLeave{get;set ;}    
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedule {  get; set; }
        public DbSet<MedicalRecord> MedicalRecord { get; set; }
        public DbSet<VitalSigns> VitalSigns { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<LabTest> LabTests { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Gender)
                    .HasConversion<string>();

               

                entity.Property(e => e.UpdatedAt)
                    
                    .ValueGeneratedOnAddOrUpdate();
            });

            // UserRole configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Role)
                    .HasConversion<string>();

      

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patients");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PatientNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.PatientNumber).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();

                entity.Property(e => e.BloodGroup)
                    .HasConversion<string>();

                entity.Property(e => e.Height)
                    .HasPrecision(5, 2);

                entity.Property(e => e.Weight)
                    .HasPrecision(5, 2);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.User)
                    .WithOne(u => u.Patient)
                    .HasForeignKey<Patient>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<MedicalHistory>(entity =>
            {
                entity.ToTable("MedicalHistory");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ChronicConditions)
                    .HasColumnType("json");

                entity.Property(e => e.Allergies)
                    .HasColumnType("json");

                entity.Property(e => e.PastSurgeries)
                    .HasColumnType("json");

                entity.Property(e => e.FamilyHistory)
                    .HasColumnType("json");

                entity.Property(e => e.CurrentMedications)
                    .HasColumnType("json");

                entity.Property(e => e.SmokingStatus)
                    .HasConversion<string>();

                entity.Property(e => e.AlcoholConsumption)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.Patient)
                    .WithOne(p => p.MedicalHistory)
                    .HasForeignKey<MedicalHistory>(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            // Doctor configuration
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DoctorNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.DoctorNumber).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.LicenseNumber).IsUnique();

                entity.Property(e => e.Specialization)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LicenseNumber)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ConsultationFee)
                    .HasPrecision(10, 2);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.User)
                    .WithOne(u => u.Doctor)
                    .HasForeignKey<Doctor>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DoctorSchedule configuration
            modelBuilder.Entity<DoctorSchedule>(entity =>
            {
                entity.ToTable("DoctorSchedule");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DayOfWeek)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Doctor)
                    .WithMany(d => d.Schedules)
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.DoctorId, e.DayOfWeek, e.IsActive });
            });

            // DoctorLeave configuration
            modelBuilder.Entity<DoctorLeave>(entity =>
            {
                entity.ToTable("DoctorLeaves");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Status)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Doctor)
                    .WithMany(d => d.Leaves)
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.DoctorId, e.StartDate, e.EndDate });
            });

            // Appointment configuration
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AppointmentNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.AppointmentNumber).IsUnique();

                entity.Property(e => e.Status)
                    .HasConversion<string>();

                entity.Property(e => e.Type)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.Patient)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Doctor)
                    .WithMany(d => d.Appointments)
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate, e.StartTime });
                entity.HasIndex(e => new { e.PatientId, e.AppointmentDate });
                entity.HasIndex(e => e.Status);
            });

        

            // Document configuration (basic for now)
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DocumentType)
                    .HasConversion<string>();

                entity.Property(e => e.UploadedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Patient)
                    .WithMany(p => p.Documents)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedBy)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.PatientId, e.DocumentType });
            });
            // MedicalRecord configuration
            modelBuilder.Entity<MedicalRecord>(entity =>
            {
                entity.ToTable("MedicalRecords");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.Patient)
                    .WithMany(p => p.MedicalRecords)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Doctor)
                    .WithMany(d => d.MedicalRecords)
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Appointment)
                    .WithMany()
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.PatientId, e.VisitDate });
                entity.HasIndex(e => e.DoctorId);
            });

            // VitalSigns configuration
            modelBuilder.Entity<VitalSigns>(entity =>
            {
                entity.ToTable("VitalSigns");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Temperature)
                    .HasPrecision(5, 2);

                entity.Property(e => e.OxygenSaturation)
                    .HasPrecision(5, 2);

                entity.Property(e => e.Weight)
                    .HasPrecision(5, 2);

                entity.Property(e => e.Height)
                    .HasPrecision(5, 2);

                entity.Property(e => e.BMI)
                    .HasPrecision(5, 2);

                entity.HasOne(e => e.MedicalRecord)
                    .WithOne(m => m.VitalSigns)
                    .HasForeignKey<VitalSigns>(e => e.MedicalRecordId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Prescription configuration
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.ToTable("Prescriptions");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PrescriptionNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.PrescriptionNumber).IsUnique();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.MedicalRecord)
                    .WithMany(m => m.Prescriptions)
                    .HasForeignKey(e => e.MedicalRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Patient)
                    .WithMany()
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Doctor)
                    .WithMany()
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.PatientId, e.PrescriptionDate });
            });

            // PrescriptionItem configuration
            modelBuilder.Entity<PrescriptionItem>(entity =>
            {
                entity.ToTable("PrescriptionItems");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.MedicineName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasOne(e => e.Prescription)
                    .WithMany(p => p.Items)
                    .HasForeignKey(e => e.PrescriptionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PrescriptionId);
            });

            // LabTest configuration
            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.ToTable("LabTests");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Status)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.Patient)
                    .WithMany()
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Doctor)
                    .WithMany()
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.MedicalRecord)
                    .WithMany(m => m.LabTests)
                    .HasForeignKey(e => e.MedicalRecordId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.PatientId, e.OrderedDate });
                entity.HasIndex(e => e.Status);
            });
        }
    }
}