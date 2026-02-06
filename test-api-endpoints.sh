#!/bin/bash

# Healthcare System API Endpoint Tester (Bash Version)
# Tests all major endpoints automatically

BASE_URL="https://localhost:7227"  # Update with your port
OUTPUT_FILE="api-test-results-$(date +%Y-%m-%d-%H%M%S).txt"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Counters
TOTAL=0
PASSED=0
FAILED=0

# Tokens
ADMIN_TOKEN=""
DOCTOR_TOKEN=""
PATIENT_TOKEN=""

# Functions
log_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
}

log_info() {
    echo -e "${CYAN}ℹ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

test_endpoint() {
    local name="$1"
    local method="$2"
    local endpoint="$3"
    local token="$4"
    local body="$5"
    local expected_status="${6:-200}"
    
    TOTAL=$((TOTAL + 1))
    
    local headers=("-H" "Content-Type: application/json")
    if [ -n "$token" ]; then
        headers+=("-H" "Authorization: Bearer $token")
    fi
    
    local curl_args=(-k -s -w "\n%{http_code}" -X "$method")
    curl_args+=("${headers[@]}")
    
    if [ -n "$body" ]; then
        curl_args+=(-d "$body")
    fi
    
    curl_args+=("$BASE_URL$endpoint")
    
    response=$(curl "${curl_args[@]}")
    status_code=$(echo "$response" | tail -n 1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$status_code" = "$expected_status" ]; then
        log_success "$name - Status: $status_code"
        PASSED=$((PASSED + 1))
        echo "$name - PASSED - Status: $status_code" >> "$OUTPUT_FILE"
        echo "$body"
    else
        log_error "$name - Expected: $expected_status, Got: $status_code"
        FAILED=$((FAILED + 1))
        echo "$name - FAILED - Expected: $expected_status, Got: $status_code" >> "$OUTPUT_FILE"
        echo ""
    fi
}

# Start
echo -e "\n${YELLOW}========================================"
echo "  Healthcare System API Test Suite"
echo -e "========================================${NC}\n"

log_info "Base URL: $BASE_URL"
log_info "Starting tests at $(date '+%Y-%m-%d %H:%M:%S')\n"

echo "Healthcare System API Test Report" > "$OUTPUT_FILE"
echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')" >> "$OUTPUT_FILE"
echo "Base URL: $BASE_URL" >> "$OUTPUT_FILE"
echo "========================================\n" >> "$OUTPUT_FILE"

# Test 1: Health Check
echo -e "\n${YELLOW}--- HEALTH CHECK ---${NC}"
test_endpoint "Health Check" "GET" "/health"

# Test 2: Authentication
echo -e "\n${YELLOW}--- AUTHENTICATION ---${NC}"

# Login Admin
admin_response=$(test_endpoint "Admin Login" "POST" "/api/auth/login" "" '{"email":"admin@healthcare.com","password":"Admin@123"}')
ADMIN_TOKEN=$(echo "$admin_response" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
if [ -n "$ADMIN_TOKEN" ]; then
    log_info "Admin token obtained"
fi

# Login Doctor
doctor_response=$(test_endpoint "Doctor Login" "POST" "/api/auth/login" "" '{"email":"doctor@healthcare.com","password":"Doctor@123"}')
DOCTOR_TOKEN=$(echo "$doctor_response" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
if [ -n "$DOCTOR_TOKEN" ]; then
    log_info "Doctor token obtained"
fi

# Login Patient
patient_response=$(test_endpoint "Patient Login" "POST" "/api/auth/login" "" '{"email":"patient@healthcare.com","password":"Patient@123"}')
PATIENT_TOKEN=$(echo "$patient_response" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
if [ -n "$PATIENT_TOKEN" ]; then
    log_info "Patient token obtained"
fi

# Test 3: Patients
echo -e "\n${YELLOW}--- PATIENT ENDPOINTS ---${NC}"
test_endpoint "Get All Patients" "GET" "/api/patients?page=1&pageSize=10" "$ADMIN_TOKEN"

# Test 4: Doctors
echo -e "\n${YELLOW}--- DOCTOR ENDPOINTS ---${NC}"
test_endpoint "Get All Doctors" "GET" "/api/doctors?page=1&pageSize=10" "$ADMIN_TOKEN"
test_endpoint "Get Available Doctors" "GET" "/api/doctors/available" "$PATIENT_TOKEN"

# Test 5: Appointments
echo -e "\n${YELLOW}--- APPOINTMENT ENDPOINTS ---${NC}"
test_endpoint "Get All Appointments" "GET" "/api/appointments?page=1&pageSize=10" "$ADMIN_TOKEN"
test_endpoint "Get Today's Appointments" "GET" "/api/appointments/today" "$ADMIN_TOKEN"
test_endpoint "Get Appointment Statistics" "GET" "/api/appointments/statistics" "$ADMIN_TOKEN"

# Test 6: Notifications
echo -e "\n${YELLOW}--- NOTIFICATIONS ---${NC}"
test_endpoint "Get My Notifications" "GET" "/api/notifications/my-notifications" "$PATIENT_TOKEN"
test_endpoint "Get Unread Count" "GET" "/api/notifications/unread-count" "$PATIENT_TOKEN"

# Test 7: Dashboard
echo -e "\n${YELLOW}--- DASHBOARD ---${NC}"
test_endpoint "Get Admin Dashboard" "GET" "/api/dashboard/admin" "$ADMIN_TOKEN"

# Test 8: Authorization Tests
echo -e "\n${YELLOW}--- AUTHORIZATION TESTS ---${NC}"
test_endpoint "Patient accessing admin dashboard (should fail)" "GET" "/api/dashboard/admin" "$PATIENT_TOKEN" "" "403"
test_endpoint "Unauthenticated access (should fail)" "GET" "/api/doctors" "" "" "401"

# Summary
echo -e "\n${YELLOW}========================================"
echo "  TEST RESULTS SUMMARY"
echo -e "========================================${NC}\n"

echo "Total Tests:   $TOTAL"
log_success "Passed:        $PASSED"
log_error "Failed:        $FAILED"

if [ "$TOTAL" -gt 0 ]; then
    PASS_RATE=$(awk "BEGIN {printf \"%.2f\", ($PASSED/$TOTAL)*100}")
    echo -e "\nPass Rate:     $PASS_RATE%"
fi

echo -e "\n${CYAN}Detailed report saved to: $OUTPUT_FILE${NC}"
echo -e "${CYAN}Test execution completed at $(date '+%Y-%m-%d %H:%M:%S')${NC}"
echo -e "${YELLOW}========================================${NC}\n"