# Testing Guide

**Document Type**: Living Document - Testing & Quality Assurance  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This guide provides comprehensive testing strategies, procedures, and scenarios for the Bellwood Global Mobile App. It covers unit testing, integration testing, manual testing, and automated test scripts.

**Testing Philosophy**:
- ?? **Test Early, Test Often** - Catch bugs before they reach users
- ?? **Automated Where Possible** - Use scripts to save time
- ?? **Device Testing** - Test on real devices, not just emulators
- ?? **Security Testing** - Verify auth, authorization, data isolation
- ? **Performance Testing** - Monitor speed, memory, battery

---

## ?? Test Types

### Unit Tests

**Purpose**: Test individual components in isolation

**Framework**: xUnit  
**Location**: `BellwoodGlobal.Mobile.Tests/UnitTests/`

**Coverage**:
- Services (`AdminApi`, `ConfigurationService`, etc.)
- View models (future)
- Utility classes
- Data models

**Run Command**:
```bash
dotnet test --filter Category=Unit
```

**Example Unit Test**:
```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task ConfigurationService_LoadAsync_ReturnsSettings()
{
    // Arrange
    var config = new ConfigurationService();
    
    // Act
    var apiUrl = await config.GetAdminApiUrlAsync();
    
    // Assert
    Assert.NotNull(apiUrl);
    Assert.StartsWith("https://", apiUrl);
}
```

---

### Integration Tests

**Purpose**: Test component interactions and API communication

**Framework**: xUnit  
**Location**: `BellwoodGlobal.Mobile.Tests/IntegrationTests/`

**Prerequisites**:
- AdminAPI running on `https://localhost:5206`
- AuthServer running on `https://localhost:5001`
- Valid test user account

**Run Command**:
```bash
dotnet test --filter Category=Integration
```

**Example Integration Test**:
```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task AdminApi_GetQuotesAsync_ReturnsQuotes()
{
    // Arrange
    var api = CreateAuthenticatedAdminApi();
    
    // Act
    var quotes = await api.GetQuotesAsync(10);
    
    // Assert
    Assert.NotNull(quotes);
    Assert.True(quotes.Count > 0);
}
```

---

### Manual UI Testing

**Purpose**: Verify user experience, visual appearance, touch interactions

**Tools**:
- Android Emulator
- iOS Simulator
- Physical devices (recommended)

**Test Areas**:
- Page navigation
- Button clicks
- Form validation
- Visual layout
- Touch gestures
- Orientation changes

See **Manual Testing Scenarios** section below for detailed test cases.

---

### Automated Test Scripts

**Purpose**: Automate backend setup, data seeding, status changes

**Framework**: PowerShell 5.1+  
**Location**: `Scripts/`

**Available Scripts**:
- `Test-Environment.ps1` - Environment health check
- `Seed-TestQuotes.ps1` - Create test data
- `Simulate-StatusChange.ps1` - Trigger status transitions
- `Get-QuoteInfo.ps1` - View quote details
- `Setup-MultiUserTest.ps1` - Multi-user testing

See **Automated Testing** section below for usage details.

---

## ?? Running Tests

### All Tests

```bash
# Run all tests (unit + integration)
dotnet test

# With verbose output
dotnet test --verbosity detailed

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

---

### Unit Tests Only

```bash
dotnet test --filter Category=Unit
```

**Expected Output**:
```
Passed! - Failed:     0, Passed:    45, Skipped:     0, Total:    45
```

---

### Integration Tests Only

```bash
# Ensure services are running first
cd ../AdminAPI && dotnet run &
cd ../BellwoodGlobal.Mobile

# Run integration tests
dotnet test --filter Category=Integration
```

**Expected Output**:
```
Passed! - Failed:     0, Passed:    12, Skipped:     0, Total:    12
```

---

### Specific Test Class

```bash
dotnet test --filter FullyQualifiedName~AdminApiTests
```

---

### Specific Test Method

```bash
dotnet test --filter FullyQualifiedName~AdminApi_GetQuotesAsync_ReturnsQuotes
```

---

## ?? Automated Testing

### Prerequisites

**Software**:
- PowerShell 5.1+ (included with Windows)
- .NET 9.0 SDK
- AdminAPI running
- AuthServer running

**Verification**:
```powershell
# Check PowerShell version
$PSVersionTable.PSVersion
# Expected: 5.1 or higher

# Check .NET SDK
dotnet --version
# Expected: 9.0.x
```

---

### Step 1: Environment Health Check

**Purpose**: Verify all services are running

**Command**:
```powershell
cd Scripts
.\Test-Environment.ps1
```

**Expected Output**:
```
======================================
Phase Alpha Environment Health Check
======================================

Testing AdminAPI at https://localhost:5206... ? OK (Status: 200)
Testing Auth Server at https://localhost:5001... ? OK (Status: 200)

======================================
Health Check Summary
======================================
Service            Status              Url
-------            ------              ---
AdminAPI          ? Running           https://localhost:5206
Auth Server       ? Running           https://localhost:5001

? All services are healthy and ready for testing!
```

**Troubleshooting**: See `32-Troubleshooting.md` if services are down.

---

### Step 2: Seed Test Data

**Purpose**: Create quotes with various details for testing

**Command**:
```powershell
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
```

**Parameters**:
- `-UserEmail` - Email of test user (required)
- `-UserFirstName` - First name (default: "Test")
- `-UserLastName` - Last name (default: "User")

**Expected Output**:
```
Creating quote: John Doe - Sedan... ? Created (ID: quote-abc-123)
Creating quote: Jane Smith - SUV... ? Created (ID: quote-def-456)
...

======================================
Summary
======================================
Created:  5 quotes
Failed:   0 quotes
```

**What It Creates**:
- 5 test quotes in Pending status
- Different passengers, vehicles, locations
- Ready for status transitions

---

### Step 3: View Created Quotes

**Purpose**: List all quotes to get IDs for testing

**Command**:
```powershell
.\Get-QuoteInfo.ps1
```

**Expected Output**:
```
Found 5 quotes:

quote-abc-123 ? Pending      ? John Doe             ? Sedan
quote-def-456 ? Pending      ? Jane Smith           ? SUV
quote-ghi-789 ? Pending      ? Bob Johnson          ? Executive Sedan
...
```

**Copy Quote IDs** for use in next steps.

---

### Step 4: Simulate Status Changes

**Purpose**: Change quote statuses to test app polling and UI updates

**4a. Acknowledge a Quote** (Pending ? Acknowledged)

```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-def-456" -Action Acknowledge
```

**4b. Respond with Price** (Acknowledged ? Responded)

```powershell
# First acknowledge
.\Simulate-StatusChange.ps1 -QuoteId "quote-ghi-789" -Action Acknowledge

# Then respond with price
.\Simulate-StatusChange.ps1 -QuoteId "quote-ghi-789" -Action Respond -EstimatedPrice 95.50
```

**4c. Cancel a Quote** (Any status ? Cancelled)

```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-mno-345" -Action Cancel
```

**Test Polling with Auto-Wait**:

```powershell
# Change status and wait 30 seconds to watch app poll
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge -WaitForPolling
```

---

### Step 5: Multi-User Testing

**Purpose**: Test data isolation (users only see their own quotes)

**Command**:
```powershell
.\Setup-MultiUserTest.ps1 -UserCount 3
```

**What It Creates**:
- TestUser1, TestUser2, TestUser3
- Credentials exported to `TestUsers.csv`
- Commands to seed quotes for each user

**Manual Steps**:
1. Create user accounts in AuthServer (if auto-registration disabled)
2. Seed quotes for each user using provided commands
3. Login as User1, verify only User1's quotes appear
4. Login as User2, verify only User2's quotes appear

---

## ?? Manual Testing Scenarios

### Scenario 1: View Quote Dashboard

**Objective**: Verify quote list displays correctly

**Steps**:
1. Launch mobile app
2. Login with test credentials
3. Navigate to Quote Dashboard
4. Observe quote list

**Expected Results**:
- ? All user's quotes appear
- ? Status badges show correct colors:
  - ?? Orange = Awaiting Response (Pending)
  - ?? Blue = Under Review (Acknowledged)
  - ?? Green = Response Received (Responded)
  - ? Gray = Booking Created (Accepted)
  - ?? Red = Cancelled
- ? Responded quotes show price in badge
- ? Each quote shows passenger, vehicle, pickup info
- ? Quotes sorted by created date (newest first)

---

### Scenario 2: Test Automatic Polling

**Objective**: Verify quotes auto-update every 30 seconds

**Setup**:
1. Open Quote Dashboard
2. Position PowerShell and mobile device side-by-side

**Steps**:
1. In PowerShell: `.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Acknowledge`
2. Watch mobile app (don't touch it)
3. Wait up to 30 seconds

**Expected Results**:
- ? Quote status updates automatically (no manual refresh)
- ? Gold notification banner appears at top
- ? Banner message: "Quote for [PassengerName] has been updated"
- ? Banner auto-dismisses after 5 seconds
- ? Status badge color changes (orange ? blue)

---

### Scenario 3: View Quote Details (Responded Status)

**Objective**: Verify dispatcher response section displays

**Steps**:
1. Open Quote Dashboard
2. Tap a "Response Received" quote
3. Scroll through detail page

**Expected Results**:
- ? Status chip shows "Response Received" (green)
- ? **Dispatcher Response** section visible with:
  - Estimated price (formatted as currency)
  - Price disclaimer
  - Estimated pickup time
  - Dispatcher notes (if provided)
- ? **Action buttons** visible:
  - "? Accept Quote & Create Booking" (gold, primary)
  - "? Cancel Quote Request" (charcoal, secondary)
- ? Trip details section shows all info
- ? Passenger/booker contact info displays

---

### Scenario 4: Accept Quote

**Objective**: Verify accept flow creates booking and navigates

**Steps**:
1. Open a Responded quote (with price)
2. Tap "? Accept Quote & Create Booking"
3. Wait for API call to complete

**Expected Results**:
- ? Button disables during API call (prevents double-tap)
- ? Success dialog appears with message:
  - "Quote accepted! Your booking has been created."
- ? Dialog offers two buttons:
  - "View Booking" (primary)
  - "OK" (secondary)
- ? Tapping "View Booking" navigates to BookingDetailPage
- ? Booking displays with status "Requested"
- ? Booking contains same details as quote
- ? Returning to dashboard shows quote as "Booking Created" (gray)

---

### Scenario 5: Cancel Quote

**Objective**: Verify cancel flow with confirmation

**Steps**:
1. Open a Pending or Responded quote
2. Tap "? Cancel Quote Request"
3. Observe confirmation dialog

**Expected Results**:
- ? Confirmation dialog appears
- ? Dialog title: "Cancel Quote?"
- ? Message: "Are you sure you want to cancel this quote request? This action cannot be undone."
- ? Buttons: "Yes, Cancel" and "No"
- ? Tapping "No" dismisses dialog, no change
- ? Tapping "Yes, Cancel" disables button
- ? Success message appears: "Your quote request has been cancelled."
- ? App navigates back to dashboard
- ? Quote shows "Cancelled" status (red)
- ? Opening cancelled quote shows no action buttons

---

### Scenario 6: Filter Quotes by Status

**Objective**: Verify filter buttons work correctly

**Steps**:
1. Open Dashboard with multiple quotes
2. Tap "Awaiting Response" filter
3. Tap "Response Received" filter
4. Tap "Cancelled" filter
5. Tap "All" filter

**Expected Results**:
- ? "Awaiting Response": Shows only Pending/Acknowledged
- ? "Response Received": Shows only Responded quotes (with prices)
- ? "Cancelled": Shows only Cancelled quotes
- ? "All": Shows all quotes
- ? Active filter highlighted in gold
- ? Inactive filters in charcoal

---

### Scenario 7: Search Quotes

**Objective**: Verify search functionality

**Steps**:
1. Enter "John" in search box
2. Clear search, enter "SUV"
3. Clear search, enter "O'Hare"
4. Clear search completely

**Expected Results**:
- ? "John": Shows only quotes with "John" in passenger name
- ? "SUV": Shows only SUV vehicle quotes
- ? "O'Hare": Shows quotes with O'Hare in pickup location
- ? Clearing search returns all quotes
- ? Search is case-insensitive

---

### Scenario 8: Pull-to-Refresh

**Objective**: Verify manual refresh works

**Steps**:
1. Open Quote Dashboard
2. Pull down from top of list
3. Release

**Expected Results**:
- ? Refresh spinner appears
- ? Quote list reloads from API
- ? Spinner disappears
- ? List updates with latest data

---

### Scenario 9: Test Notification Banner

**Objective**: Verify notification banner behavior

**Setup**:
1. Open Quote Dashboard
2. Position PowerShell and device side-by-side

**Steps**:
1. Run: `.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond -EstimatedPrice 120.00`
2. Wait for banner to appear (up to 30 seconds)
3. Observe banner
4. Wait for auto-dismiss

**Expected Results**:
- ? Banner appears within 30 seconds
- ? Gold background
- ? ? icon on left
- ? Message: "Quote for [PassengerName] has been updated"
- ? ? dismiss button on right
- ? Tapping ? immediately dismisses
- ? Auto-dismisses after 5 seconds if not tapped

---

### Scenario 10: Error Handling

**Objective**: Verify graceful error handling

**10a. Network Error (Airplane Mode)**

**Steps**:
1. Enable airplane mode on device
2. Open Quote Dashboard
3. Pull-to-refresh

**Expected Results**:
- ? Error dialog appears
- ? Message: "Couldn't load quotes: [network error]"
- ? Tapping OK dismisses dialog
- ? App doesn't crash

**10b. Cannot Accept Pending Quote**

**Steps**:
1. Using backend, attempt to accept Pending quote
2. Observe error

**Expected Results**:
- ? Error message: "Cannot accept quote with status 'Pending'"
- ? Accept button not visible for Pending quotes in app

**10c. Cannot Cancel Accepted Quote**

**Steps**:
1. Try to cancel an Accepted quote
2. Observe error

**Expected Results**:
- ? Error message: "Cannot cancel quote with status 'Accepted'"
- ? Cancel button not visible for Accepted quotes in app

---

## ?? Test Coverage

### Current Coverage

| Component | Unit Tests | Integration Tests | Manual Tests | Total Coverage |
|-----------|------------|-------------------|--------------|----------------|
| AdminApi | ? 85% | ? 90% | ? 100% | 90% |
| ConfigurationService | ? 92% | ? 80% | ? 100% | 88% |
| PlacesService | ? 75% | ? 85% | ? 100% | 82% |
| Pages | ??  40% | ??  50% | ? 100% | 65% |
| Models | ? 100% | N/A | N/A | 100% |

**Overall Coverage**: ~82% (Good)

**Improvement Areas**:
- Add more unit tests for Pages (view models planned)
- Add integration tests for location services
- Add automated UI tests (Appium planned for v1.1)

---

## ?? Bug Reporting

### How to Report a Bug

**Before Reporting**:
1. Check `32-Troubleshooting.md` for known issues
2. Search existing GitHub issues
3. Verify bug is reproducible

**Bug Report Template**:

```markdown
## Bug Description
[Clear description of the bug]

## Steps to Reproduce
1. [First step]
2. [Second step]
3. [Third step]

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Environment
- Device: [e.g., iPhone 14, Pixel 7]
- OS: [e.g., iOS 17.2, Android 14]
- App Version: [e.g., 1.0.0]
- .NET Version: [e.g., 9.0.1]

## Screenshots/Logs
[Add screenshots or error logs]

## Additional Context
[Any other relevant information]
```

---

## ?? CI/CD Testing

### GitHub Actions Workflow

**File**: `.github/workflows/mobile-tests.yml`

**Triggers**:
- Push to `main` branch
- Pull request to `main` branch
- Manual workflow dispatch

**Jobs**:
1. **Build** - Compile app for all platforms
2. **Unit Tests** - Run all unit tests
3. **Integration Tests** - Run integration tests (with mock services)
4. **Code Coverage** - Generate coverage report
5. **Upload Artifacts** - Store test results

**Status Badge**:
![Tests](https://github.com/BidumanADT/BellwoodApp/actions/workflows/mobile-tests.yml/badge.svg)

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[10-Google-Places-Autocomplete.md](10-Google-Places-Autocomplete.md)** - Autocomplete testing
- **[11-Location-Tracking.md](11-Location-Tracking.md)** - Tracking testing
- **[12-Quote-Lifecycle.md](12-Quote-Lifecycle.md)** - Quote management testing
- **[31-Scripts-Reference.md](31-Scripts-Reference.md)** - Automated test scripts
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common issues & solutions

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
