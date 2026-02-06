param(
    [string]$BaseUrl = "https://localhost:7227"
)

Write-Host "=== HealthCare API FULL E2E TESTS ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan

$script:FailedCount = 0

function Call-Api {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200,
        [switch]$AllowFail
    )

    Write-Host "-> [$Method] $Url ($Name)" -ForegroundColor Yellow

    $jsonBody = $null
    if ($Body -ne $null) {
        $jsonBody = $Body | ConvertTo-Json -Depth 10
    }

    $status = $null
    $parsed = $null

    try {
        $resp = Invoke-WebRequest -Method $Method -Uri $Url `
            -Headers $Headers `
            -ContentType "application/json" `
            -Body $jsonBody `
            -ErrorAction Stop

        $status = $resp.StatusCode.value__
        if ($resp.Content) {
            try { $parsed = $resp.Content | ConvertFrom-Json } catch { $parsed = $null }
        }
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response -ne $null) {
            $status = [int]$_.Exception.Response.StatusCode
            try {
                $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $content = $sr.ReadToEnd()
                if ($content) { $parsed = $content | ConvertFrom-Json }
            } catch { }
        } else {
            Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }

    if ($status -eq $ExpectedStatus) {
        Write-Host "   PASSED (Status: $status)" -ForegroundColor Green
        return @{ ok = $true; status = $status; body = $parsed }
    } else {
        Write-Host "   FAILED (Status: $status, Expected: $ExpectedStatus)" -ForegroundColor Red
        if (-not $AllowFail) { $script:FailedCount++ }
        return @{ ok = $false; status = $status; body = $parsed }
    }
}

function New-RandomEmail {
    param([string]$Prefix)
    $suffix = [Guid]::NewGuid().ToString("N").Substring(0,8)
    return "$Prefix+$suffix@test.local"
}

### 1) Create users via Auth/register (Admin, Doctor, Receptionist, Patient)

Write-Host "`n=== AUTH: Register test users ===" -ForegroundColor Cyan

$commonPassword = "Test@1234!"

$now = [DateTime]::UtcNow
$dob  = $now.AddYears(-30)

$users = @(
    @{ label = "Admin";        role = "Admin"        },
    @{ label = "Doctor";       role = "Doctor"       },
    @{ label = "Receptionist"; role = "Receptionist" },
    @{ label = "Patient";      role = "Patient"      }
)

$tokens = @{}

foreach ($u in $users) {
    $email = New-RandomEmail -Prefix $u.label.ToLower()
    $body = @{
        firstName         = $u.label
        lastName          = "Test"
        email             = $email
        passwordHash      = $commonPassword   # plain password; API hashes it
        phoneNumber       = "1234567890"
        dateOfBirth       = $dob
        gender            = "Male"
        profilePictureUrl = $null
        role              = $u.role
    }

    $res = Call-Api -Name "Register $($u.label)" -Method "POST" `
        -Url "$BaseUrl/Api/Auth/register" `
        -Body $body -ExpectedStatus 200

    if ($res.ok -and $res.body) {
        $tokens[$u.label] = [pscustomobject]@{
            Email        = $email
            Password     = $commonPassword
            UserId       = $res.body.userId
            AccessToken  = $res.body.token
            RefreshToken = $res.body.refreshToken
            Role         = $res.body.role
        }
    } else {
        Write-Host "  Could not register $($u.label). Further role-based tests may fail." -ForegroundColor Red
    }
}

function Get-AuthHeader {
    param([string]$Label)
    if ($tokens.ContainsKey($Label) -and $tokens[$Label].AccessToken) {
        return @{ "Authorization" = "Bearer $($tokens[$Label].AccessToken)" }
    }
    return @{}
}

$adminHeaders        = Get-AuthHeader -Label "Admin"
$doctorHeaders       = Get-AuthHeader -Label "Doctor"
$receptionistHeaders = Get-AuthHeader -Label "Receptionist"
$patientHeaders      = Get-AuthHeader -Label "Patient"

### 2) Auth: test login, refresh, logout for Admin

Write-Host "`n=== AUTH: Login/refresh/logout ===" -ForegroundColor Cyan

if ($tokens.ContainsKey("Admin")) {
    $adminEmail = $tokens["Admin"].Email

    $loginBody = @{
        email    = $adminEmail
        password = $commonPassword
    }

    $loginRes = Call-Api -Name "Admin login" -Method "POST" `
        -Url "$BaseUrl/Api/Auth/login" `
        -Body $loginBody -ExpectedStatus 200

    if ($loginRes.ok -and $loginRes.body) {
        $tokens["Admin"].AccessToken = $loginRes.body.token
        $tokens["Admin"].RefreshToken = $loginRes.body.refreshToken
        $adminHeaders = Get-AuthHeader -Label "Admin"
    }

    $refreshRes = Call-Api -Name "Admin refresh token" -Method "POST" `
        -Url "$BaseUrl/Api/Auth/refresh" `
        -Body $tokens["Admin"].RefreshToken `
        -ExpectedStatus 200

    $logoutBody = @{ refreshToken = $tokens["Admin"].RefreshToken }
    $logoutRes = Call-Api -Name "Admin logout" -Method "POST" `
        -Url "$BaseUrl/Api/Auth/logout" `
        -Body $logoutBody -ExpectedStatus 200
} else {
    Write-Host "Skipping login/refresh/logout (no admin user)" -ForegroundColor DarkYellow
}

### 3) Patient flow: create patient, medical history, fetch back

Write-Host "`n=== PATIENT FLOW ===" -ForegroundColor Cyan

$patientId = $null

if ($tokens.ContainsKey("Patient") -and $adminHeaders.Count -gt 0) {
    $createPatientBody = @{
        userId                = $tokens["Patient"].UserId
        bloodGroup            = "OPositive"
        height                = 175.5
        weight                = 75.2
        emergencyContactName  = "Emergency Contact"
        emergencyContactPhone = "9876543210"
        emergencyContactRelation = "Spouse"
        address               = "123 Test Street"
        city                  = "Testville"
        state                 = "TS"
        zipCode               = "12345"
        insuranceProvider     = "Test Insurance"
        insurancePolicyNumber = "POL123456"
    }

    $cpRes = Call-Api -Name "Create patient" -Method "POST" `
        -Url "$BaseUrl/api/patients" `
        -Body $createPatientBody -Headers $adminHeaders -ExpectedStatus 200

    if ($cpRes.ok -and $cpRes.body) {
        $patientId = $cpRes.body.id
        Write-Host "   PatientId: $patientId" -ForegroundColor Green

        Call-Api -Name "Get patient by id" -Method "GET" `
            -Url "$BaseUrl/api/patients/$patientId" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get patient by userId" -Method "GET" `
            -Url "$BaseUrl/api/patients/user/$($tokens["Patient"].UserId)" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        $mhBody = @{
            chronicConditions  = @("Hypertension")
            allergies          = @("Penicillin")
            pastSurgeries      = @("Appendectomy")
            familyHistory      = @("Diabetes")
            currentMedications = @("Amlodipine")
            smokingStatus      = "Never"
            alcoholConsumption = "Occasional"
        }

        Call-Api -Name "Create/Update medical history" -Method "POST" `
            -Url "$BaseUrl/api/patients/$patientId/medical-history" `
            -Body $mhBody -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get medical history" -Method "GET" `
            -Url "$BaseUrl/api/patients/$patientId/medical-history" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "List patients" -Method "GET" `
            -Url "$BaseUrl/api/patients?page=1&pageSize=10" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null
    }
} else {
    Write-Host "Skipping patient flow (missing patient user or admin token)" -ForegroundColor DarkYellow
}

### 4) Doctor flow: create doctor, schedules, leaves

Write-Host "`n=== DOCTOR FLOW ===" -ForegroundColor Cyan

$doctorId = $null

if ($tokens.ContainsKey("Doctor") -and $adminHeaders.Count -gt 0) {
    $createDoctorBody = @{
        userId          = $tokens["Doctor"].UserId
        specialization  = "General Medicine"
        licenseNumber   = "LIC-" + ([Guid]::NewGuid().ToString("N").Substring(0,6))
        qualification   = "MBBS, MD"
        experienceYears = 5
        consultationFee = 1000.00
        bio             = "Test doctor created by automated script."
    }

    $cdRes = Call-Api -Name "Create doctor" -Method "POST" `
        -Url "$BaseUrl/api/v1/doctors" `
        -Body $createDoctorBody -Headers $adminHeaders -ExpectedStatus 201

    if ($cdRes.ok -and $cdRes.body) {
        $doctorId = $cdRes.body.id
        Write-Host "   DoctorId: $doctorId" -ForegroundColor Green

        Call-Api -Name "Get doctor by id" -Method "GET" `
            -Url "$BaseUrl/api/v1/doctors/$doctorId" `
            -ExpectedStatus 200 | Out-Null

        Call-Api -Name "List doctors (public)" -Method "GET" `
            -Url "$BaseUrl/api/v1/doctors?page=1&pageSize=10" `
            -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Available doctors (public)" -Method "GET" `
            -Url "$BaseUrl/api/v1/doctors/available" `
            -ExpectedStatus 200 | Out-Null

        if ($doctorHeaders.Count -gt 0) {
            Call-Api -Name "Get my doctor profile" -Method "GET" `
                -Url "$BaseUrl/api/v1/doctors/me" `
                -Headers $doctorHeaders -ExpectedStatus 200 -AllowFail | Out-Null
        }

        # Schedule
        $schedBody = @{
            dayOfWeek          = "Monday"
            startTime          = "09:00:00"
            endTime            = "12:00:00"
            slotDurationMinutes = 30
        }

        $schedRes = Call-Api -Name "Add doctor schedule" -Method "POST" `
            -Url "$BaseUrl/api/v1/doctors/$doctorId/schedules" `
            -Body $schedBody -Headers $adminHeaders -ExpectedStatus 201

        Call-Api -Name "Get doctor schedules" -Method "GET" `
            -Url "$BaseUrl/api/v1/doctors/$doctorId/schedules" `
            -ExpectedStatus 200 | Out-Null

        $tomorrow = [DateTime]::UtcNow.AddDays(1).Date
        Call-Api -Name "Available slots" -Method "GET" `
            -Url "$BaseUrl/api/v1/doctors/$doctorId/available-slots?date=$($tomorrow.ToString("o"))" `
            -ExpectedStatus 200 -AllowFail | Out-Null

        # Leave request & approval
        if ($doctorHeaders.Count -gt 0) {
            $leaveBody = @{
                doctorId  = $doctorId
                startDate = [DateTime]::UtcNow.AddDays(7).Date
                endDate   = [DateTime]::UtcNow.AddDays(9).Date
                reason    = "Vacation leave (test)"
            }

            $leaveReq = Call-Api -Name "Doctor request leave" -Method "POST" `
                -Url "$BaseUrl/api/v1/doctors/leaves" `
                -Body $leaveBody -Headers $doctorHeaders -ExpectedStatus 201

            if ($leaveReq.ok -and $leaveReq.body) {
                $leaveId = $leaveReq.body.id

                Call-Api -Name "Get my leaves" -Method "GET" `
                    -Url "$BaseUrl/api/v1/doctors/leaves/my-leaves" `
                    -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

                if ($adminHeaders.Count -gt 0) {
                    Call-Api -Name "Approve leave" -Method "PUT" `
                        -Url "$BaseUrl/api/v1/doctors/leaves/$leaveId/approve" `
                        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

                    Call-Api -Name "Get doctor leaves" -Method "GET" `
                        -Url "$BaseUrl/api/v1/doctors/$doctorId/leaves" `
                        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

                    Call-Api -Name "Get pending leaves" -Method "GET" `
                        -Url "$BaseUrl/api/v1/doctors/leaves/pending" `
                        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null
                }
            }
        }
    }
} else {
    Write-Host "Skipping doctor flow (missing doctor user or admin token)" -ForegroundColor DarkYellow
}

### 5) Appointment lifecycle

Write-Host "`n=== APPOINTMENT FLOW ===" -ForegroundColor Cyan

$appointmentId = $null

if ($patientId -and $doctorId -and $adminHeaders.Count -gt 0) {
    $apptDate = [DateTime]::UtcNow.AddDays(1).Date
    $createApptBody = @{
        patientId       = $patientId
        doctorId        = $doctorId
        appointmentDate = $apptDate
        startTime       = "10:00:00"
        type            = "InPerson"
        reason          = "Routine check-up (E2E test)"
        notes           = "Created by automated script"
    }

    $caRes = Call-Api -Name "Create appointment" -Method "POST" `
        -Url "$BaseUrl/api/appointments" `
        -Body $createApptBody -Headers $adminHeaders -ExpectedStatus 201

    if ($caRes.ok -and $caRes.body) {
        $appointmentId = $caRes.body.id
        Write-Host "   AppointmentId: $appointmentId" -ForegroundColor Green

        Call-Api -Name "Get appointment by id" -Method "GET" `
            -Url "$BaseUrl/api/appointments/$appointmentId" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "List appointments" -Method "GET" `
            -Url "$BaseUrl/api/appointments?page=1&pageSize=10" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get patient appointments" -Method "GET" `
            -Url "$BaseUrl/api/appointments/patient/$patientId" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get doctor appointments" -Method "GET" `
            -Url "$BaseUrl/api/appointments/doctor/$doctorId" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Appointment statistics" -Method "GET" `
            -Url "$BaseUrl/api/appointments/statistics" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        # Status transitions
        Call-Api -Name "Check-in appointment" -Method "PUT" `
            -Url "$BaseUrl/api/appointments/$appointmentId/check-in" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        if ($doctorHeaders.Count -gt 0) {
            Call-Api -Name "Start consultation" -Method "PUT" `
                -Url "$BaseUrl/api/appointments/$appointmentId/start" `
                -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

            Call-Api -Name "Complete appointment" -Method "PUT" `
                -Url "$BaseUrl/api/appointments/$appointmentId/complete" `
                -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null
        }

        $statusBody = @{
            status = "Completed"
            notes  = "Marked completed by test"
        }
        Call-Api -Name "Update appointment status" -Method "PUT" `
            -Url "$BaseUrl/api/appointments/$appointmentId/status" `
            -Body $statusBody -Headers $adminHeaders -ExpectedStatus 200 -AllowFail | Out-Null
    }
} else {
    Write-Host "Skipping appointment flow (missing patient/doctor/admin)" -ForegroundColor DarkYellow
}

### 6) Medical record + vitals

Write-Host "`n=== MEDICAL RECORD FLOW ===" -ForegroundColor Cyan

$medicalRecordId = $null

if ($patientId -and $doctorId -and $doctorHeaders.Count -gt 0) {
    $mrBody = @{
        patientId   = $patientId
        doctorId    = $doctorId
        appointmentId = $appointmentId
        visitDate   = [DateTime]::UtcNow
        chiefComplaint     = "Headache"
        symptoms           = "Mild headache, no nausea"
        diagnosis          = "Tension headache"
        physicalExamination = "Normal"
        treatmentPlan      = "Hydration, rest"
        notes              = "Test medical record"

        vitalSigns = @{
            bloodPressureSystolic  = "120"
            bloodPressureDiastolic = "80"
            temperature            = 36.8
            heartRate              = 72
            respiratoryRate        = 16
            oxygenSaturation       = 98.0
            weight                 = 75.0
            height                 = 175.0
            notes                  = "Vitals normal"
        }
    }

    $mrRes = Call-Api -Name "Create medical record" -Method "POST" `
        -Url "$BaseUrl/api/medicalrecords" `
        -Body $mrBody -Headers $doctorHeaders -ExpectedStatus 201

    if ($mrRes.ok -and $mrRes.body) {
        $medicalRecordId = $mrRes.body.id
        Write-Host "   MedicalRecordId: $medicalRecordId" -ForegroundColor Green

        Call-Api -Name "Get medical record by id" -Method "GET" `
            -Url "$BaseUrl/api/medicalrecords/$medicalRecordId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get patient medical records" -Method "GET" `
            -Url "$BaseUrl/api/medicalrecords/patient/$patientId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get doctor medical records" -Method "GET" `
            -Url "$BaseUrl/api/medicalrecords/doctor/$doctorId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        $vitalUpdateBody = @{
            temperature      = 37.2
            heartRate        = 78
            respiratoryRate  = 18
            oxygenSaturation = 97.0
            weight           = 76.0
            height           = 175.0
            notes            = "Updated vitals"
        }

        Call-Api -Name "Update vital signs" -Method "PUT" `
            -Url "$BaseUrl/api/medicalrecords/$medicalRecordId/vital-signs" `
            -Body $vitalUpdateBody -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null
    }
} else {
    Write-Host "Skipping medical record flow (missing ids or doctor token)" -ForegroundColor DarkYellow
}

### 7) Prescription flow

Write-Host "`n=== PRESCRIPTION FLOW ===" -ForegroundColor Cyan

$prescriptionId = $null

if ($medicalRecordId -and $doctorHeaders.Count -gt 0) {
    $presBody = @{
        medicalRecordId = $medicalRecordId
        validUntil      = [DateTime]::UtcNow.AddDays(7)
        notes           = "Take as prescribed"

        items = @(
            @{
                medicineName = "Paracetamol"
                dosage       = "500mg"
                frequency    = "Twice daily"
                duration     = "5 days"
                quantity     = 10
                instructions = "After meals"
            }
        )
    }

    $prRes = Call-Api -Name "Create prescription" -Method "POST" `
        -Url "$BaseUrl/api/prescriptions" `
        -Body $presBody -Headers $doctorHeaders -ExpectedStatus 201

    if ($prRes.ok -and $prRes.body) {
        $prescriptionId = $prRes.body.id
        Write-Host "   PrescriptionId: $prescriptionId" -ForegroundColor Green

        Call-Api -Name "Get prescription by id" -Method "GET" `
            -Url "$BaseUrl/api/prescriptions/$prescriptionId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get patient prescriptions" -Method "GET" `
            -Url "$BaseUrl/api/prescriptions/patient/$patientId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get doctor prescriptions" -Method "GET" `
            -Url "$BaseUrl/api/prescriptions/doctor/$doctorId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Generate prescription PDF" -Method "GET" `
            -Url "$BaseUrl/api/prescriptions/$prescriptionId/pdf" `
            -Headers $doctorHeaders -ExpectedStatus 200 -AllowFail | Out-Null
    }
} else {
    Write-Host "Skipping prescription flow (missing medical record or doctor token)" -ForegroundColor DarkYellow
}

### 8) Lab test flow

Write-Host "`n=== LAB TEST FLOW ===" -ForegroundColor Cyan

$labTestId = $null

if ($patientId -and $doctorId -and $doctorHeaders.Count -gt 0) {
    $labBody = @{
        patientId      = $patientId
        doctorId       = $doctorId
        medicalRecordId = $medicalRecordId
        testName       = "Complete Blood Count"
        testType       = "Hematology"
        notes          = "Routine lab test"
    }

    $ltRes = Call-Api -Name "Order lab test" -Method "POST" `
        -Url "$BaseUrl/api/labtests" `
        -Body $labBody -Headers $doctorHeaders -ExpectedStatus 201

    if ($ltRes.ok -and $ltRes.body) {
        $labTestId = $ltRes.body.id
        Write-Host "   LabTestId: $labTestId" -ForegroundColor Green

        Call-Api -Name "Get lab test by id" -Method "GET" `
            -Url "$BaseUrl/api/labtests/$labTestId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get patient lab tests" -Method "GET" `
            -Url "$BaseUrl/api/labtests/patient/$patientId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get doctor lab tests" -Method "GET" `
            -Url "$BaseUrl/api/labtests/doctor/$doctorId" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Lab test statistics" -Method "GET" `
            -Url "$BaseUrl/api/labtests/doctor/$doctorId/statistics" `
            -Headers $doctorHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Cancel lab test" -Method "POST" `
            -Url "$BaseUrl/api/labtests/$labTestId/cancel" `
            -Headers $doctorHeaders -ExpectedStatus 200 -AllowFail | Out-Null

        Call-Api -Name "Generate lab test PDF" -Method "GET" `
            -Url "$BaseUrl/api/labtests/$labTestId/report-pdf" `
            -Headers $doctorHeaders -ExpectedStatus 200 -AllowFail | Out-Null
    }
} else {
    Write-Host "Skipping lab test flow (missing ids or doctor token)" -ForegroundColor DarkYellow
}

### 9) Invoice & payment flow

Write-Host "`n=== INVOICE & PAYMENT FLOW ===" -ForegroundColor Cyan

$invoiceId = $null

if ($appointmentId -and $patientId -and $adminHeaders.Count -gt 0) {
    $invBody = @{
        appointmentId  = $appointmentId
        dueDate        = [DateTime]::UtcNow.AddDays(14)
        taxAmount      = 50.0
        discountAmount = 0.0
        notes          = "Invoice generated from appointment (E2E test)"
        additionalItems = @(
            @{
                description = "Blood test"
                itemType    = "Lab"
                quantity    = 1
                unitPrice   = 500.0
            }
        )
    }

    $ciRes = Call-Api -Name "Create invoice from appointment" -Method "POST" `
        -Url "$BaseUrl/api/invoice/from-appointment" `
        -Body $invBody -Headers $adminHeaders -ExpectedStatus 201

    if ($ciRes.ok -and $ciRes.body) {
        $invoiceId = $ciRes.body.id
        Write-Host "   InvoiceId: $invoiceId" -ForegroundColor Green

        Call-Api -Name "Get invoice by id" -Method "GET" `
            -Url "$BaseUrl/api/invoice/$invoiceId" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Get patient invoices" -Method "GET" `
            -Url "$BaseUrl/api/invoice/patient/$patientId" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "List invoices" -Method "GET" `
            -Url "$BaseUrl/api/invoice?page=1&pageSize=10" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Overdue invoices" -Method "GET" `
            -Url "$BaseUrl/api/invoice/overdue" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Invoice revenue statistics" -Method "GET" `
            -Url "$BaseUrl/api/invoice/statistics/revenue" `
            -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

        # Create payment for invoice
        $amountToPay = $ciRes.body.totalAmount
        if (-not $amountToPay) { $amountToPay = 500.0 }

        $payBody = @{
            invoiceId     = $invoiceId
            amount        = $amountToPay
            paymentMethod = "Cash"
            paymentDate   = [DateTime]::UtcNow
            transactionId = "TX-" + ([Guid]::NewGuid().ToString("N").Substring(0,8))
            notes         = "Full payment (E2E test)"
        }

        $payRes = Call-Api -Name "Create payment" -Method "POST" `
            -Url "$BaseUrl/api/payment" `
            -Body $payBody -Headers $adminHeaders -ExpectedStatus 201

        if ($payRes.ok -and $payRes.body) {
            $paymentId = $payRes.body.id
            Write-Host "   PaymentId: $paymentId" -ForegroundColor Green

            Call-Api -Name "Get payment by id" -Method "GET" `
                -Url "$BaseUrl/api/payment/$paymentId" `
                -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

            Call-Api -Name "Get invoice payments" -Method "GET" `
                -Url "$BaseUrl/api/payment/invoice/$invoiceId" `
                -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

            if ($patientId) {
                Call-Api -Name "Get patient payments" -Method "GET" `
                    -Url "$BaseUrl/api/payment/patient/$patientId" `
                    -Headers $adminHeaders -ExpectedStatus 200 | Out-Null
            }

            $statsRes = Call-Api -Name "Payment statistics" -Method "GET" `
                -Url "$BaseUrl/api/payment/statistics" `
                -Headers $adminHeaders -ExpectedStatus 200

            $statusUpdateBody = @{ newStatus = "Completed" }
            Call-Api -Name "Update payment status" -Method "PATCH" `
                -Url "$BaseUrl/api/payment/$paymentId/status" `
                -Body $statusUpdateBody -Headers $adminHeaders -ExpectedStatus 200 -AllowFail | Out-Null

            $refundBody = @{ reason = "Test refund" }
            Call-Api -Name "Refund payment" -Method "POST" `
                -Url "$BaseUrl/api/payment/$paymentId/refund" `
                -Body $refundBody -Headers $adminHeaders -ExpectedStatus 200 -AllowFail | Out-Null
        }

        Call-Api -Name "Send invoice by email" -Method "POST" `
            -Url "$BaseUrl/api/invoice/$invoiceId/send" `
            -Headers $adminHeaders -ExpectedStatus 200 -AllowFail | Out-Null
    }
} else {
    Write-Host "Skipping invoice/payment flow (missing appointment/patient/admin)" -ForegroundColor DarkYellow
}

### 10) Notifications flow

Write-Host "`n=== NOTIFICATIONS FLOW ===" -ForegroundColor Cyan

if ($adminHeaders.Count -gt 0 -and $tokens.ContainsKey("Patient")) {
    $notifBody = @{
        userId          = $tokens["Patient"].UserId
        type            = "GeneralNotification"
        title           = "Test Notification"
        message         = "This is a test notification from the E2E script."
        actionUrl       = "https://example.com"
        relatedEntityId = $appointmentId
    }

    Call-Api -Name "Send test notification" -Method "POST" `
        -Url "$BaseUrl/api/notifications/test" `
        -Body $notifBody -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

    $patientNotifHeaders = Get-AuthHeader -Label "Patient"

    if ($patientNotifHeaders.Count -gt 0) {
        Call-Api -Name "My notifications" -Method "GET" `
            -Url "$BaseUrl/api/notifications/my-notifications" `
            -Headers $patientNotifHeaders -ExpectedStatus 200 | Out-Null

        Call-Api -Name "Unread count" -Method "GET" `
            -Url "$BaseUrl/api/notifications/unread-count" `
            -Headers $patientNotifHeaders -ExpectedStatus 200 | Out-Null
    }
} else {
    Write-Host "Skipping notifications flow (missing admin/patient)" -ForegroundColor DarkYellow
}

### 11) Background jobs flow

Write-Host "`n=== BACKGROUND JOBS FLOW ===" -ForegroundColor Cyan

if ($adminHeaders.Count -gt 0) {
    Call-Api -Name "Job statistics" -Method "GET" `
        -Url "$BaseUrl/api/backgroundjobs/statistics" `
        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

    Call-Api -Name "Trigger appointment reminders" -Method "POST" `
        -Url "$BaseUrl/api/backgroundjobs/trigger-appointment-reminders" `
        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

    Call-Api -Name "Trigger overdue invoice reminders" -Method "POST" `
        -Url "$BaseUrl/api/backgroundjobs/trigger-overdue-invoice-reminders" `
        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

    Call-Api -Name "Trigger daily summary" -Method "POST" `
        -Url "$BaseUrl/api/backgroundjobs/trigger-daily-summary" `
        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

    Call-Api -Name "Trigger overdue status update" -Method "POST" `
        -Url "$BaseUrl/api/backgroundjobs/trigger-update-overdue-status" `
        -Headers $adminHeaders -ExpectedStatus 200 | Out-Null

    if ($appointmentId) {
        $reminderTime = [DateTime]::UtcNow.AddMinutes(5)
        Call-Api -Name "Schedule appointment reminder" -Method "POST" `
            -Url "$BaseUrl/api/backgroundjobs/schedule-appointment-reminder/$appointmentId?reminderTime=$($reminderTime.ToString("o"))" `
            -Headers $adminHeaders -ExpectedStatus 200 -AllowFail | Out-Null
    }
} else {
    Write-Host "Skipping background jobs flow (no admin token)" -ForegroundColor DarkYellow
}

### Final result

Write-Host "`n=== FINAL RESULT ===" -ForegroundColor Cyan
if ($script:FailedCount -eq 0) {
    Write-Host "All end-to-end tests PASSED." -ForegroundColor Green
    exit 0
} else {
    Write-Host "$($script:FailedCount) test(s) FAILED." -ForegroundColor Red
    exit 1
}