using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Domain.Enums
{
    public enum BloodGroup    {
        APositive,
        ANegative,
        BPositive,
        BNegative,
        OPositive,
        ONegative,
        ABPositive,
        ABNegative
    }
    public enum SmokingStatus
    {
        Never,
        Former,
        Current
    }
     public enum AlcoholConsumption
    {
        None,
        Occasional,
        Regular
    }
    public enum LeaveStatus { 
    Pending,
    Approved,
    Rejected
    }
    public enum AppointmentStatus
    {
        Scheduled,
        CheckedIn,
        InProgress,
        Completed,
        Cancelled,
        NoShow,
    }
    public enum AppointmentType
    {
        InPerson,
            Telemedicine
    }
    public enum DocumentType
    {
        LabReport,
        Prescription,
        XRay,
        MRI,
        CT_Scan,
        Insurance,
        Other
    }
}
