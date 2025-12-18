# Testing Plan for Waste Collection System

## 1. Test Scope and Objectives

### Objectives
- Verify application starts and runs correctly
- Validate core functionality (user registration, waste requests, payments)
- Ensure admin operations work (approval, truck assignment)
- Test payment integration with MTN MoMo sandbox
- Verify database operations and data integrity

### Scope
- **Functional Testing**: All user and admin features
- **Integration Testing**: Payment gateway, email notifications
- **Database Testing**: Migrations, data persistence
- **API Testing**: REST endpoints, authentication

## 2. Test Environments

| Environment | Configuration | Purpose |
|------------|--------------|---------|
| Local Development | `ASPNETCORE_ENVIRONMENT=Development` | Development and initial testing |
| Docker Container | `docker-compose up` | Containerized testing |
| Production-like | `ASPNETCORE_ENVIRONMENT=Production` | Pre-deployment validation |

## 3. Test Cases

### 3.1 Smoke Tests (Automated)

**Test Case TC-001: Application Startup**
- **Objective**: Verify application starts without errors
- **Steps**:
  1. Run `dotnet run --project WasteCollectionSystem.csproj`
  2. Check console for "Application started" message
  3. Verify no exceptions in logs
- **Expected Result**: Application starts on http://localhost:5223 or specified port
- **Status**: ✅ Automated in `SmokeTests.cs`

**Test Case TC-002: Homepage Accessibility**
- **Objective**: Verify homepage loads successfully
- **Steps**:
  1. Navigate to `http://localhost:5223/`
  2. Check HTTP response code
- **Expected Result**: HTTP 200 OK
- **Status**: ✅ Automated in `SmokeTests.cs`

**Test Case TC-003: Database Migrations**
- **Objective**: Verify EF Core migrations apply correctly
- **Steps**:
  1. Check `ApplicationDbContext` migrations
  2. Verify `Payment.Amount` has precision (18,2)
- **Expected Result**: All migrations applied, no errors
- **Status**: ✅ Verified in `ApplicationDbContext.cs`

### 3.2 Functional Tests (Manual)

#### User Registration and Authentication

**Test Case TC-004: User Registration**
- **Objective**: New user can register successfully
- **Steps**:
  1. Navigate to `/Identity/Account/Register`
  2. Fill in: First Name, Last Name, Email, Phone, Address, Password
  3. Click "Register"
- **Expected Result**: User created, redirected to login or dashboard
- **Test Data**: 
  - Email: `testuser@example.com`
  - Password: `Test@123456`

**Test Case TC-005: User Login**
- **Objective**: Registered user can log in
- **Steps**:
  1. Navigate to `/Identity/Account/Login`
  2. Enter email and password
  3. Click "Login"
- **Expected Result**: User logged in, redirected to dashboard

**Test Case TC-006: Invalid Login**
- **Objective**: System rejects invalid credentials
- **Steps**:
  1. Enter wrong email/password
  2. Attempt login
- **Expected Result**: Error message displayed, user not logged in

#### Waste Request Management

**Test Case TC-007: Submit Waste Request**
- **Objective**: User can submit waste collection request
- **Steps**:
  1. Login as regular user
  2. Navigate to request submission page
  3. Fill in: Waste Type, Location, Notes
  4. Submit request
- **Expected Result**: Request created with status "Pending"

**Test Case TC-008: View Request Status**
- **Objective**: User can view their request status
- **Steps**:
  1. Login as user
  2. Navigate to dashboard
  3. View "My Requests" section
- **Expected Result**: All user's requests displayed with current status

**Test Case TC-009: Guest Request Submission**
- **Objective**: Guest users can submit requests
- **Steps**:
  1. Navigate to `/GuestRequest` (without login)
  2. Fill in request details
  3. Submit
- **Expected Result**: Request created, added to guest cart

#### Payment Processing

**Test Case TC-010: Add Items to Cart**
- **Objective**: User can add requests to cart for payment
- **Steps**:
  1. Login as user
  2. Submit waste request
  3. Add to cart
- **Expected Result**: Cart count increases, items visible in cart

**Test Case TC-011: Initiate MTN MoMo Payment**
- **Objective**: User can initiate payment via MTN Mobile Money
- **Steps**:
  1. Login as user
  2. Go to payment page
  3. Enter phone number (sandbox format: 46733123450)
  4. Enter amount
  5. Click "Pay with MoMo"
- **Expected Result**: Payment initiated, transaction ID returned
- **Note**: Uses MTN MoMo sandbox environment

**Test Case TC-012: Payment Status Check**
- **Objective**: System can check payment transaction status
- **Steps**:
  1. After initiating payment, check transaction status
  2. Use transaction ID from TC-011
- **Expected Result**: Status returned (PENDING, SUCCESSFUL, or FAILED)

#### Admin Operations

**Test Case TC-013: Admin Login**
- **Objective**: Admin can access admin dashboard
- **Steps**:
  1. Login with admin credentials
  2. Navigate to `/Admin`
- **Expected Result**: Admin dashboard displayed

**Test Case TC-014: View All Requests**
- **Objective**: Admin can view all waste requests
- **Steps**:
  1. Login as admin
  2. Navigate to "Requests" section
- **Expected Result**: All requests listed with details

**Test Case TC-015: Approve Request**
- **Objective**: Admin can approve waste requests
- **Steps**:
  1. Login as admin
  2. Find pending request
  3. Click "Approve"
- **Expected Result**: Request status changes to "Approved", user notified

**Test Case TC-016: Reject Request**
- **Objective**: Admin can reject waste requests
- **Steps**:
  1. Login as admin
  2. Find pending request
  3. Click "Reject"
- **Expected Result**: Request status changes to "Rejected", user notified

**Test Case TC-017: Assign Truck to Request**
- **Objective**: Admin can assign truck and driver
- **Steps**:
  1. Login as admin
  2. Select approved request
  3. Assign available truck
  4. Assign driver
- **Expected Result**: Truck assigned, request status updates, driver notified

**Test Case TC-018: Update Collection Status**
- **Objective**: Admin can mark collection as completed
- **Steps**:
  1. Login as admin
  2. Find request with assigned truck
  3. Update status to "Completed"
- **Expected Result**: Status updated, completion date recorded

#### Notifications

**Test Case TC-019: Receive Notification**
- **Objective**: User receives notification on status change
- **Steps**:
  1. Login as user
  2. Admin approves user's request
  3. Check notifications
- **Expected Result**: Notification appears in user's notification list

**Test Case TC-020: Email Notification**
- **Objective**: User receives email on status change
- **Steps**:
  1. Ensure SMTP configured
  2. Admin approves request
  3. Check user's email
- **Expected Result**: Email received with status update

### 3.3 API Tests

**Test Case TC-021: API Authentication**
- **Objective**: API endpoints require authentication
- **Steps**:
  1. Call `/api/payments/pay` without token
  2. Check response
- **Expected Result**: HTTP 401 Unauthorized

**Test Case TC-022: Authenticated API Call**
- **Objective**: Authenticated users can access API
- **Steps**:
  1. Login via `/api/auth/login`
  2. Get JWT token
  3. Call `/api/payments/pay` with token in header
- **Expected Result**: API call succeeds

### 3.4 Integration Tests

**Test Case TC-023: Database Connection**
- **Objective**: Application connects to SQL Server
- **Steps**:
  1. Start application
  2. Check logs for database connection
- **Expected Result**: Connection successful, migrations applied

**Test Case TC-024: MTN MoMo Integration**
- **Objective**: Payment service integrates with MTN MoMo API
- **Steps**:
  1. Configure MTN MoMo credentials
  2. Initiate test payment
  3. Verify API call to MTN
- **Expected Result**: Payment request sent, response received

## 4. Test Execution

### Automated Tests
```bash
# Run all automated tests
dotnet test tests/WasteCollectionSystem.Tests/WasteCollectionSystem.Tests.csproj

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Manual Test Execution
1. Follow test cases TC-004 through TC-024 in sequence
2. Document results in test execution log
3. Report any defects found

## 5. Test Data

### Test Users
- **Regular User**: `user@test.com` / `Password123!`
- **Admin User**: `admin@test.com` / `Admin123!`

### Test Phone Numbers (MTN MoMo Sandbox)
- Valid: `46733123450`
- Invalid: `1234567890`

## 6. Defect Management

### Severity Levels
- **Critical**: Application crashes, data loss
- **High**: Core feature not working
- **Medium**: Feature works with workaround
- **Low**: UI/UX issues, minor bugs

### Defect Reporting Template
```
Test Case: TC-XXX
Description: [What was tested]
Steps to Reproduce: [Detailed steps]
Expected Result: [What should happen]
Actual Result: [What actually happened]
Severity: [Critical/High/Medium/Low]
```

## 7. Exit Criteria

✅ **All smoke tests pass**  
✅ **All critical functional tests pass**  
✅ **Payment integration works with sandbox**  
✅ **No blocking defects in core workflows**  
✅ **Application runs successfully in Docker container**

## 8. Test Schedule

| Phase | Duration | Activities |
|-------|----------|------------|
| Smoke Testing | 1 day | Automated tests, basic functionality |
| Functional Testing | 3 days | Manual test cases TC-004 to TC-020 |
| Integration Testing | 2 days | API and external service integration |
| Regression Testing | 1 day | Re-test after fixes |

## 9. Test Tools

- **Automated Testing**: xUnit, Microsoft.AspNetCore.Mvc.Testing
- **API Testing**: Postman or Swagger UI (available at `/swagger`)
- **Database Testing**: SQL Server Management Studio
- **Browser Testing**: Chrome, Firefox, Edge

## 10. Test Results Summary

| Category | Total Tests | Passed | Failed | Blocked |
|----------|-------------|--------|--------|---------|
| Smoke Tests | 3 | _ | _ | _ |
| Functional Tests | 17 | _ | _ | _ |
| API Tests | 2 | _ | _ | _ |
| Integration Tests | 2 | _ | _ | _ |
| **Total** | **24** | _ | _ | _ |

*Fill in results during test execution*

