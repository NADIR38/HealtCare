using System.Collections.Generic;

namespace HealthcareSystem.Application.Models
{
    public class EmailMessage
    {
        public List<string> To { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
    }
}