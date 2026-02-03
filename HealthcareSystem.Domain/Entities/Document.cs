using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid UploadedBy { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public User UploadedByUser { get; set; } = null!;
    }
}
