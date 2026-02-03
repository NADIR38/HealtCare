using HealthcareSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

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
        }
    }
}