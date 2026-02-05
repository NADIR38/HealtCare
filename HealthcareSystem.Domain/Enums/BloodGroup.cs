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
    public enum Role
    {
        Admin,
        Doctor,
        Nurse,
        Receptionist,
        Patient
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
    public enum LabTestStatus
    {
        Ordered,
        SampleCollected,
        InProgress,
        Completed,
        Cancelled
    }
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
    public enum PaymentMethod
    {
        Cash,
        Card,
        Insurance,
        BankTransfer,
        OnlinePayment,
        Cheque
    }
    public enum InvoiceStatus
    {
        Draft,
        Pending,
        Paid,
        PartiallyPaid,
        Overdue,
        Cancelled
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
