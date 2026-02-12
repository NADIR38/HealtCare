using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.MedicalRecords
{
   public class UpdateLabTestResultRequest
    {
        public string Result { get; set; } = string.Empty; // Frontend 'result'
        public string? ResultValue { get; set; }
        public string? ResultUnit { get; set; }
        public string? ReferenceRange { get; set; }
        public string? Notes { get; set; }
        public bool AbnormalFlag { get; set; } // Matches TS interface
    }
}
