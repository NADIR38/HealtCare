using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class PdfService : IPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PdfService> _logger;

        public PdfService(ApplicationDbContext context, ILogger<PdfService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> GeneratePrescriptionPdfAsync(Guid prescriptionId)
        {
            // Fetch prescription with all details
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                throw new NotFoundException("Prescription", prescriptionId);
            }

            _logger.LogInformation("Generating PDF for prescription {PrescriptionNumber}", prescription.PrescriptionNumber);

            // Generate PDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(Header);

                    page.Content().Element(content => Content(content, prescription));

                    page.Footer().Element(Footer);
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        private void Header(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("HEALTHCARE MANAGEMENT SYSTEM")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    column.Item().Text("Medical Prescription")
                        .FontSize(14)
                        .SemiBold();

                    column.Item().PaddingTop(5).Text("123 Medical Center Drive")
                        .FontSize(9);
                    column.Item().Text("City, State 12345")
                        .FontSize(9);
                    column.Item().Text("Phone: (123) 456-7890")
                        .FontSize(9);
                });

                row.ConstantItem(120).Height(80).Placeholder();
            });
        }

        private void Content(IContainer container, Domain.Entities.Prescription prescription)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Prescription No: {prescription.PrescriptionNumber}")
                            .SemiBold();
                        col.Item().Text($"Date: {prescription.PrescriptionDate:MMMM dd, yyyy}");
                        if (prescription.ValidUntil.HasValue)
                        {
                            col.Item().Text($"Valid Until: {prescription.ValidUntil.Value:MMMM dd, yyyy}");
                        }
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Patient Information
                column.Item().PaddingTop(10).Text("Patient Information")
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Name: {prescription.Patient.User.FirstName} {prescription.Patient.User.LastName}");
                        col.Item().Text($"Patient ID: {prescription.Patient.PatientNumber}");
                        col.Item().Text($"Date of Birth: {prescription.Patient.User.DateOfBirth:MMMM dd, yyyy}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Phone: {prescription.Patient.User.PhoneNumber ?? "N/A"}");
                        col.Item().Text($"Email: {prescription.Patient.User.Email}");
                        col.Item().Text($"Blood Group: {prescription.Patient.BloodGroup}");
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(10).Text("Prescribed By")
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(5).Column(col =>
                {
                    col.Item().Text($"Dr. {prescription.Doctor.User.FirstName} {prescription.Doctor.User.LastName}")
                        .SemiBold();
                    col.Item().Text($"Specialization: {prescription.Doctor.Specialization}");
                    col.Item().Text($"License No: {prescription.Doctor.LicenseNumber}");
                    col.Item().Text($"Contact: {prescription.Doctor.User.PhoneNumber ?? "N/A"}");
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Medications Table
                column.Item().PaddingTop(10).Text("Medications")
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);  // Sr. No
                        columns.RelativeColumn(3);   // Medicine Name
                        columns.RelativeColumn(2);   // Dosage
                        columns.RelativeColumn(2);   // Frequency
                        columns.RelativeColumn(2);   // Duration
                        columns.RelativeColumn(3);   // Instructions
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").SemiBold();
                        header.Cell().Element(CellStyle).Text("Medicine Name").SemiBold();
                        header.Cell().Element(CellStyle).Text("Dosage").SemiBold();
                        header.Cell().Element(CellStyle).Text("Frequency").SemiBold();
                        header.Cell().Element(CellStyle).Text("Duration").SemiBold();
                        header.Cell().Element(CellStyle).Text("Instructions").SemiBold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold())
                                .PaddingVertical(5)
                                .BorderBottom(1)
                                .BorderColor(Colors.Black);
                        }
                    });

                    // Rows
                    int index = 1;
                    foreach (var item in prescription.Items)
                    {
                        table.Cell().Element(CellStyle).Text(index.ToString());
                        table.Cell().Element(CellStyle).Text(item.MedicineName);
                        table.Cell().Element(CellStyle).Text(item.Dosage);
                        table.Cell().Element(CellStyle).Text(item.Frequency);
                        table.Cell().Element(CellStyle).Text(item.Duration);
                        table.Cell().Element(CellStyle).Text(item.Instructions ?? "-");

                        index++;

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(5);
                        }
                    }
                });

                // Notes
                if (!string.IsNullOrWhiteSpace(prescription.Notes))
                {
                    column.Item().PaddingTop(15).Text("Additional Notes")
                        .FontSize(12)
                        .SemiBold();
                    column.Item().PaddingTop(5).Text(prescription.Notes)
                        .FontSize(10);
                }

                // Signature
                column.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("_________________________");
                        col.Item().PaddingTop(5).Text("Patient Signature");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("_________________________");
                        col.Item().AlignRight().PaddingTop(5).Text("Doctor Signature");
                    });
                });

                // Warning Box
                column.Item().PaddingTop(20).Border(1).BorderColor(Colors.Red.Medium)
                    .Background(Colors.Red.Lighten5)
                    .Padding(10)
                    .Text("⚠️ IMPORTANT: Take medications as prescribed. Do not share medications with others. Contact your doctor if you experience any side effects.")
                    .FontSize(9)
                    .FontColor(Colors.Red.Darken2);
            });
        }

        private void Footer(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("Generated on: ").FontSize(9);
                text.Span($"{DateTime.UtcNow:MMMM dd, yyyy hh:mm tt} UTC").FontSize(9).SemiBold();
                text.Span(" | This is a computer-generated document and does not require a signature.").FontSize(8);
            });
        }

        // Medical Report PDF
        public async Task<byte[]> GenerateMedicalReportPdfAsync(Guid medicalRecordId)
        {
            var medicalRecord = await _context.MedicalRecord
                .Include(m => m.Patient)
                    .ThenInclude(p => p.User)
                .Include(m => m.Doctor)
                    .ThenInclude(d => d.User)
                .Include(m => m.VitalSigns)
                .Include(m => m.Prescriptions)
                    .ThenInclude(p => p.Items)
                .Include(m => m.LabTests)
                .FirstOrDefaultAsync(m => m.Id == medicalRecordId);

            if (medicalRecord == null)
            {
                throw new NotFoundException("Medical Record", medicalRecordId);
            }

            _logger.LogInformation("Generating medical report PDF for record {RecordId}", medicalRecordId);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(MedicalReportHeader);
                    page.Content().Element(content => MedicalReportContent(content, medicalRecord));
                    page.Footer().Element(Footer);
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        private void MedicalReportHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("MEDICAL EXAMINATION REPORT")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Medium)
                    .AlignCenter();

                column.Item().PaddingTop(5).Text("Healthcare Management System")
                    .FontSize(12)
                    .AlignCenter();

                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Medium);
            });
        }

        private void MedicalReportContent(IContainer container, Domain.Entities.MedicalRecord record)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Visit Date: {record.VisitDate:MMMM dd, yyyy}").SemiBold();
                    row.RelativeItem().AlignRight().Text($"Report Generated: {DateTime.UtcNow:MMMM dd, yyyy}");
                });

                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(15).Text("Patient Information")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Name: {record.Patient.User.FirstName} {record.Patient.User.LastName}");
                        col.Item().Text($"Patient ID: {record.Patient.PatientNumber}");

                        col.Item().Text(
                            record.Patient.User.DateOfBirth.HasValue
                                ? $"Age: {DateTime.UtcNow.Year - record.Patient.User.DateOfBirth.Value.Year} years"
                                : "Age: N/A"
                        );
                        col.Item().Text($"Gender: {record.Patient.User.Gender}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Blood Group: {record.Patient.BloodGroup}");
                        col.Item().Text($"Phone: {record.Patient.User.PhoneNumber ?? "N/A"}");
                        col.Item().Text($"Email: {record.Patient.User.Email}");
                    });
                });

                column.Item().PaddingTop(15).Text("Examining Doctor")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(5).Column(col =>
                {
                    col.Item().Text($"Dr. {record.Doctor.User.FirstName} {record.Doctor.User.LastName}").SemiBold();
                    col.Item().Text($"Specialization: {record.Doctor.Specialization}");
                    col.Item().Text($"License No: {record.Doctor.LicenseNumber}");
                });

                if (record.VitalSigns != null)
                {
                    column.Item().PaddingTop(15).Text("Vital Signs")
                        .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);

                    column.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Blood Pressure: {record.VitalSigns.BloodPressureSystolic}/{record.VitalSigns.BloodPressureDiastolic} mmHg");
                            row.RelativeItem().Text($"Heart Rate: {record.VitalSigns.HeartRate} bpm");
                            row.RelativeItem().Text($"Temperature: {record.VitalSigns.Temperature}°F");
                        });
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"SpO2: {record.VitalSigns.OxygenSaturation}%");
                            row.RelativeItem().Text($"Respiratory Rate: {record.VitalSigns.RespiratoryRate}/min");
                            row.RelativeItem().Text($"Weight: {record.VitalSigns.Weight} kg");
                        });
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Height: {record.VitalSigns.Height} cm");
                            row.RelativeItem().Text($"BMI: {record.VitalSigns.BMI:F2}");
                        });
                    });
                }

                if (!string.IsNullOrWhiteSpace(record.ChiefComplaint))
                {
                    column.Item().PaddingTop(15).Text("Chief Complaint")
                        .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().PaddingTop(5).Text(record.ChiefComplaint);
                }

                if (!string.IsNullOrWhiteSpace(record.Symptoms))
                {
                    column.Item().PaddingTop(10).Text("Symptoms")
                        .FontSize(12).SemiBold();
                    column.Item().PaddingTop(5).Text(record.Symptoms);
                }

                if (!string.IsNullOrWhiteSpace(record.PhysicalExamination))
                {
                    column.Item().PaddingTop(10).Text("Physical Examination")
                        .FontSize(12).SemiBold();
                    column.Item().PaddingTop(5).Text(record.PhysicalExamination);
                }

                if (!string.IsNullOrWhiteSpace(record.Diagnosis))
                {
                    column.Item().PaddingTop(15).Text("Diagnosis")
                        .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().PaddingTop(5).Border(1).BorderColor(Colors.Blue.Lighten2)
                        .Background(Colors.Blue.Lighten5).Padding(10)
                        .Text(record.Diagnosis);
                }

                if (!string.IsNullOrWhiteSpace(record.TreatmentPlan))
                {
                    column.Item().PaddingTop(15).Text("Treatment Plan")
                        .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().PaddingTop(5).Text(record.TreatmentPlan);
                }

                if (record.LabTests?.Any() == true)
                {
                    column.Item().PaddingTop(15).Text("Lab Tests Ordered")
                        .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);

                    column.Item().PaddingTop(5).Column(col =>
                    {
                        foreach (var test in record.LabTests)
                        {
                            col.Item().PaddingVertical(3).Text($"• {test.TestName} - Status: {test.Status}");
                        }
                    });
                }

                if (!string.IsNullOrWhiteSpace(record.Notes))
                {
                    column.Item().PaddingTop(15).Text("Additional Notes")
                        .FontSize(12).SemiBold();
                    column.Item().PaddingTop(5).Text(record.Notes);
                }

                column.Item().PaddingTop(30).AlignRight().Column(col =>
                {
                    col.Item().Text("_________________________");
                    col.Item().PaddingTop(5).Text($"Dr. {record.Doctor.User.FirstName} {record.Doctor.User.LastName}");
                    col.Item().Text("Examining Physician");
                });
            });
        }

        public async Task<byte[]> GenerateLabTestReportPdfAsync(Guid labTestId)
        {
            var labTest = await _context.LabTests
                .Include(l => l.Patient)
                    .ThenInclude(p => p.User)
                .Include(l => l.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(l => l.Id == labTestId);

            if (labTest == null)
            {
                throw new NotFoundException("Lab Test", labTestId);
            }

            _logger.LogInformation("Generating lab test report PDF for test {TestId}", labTestId);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(LabTestHeader);
                    page.Content().Element(content => LabTestContent(content, labTest));
                    page.Footer().Element(Footer);
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        private void LabTestHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("LABORATORY TEST REPORT")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Green.Medium)
                    .AlignCenter();

                column.Item().PaddingTop(5).Text("Healthcare Management System - Laboratory")
                    .FontSize(12)
                    .AlignCenter();

                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Green.Medium);
            });
        }

        private void LabTestContent(IContainer container, Domain.Entities.LabTest labTest)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Test Name: {labTest.TestName}").SemiBold().FontSize(14);
                        col.Item().Text($"Test Type: {labTest.TestType ?? "N/A"}");
                        col.Item().Text($"Status: {labTest.Status}").FontColor(
                            labTest.Status == Domain.Enums.LabTestStatus.Completed ? Colors.Green.Medium : Colors.Orange.Medium);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Ordered: {labTest.OrderedDate:MMMM dd, yyyy}");
                        if (labTest.SampleCollectedDate.HasValue)
                            col.Item().Text($"Sample Collected: {labTest.SampleCollectedDate.Value:MMMM dd, yyyy}");
                        if (labTest.ResultDate.HasValue)
                            col.Item().Text($"Result Date: {labTest.ResultDate.Value:MMMM dd, yyyy}");
                    });
                });

                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(15).Text("Patient Information")
                    .FontSize(14).SemiBold().FontColor(Colors.Green.Medium);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Name: {labTest.Patient.User.FirstName} {labTest.Patient.User.LastName}");
                        col.Item().Text($"Patient ID: {labTest.Patient.PatientNumber}");
                        col.Item().Text(labTest.Patient.User.DateOfBirth.HasValue ? $"Age: {DateTime.UtcNow.Year - labTest.Patient.User.DateOfBirth.Value.Year} years" : "Age: N/A");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Gender: {labTest.Patient.User.Gender}");
                        col.Item().Text($"Blood Group: {labTest.Patient.BloodGroup}");
                        col.Item().Text($"Phone: {labTest.Patient.User.PhoneNumber ?? "N/A"}");
                    });
                });

                // Ordered By
                column.Item().PaddingTop(15).Text("Ordered By")
                    .FontSize(14).SemiBold().FontColor(Colors.Green.Medium);

                column.Item().PaddingTop(5).Column(col =>
                {
                    col.Item().Text($"Dr. {labTest.Doctor.User.FirstName} {labTest.Doctor.User.LastName}").SemiBold();
                    col.Item().Text($"Specialization: {labTest.Doctor.Specialization}");
                });

                // Results
                if (!string.IsNullOrWhiteSpace(labTest.Results))
                {
                    column.Item().PaddingTop(15).Text("Test Results")
                        .FontSize(14).SemiBold().FontColor(Colors.Green.Medium);

                    column.Item().PaddingTop(5).Border(2).BorderColor(Colors.Green.Medium)
                        .Background(Colors.Green.Lighten5).Padding(15)
                        .Text(labTest.Results).FontSize(12);
                }
                else
                {
                    column.Item().PaddingTop(15).Border(1).BorderColor(Colors.Orange.Medium)
                        .Background(Colors.Orange.Lighten5).Padding(15)
                        .Text("Results pending. This report will be updated once the test is completed.")
                        .FontSize(11).FontColor(Colors.Orange.Darken2);
                }

                if (!string.IsNullOrWhiteSpace(labTest.Notes))
                {
                    column.Item().PaddingTop(15).Text("Notes")
                        .FontSize(12).SemiBold();
                    column.Item().PaddingTop(5).Text(labTest.Notes);
                }

                if (labTest.Status == Domain.Enums.LabTestStatus.Completed)
                {
                    column.Item().PaddingTop(30).AlignRight().Column(col =>
                    {
                        col.Item().Text("_________________________");
                        col.Item().PaddingTop(5).Text("Lab Technician");
                        col.Item().Text($"Date: {labTest.ResultDate?.ToString("MMMM dd, yyyy") ?? "N/A"}");
                    });
                }
            });
        }

        // Add this method to the PdfService class to replace the NotImplementedException

        public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            _logger.LogInformation("Generating invoice PDF for {InvoiceNumber}", invoice.InvoiceNumber);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(InvoiceHeader);
                    page.Content().Element(content => InvoiceContent(content, invoice));
                    page.Footer().Element(Footer);
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        private void InvoiceHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("HEALTHCARE MANAGEMENT SYSTEM")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Medium);

                        col.Item().Text("INVOICE")
                            .FontSize(16)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken1);

                        col.Item().PaddingTop(5).Text("123 Medical Center Drive")
                            .FontSize(9);
                        col.Item().Text("City, State 12345")
                            .FontSize(9);
                        col.Item().Text("Phone: (123) 456-7890")
                            .FontSize(9);
                        col.Item().Text("Email: billing@healthcare.com")
                            .FontSize(9);
                    });

                    row.ConstantItem(120).Height(80).Placeholder();
                });

                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Medium);
            });
        }

        private void InvoiceContent(IContainer container, Domain.Entities.Invoice invoice)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Invoice details section
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Invoice #: {invoice.InvoiceNumber}")
                            .FontSize(12)
                            .SemiBold();
                        col.Item().Text($"Date: {invoice.InvoiceDate:MMMM dd, yyyy}");
                        if (invoice.DueDate.HasValue)
                        {
                            col.Item().Text($"Due Date: {invoice.DueDate.Value:MMMM dd, yyyy}");
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Status: {invoice.Status}")
                            .FontSize(12)
                            .SemiBold()
                            .FontColor(GetStatusColor(invoice.Status));
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Bill To section
                column.Item().PaddingTop(10).Text("Bill To:")
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(5).Column(col =>
                {
                    col.Item().Text($"{invoice.Patient.User.FirstName} {invoice.Patient.User.LastName}")
                        .SemiBold();
                    col.Item().Text($"Patient ID: {invoice.Patient.PatientNumber}");
                    col.Item().Text($"Phone: {invoice.Patient.User.PhoneNumber ?? "N/A"}");
                    col.Item().Text($"Email: {invoice.Patient.User.Email}");
                });

                column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Items table
                column.Item().Text("Invoice Items")
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);   // #
                        columns.RelativeColumn(4);    // Description
                        columns.RelativeColumn(2);    // Type
                        columns.RelativeColumn(1);    // Qty
                        columns.RelativeColumn(2);    // Unit Price
                        columns.RelativeColumn(2);    // Amount
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("#");
                        header.Cell().Element(HeaderCellStyle).Text("Description");
                        header.Cell().Element(HeaderCellStyle).Text("Type");
                        header.Cell().Element(HeaderCellStyle).Text("Qty");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Unit Price");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount");

                        static IContainer HeaderCellStyle(IContainer container)
                        {
                            return container
                                .Background(Colors.Blue.Lighten4)
                                .PaddingVertical(5)
                                .PaddingHorizontal(10)
                                .BorderBottom(1)
                                .BorderColor(Colors.Blue.Medium);
                        }
                    });

                    // Rows
                    int index = 1;
                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Element(RowCellStyle).Text(index.ToString());
                        table.Cell().Element(RowCellStyle).Text(item.Description);
                        table.Cell().Element(RowCellStyle).Text(item.ItemType ?? "-");
                        table.Cell().Element(RowCellStyle).Text(item.Quantity.ToString());
                        table.Cell().Element(RowCellStyle).AlignRight().Text($"{item.UnitPrice:C}");
                        table.Cell().Element(RowCellStyle).AlignRight().Text($"{item.Amount:C}").SemiBold();

                        index++;

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(8)
                                .PaddingHorizontal(10);
                        }
                    }
                });

                // Totals section
                column.Item().PaddingTop(20).AlignRight().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text("Subtotal:");
                        row.ConstantItem(100).AlignRight().Text($"{invoice.SubTotal:C}");
                    });

                    if (invoice.TaxAmount.HasValue && invoice.TaxAmount.Value > 0)
                    {
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.ConstantItem(150).Text("Tax:");
                            row.ConstantItem(100).AlignRight().Text($"{invoice.TaxAmount.Value:C}");
                        });
                    }

                    if (invoice.DiscountAmount.HasValue && invoice.DiscountAmount.Value > 0)
                    {
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.ConstantItem(150).Text("Discount:");
                            row.ConstantItem(100).AlignRight().Text($"-{invoice.DiscountAmount.Value:C}")
                                .FontColor(Colors.Red.Medium);
                        });
                    }

                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(150).Text("Total Amount:")
                            .FontSize(14)
                            .SemiBold();
                        row.ConstantItem(100).AlignRight().Text($"{invoice.TotalAmount:C}")
                            .FontSize(14)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);
                    });

                    // Payment information
                    if (invoice.Payments.Any())
                    {
                        var paidAmount = invoice.Payments
                            .Where(p => p.Status == Domain.Enums.PaymentStatus.Completed)
                            .Sum(p => p.Amount);
                        var balanceAmount = invoice.TotalAmount - paidAmount;

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.ConstantItem(150).Text("Paid Amount:");
                            row.ConstantItem(100).AlignRight().Text($"{paidAmount:C}")
                                .FontColor(Colors.Green.Medium);
                        });

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.ConstantItem(150).Text("Balance Due:")
                                .SemiBold();
                            row.ConstantItem(100).AlignRight().Text($"{balanceAmount:C}")
                                .SemiBold()
                                .FontColor(balanceAmount > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                        });
                    }
                });

                // Payment History
                if (invoice.Payments.Any())
                {
                    column.Item().PaddingTop(25).Text("Payment History")
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(Colors.Blue.Medium);

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);  // Payment Number
                            columns.RelativeColumn(2);  // Date
                            columns.RelativeColumn(2);  // Method
                            columns.RelativeColumn(1);  // Amount
                            columns.RelativeColumn(1);  // Status
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Payment #");
                            header.Cell().Element(HeaderCellStyle).Text("Date");
                            header.Cell().Element(HeaderCellStyle).Text("Method");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount");
                            header.Cell().Element(HeaderCellStyle).Text("Status");

                            static IContainer HeaderCellStyle(IContainer container)
                            {
                                return container
                                    .Background(Colors.Grey.Lighten3)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(10)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Darken1);
                            }
                        });

                        // Rows
                        foreach (var payment in invoice.Payments.OrderByDescending(p => p.PaymentDate))
                        {
                            table.Cell().Element(RowCellStyle).Text(payment.PaymentNumber).FontSize(9);
                            table.Cell().Element(RowCellStyle).Text(payment.PaymentDate.ToString("MMM dd, yyyy")).FontSize(9);
                            table.Cell().Element(RowCellStyle).Text(payment.PaymentMethod.ToString()).FontSize(9);
                            table.Cell().Element(RowCellStyle).AlignRight().Text($"{payment.Amount:C}").FontSize(9);
                            table.Cell().Element(RowCellStyle).Text(payment.Status.ToString())
                                .FontSize(9)
                                .FontColor(payment.Status == Domain.Enums.PaymentStatus.Completed
                                    ? Colors.Green.Medium
                                    : Colors.Orange.Medium);

                            static IContainer RowCellStyle(IContainer container)
                            {
                                return container
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(10);
                            }
                        }
                    });
                }

                // Notes
                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    column.Item().PaddingTop(20).Text("Notes")
                        .FontSize(12)
                        .SemiBold();
                    column.Item().PaddingTop(5).Text(invoice.Notes)
                        .FontSize(10);
                }

                // Payment Instructions
                if (invoice.Status != Domain.Enums.InvoiceStatus.Paid &&
                    invoice.Status != Domain.Enums.InvoiceStatus.Cancelled)
                {
                    column.Item().PaddingTop(25).Border(1).BorderColor(Colors.Blue.Medium)
                        .Background(Colors.Blue.Lighten5)
                        .Padding(15)
                        .Column(col =>
                        {
                            col.Item().Text("Payment Instructions")
                                .FontSize(12)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            col.Item().PaddingTop(5).Text("Please make payment by the due date using one of the following methods:")
                                .FontSize(10);

                            col.Item().PaddingTop(5).Text("• Cash at reception")
                                .FontSize(9);
                            col.Item().Text("• Credit/Debit card")
                                .FontSize(9);
                            col.Item().Text("• Bank transfer to: Account # 1234567890")
                                .FontSize(9);
                            col.Item().Text("• Insurance claim (please provide policy details)")
                                .FontSize(9);

                            col.Item().PaddingTop(5).Text("For questions about this invoice, please contact our billing department at billing@healthcare.com or call (123) 456-7890.")
                                .FontSize(9)
                                .Italic();
                        });
                }

                // Thank you note
                column.Item().PaddingTop(30).AlignCenter().Text("Thank you for choosing Healthcare Management System!")
                    .FontSize(12)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);
            });
        }

        private string GetStatusColor(Domain.Enums.InvoiceStatus status)
        {
            return status switch
            {
                Domain.Enums.InvoiceStatus.Draft => Colors.Grey.Darken1,
                Domain.Enums.InvoiceStatus.Pending => Colors.Orange.Medium,
                Domain.Enums.InvoiceStatus.Paid => Colors.Green.Medium,
                Domain.Enums.InvoiceStatus.PartiallyPaid => Colors.Blue.Medium,
                Domain.Enums.InvoiceStatus.Overdue => Colors.Red.Medium,
                Domain.Enums.InvoiceStatus.Cancelled => Colors.Red.Darken1,
                _ => Colors.Black
            };
        }
    }
}