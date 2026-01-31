# ?? Phase Alpha Quote Lifecycle - Testing Walkthrough Guide

**Feature**: Passenger App Quote Lifecycle Integration  
**Branch**: `feature/passenger-quote-tracking`  
**Version**: 1.0  
**Last Updated**: January 2026

---

## ?? Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start Guide](#quick-start-guide)
3. [Automated Test Scripts](#automated-test-scripts)
4. [Manual Testing Scenarios](#manual-testing-scenarios)
5. [Troubleshooting](#troubleshooting)
6. [Test Data Reference](#test-data-reference)

---

## Prerequisites

### Software Requirements
- ? PowerShell 5.1 or higher
- ? .NET 8 or .NET 9 SDK
- ? Visual Studio 2022 or VS Code
- ? Android Emulator or iOS Simulator (for mobile app testing)

### Services Required
- ? AdminAPI running on `https://localhost:5206`
- ? Auth Server running on `https://localhost:5001`
- ? Mobile app deployed to test device/emulator

### Test User Account
- Create a test user account with "booker" role
- Example credentials:
  - Email: `testuser@example.com`
  - Password: `Test123!`

---

## Quick Start Guide

### Step 1: Environment Check

Open PowerShell and navigate to the Scripts folder:

```powershell
cd BellwoodGlobal.Mobile\Scripts
```

Run the environment health check:

```powershell
.\Test-Environment.ps1
```

**Expected Output:**
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
Quote Data Files  ? Found (7 files)   ...

? All services are healthy and ready for testing!
```

**If services are down:**
1. Start AdminAPI: `cd AdminAPI && dotnet run`
2. Start Auth Server: Ensure it's running on port 5001
3. Re-run `.\Test-Environment.ps1`

---

### Step 2: Seed Test Data

Create test quotes with various statuses:

```powershell
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
```

**Expected Output:**
```
======================================
Phase Alpha Test Data Seeder
======================================

This script will create 5 test quotes for user: testuser@example.com

Creating test quotes...

Creating quote: John Doe - Sedan... ? Created (ID: quote-abc-123)
Creating quote: Jane Smith - SUV... ? Created (ID: quote-def-456)
Creating quote: Bob Johnson - Executive Sedan... ? Created (ID: quote-ghi-789)
Creating quote: Alice Williams - Luxury SUV... ? Created (ID: quote-jkl-012)
Creating quote: Charlie Brown - Sedan... ? Created (ID: quote-mno-345)

======================================
Summary
======================================
Created:  5 quotes
Failed:   0 quotes
```

**Note:** All quotes are created in **Pending** status. You'll transition them in the next steps.

---

### Step 3: View Created Quotes

List all quotes to get their IDs:

```powershell
.\Get-QuoteInfo.ps1
```

**Expected Output:**
```
Found 5 quotes:

quote-abc-123 ? Pending      ? John Doe             ? Sedan
quote-def-456 ? Pending      ? Jane Smith           ? SUV
quote-ghi-789 ? Pending      ? Bob Johnson          ? Executive Sedan
quote-jkl-012 ? Pending      ? Alice Williams       ? Luxury SUV
quote-mno-345 ? Pending      ? Charlie Brown        ? Sedan
```

Copy these Quote IDs for use in testing!

---

### Step 4: Simulate Quote Status Changes

Now let's create quotes in different statuses for comprehensive testing.

#### 4a. Create an Acknowledged Quote

```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-def-456" -Action Acknowledge
```

**Expected Output:**
```
======================================
Quote Status Change Simulator
======================================

Acknowledging quote quote-def-456... ? Done

Status changed: Pending ? Acknowledged
Display in app: 'Awaiting Response' ? 'Under Review' (Blue)
```

---

#### 4b. Create a Responded Quote (with Price)

First, acknowledge it:
```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-ghi-789" -Action Acknowledge
```

Then respond with a price:
```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-ghi-789" -Action Respond -EstimatedPrice 95.50
```

**Expected Output:**
```
Responding to quote quote-ghi-789 with price... ? Done

Status changed: Acknowledged ? Responded
Estimated Price: $95.50
Display in app: 'Under Review' ? 'Response Received - $95.50' (Green)
```

---

#### 4c. Create a Cancelled Quote

```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-mno-345" -Action Cancel
```

**Expected Output:**
```
Cancelling quote quote-mno-345... ? Done

Status changed: [Current] ? Cancelled
Display in app: 'Cancelled' (Red)
```

---

### Step 5: Your Test Data is Ready!

You should now have quotes in the following statuses:

| Quote ID | Passenger | Status | Display in App |
|----------|-----------|--------|----------------|
| quote-abc-123 | John Doe | **Pending** | Awaiting Response (Orange) |
| quote-def-456 | Jane Smith | **Acknowledged** | Under Review (Blue) |
| quote-ghi-789 | Bob Johnson | **Responded** | Response Received - $95.50 (Green) |
| quote-jkl-012 | Alice Williams | **Pending** | Awaiting Response (Orange) |
| quote-mno-345 | Charlie Brown | **Cancelled** | Cancelled (Red) |

---

## Automated Test Scripts

### Available Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `Test-Environment.ps1` | Check if all services are running | `.\Test-Environment.ps1` |
| `Seed-TestQuotes.ps1` | Create test quotes | `.\Seed-TestQuotes.ps1 -UserEmail "user@example.com"` |
| `Simulate-StatusChange.ps1` | Change quote status | `.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond` |
| `Get-QuoteInfo.ps1` | View quote details | `.\Get-QuoteInfo.ps1 -QuoteId "quote-123"` |
| `Setup-MultiUserTest.ps1` | Create multi-user test data | `.\Setup-MultiUserTest.ps1 -UserCount 3` |

---

### Script Examples

#### View Specific Quote Details

```powershell
.\Get-QuoteInfo.ps1 -QuoteId "quote-abc-123"
```

Output shows full quote details including status, timestamps, price (if responded), etc.

---

#### Test Polling with Auto-Wait

This simulates a status change and waits 30 seconds for the app to poll:

```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge -WaitForPolling
```

The script will count down 30 seconds, giving you time to watch the app auto-refresh.

---

#### Create Multiple Users for Isolation Testing

```powershell
.\Setup-MultiUserTest.ps1 -UserCount 3
```

This creates:
- TestUser1, TestUser2, TestUser3
- Exports credentials to `TestUsers.csv`
- Provides commands to seed quotes for each user

---

## Manual Testing Scenarios

### Scenario 1: View Quote Dashboard

**Steps:**
1. Launch mobile app
2. Login with `testuser@example.com`
3. Navigate to **Quote Dashboard**
4. Verify all 5 quotes appear

**Expected Results:**
- ? All quotes display in list
- ? Status badges show correct colors:
  - Orange = Awaiting Response
  - Blue = Under Review
  - Green = Response Received
  - Red = Cancelled
- ? Responded quote shows price in status badge
- ? Each quote shows passenger, vehicle, pickup info

---

### Scenario 2: Test Automatic Polling

**Steps:**
1. Open Quote Dashboard in app
2. Keep app on that screen
3. In PowerShell, run:
   ```powershell
   .\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge
   ```
4. Wait up to 30 seconds (don't touch the app)

**Expected Results:**
- ? Quote automatically updates from orange to blue
- ? Gold notification banner appears at top
- ? Banner says "Quote for John Doe has been updated"
- ? Banner auto-dismisses after 5 seconds

---

### Scenario 3: View Quote Detail (Responded Status)

**Steps:**
1. Open Quote Dashboard
2. Tap the "Response Received" quote (Bob Johnson)
3. Scroll through the detail page

**Expected Results:**
- ? Status chip shows "Response Received" (Green)
- ? **Dispatcher Response** section is visible
- ? Shows estimated price: $95.50
- ? Shows price disclaimer
- ? Shows estimated pickup time
- ? Shows dispatcher notes
- ? **Both** buttons visible:
  - "? Accept Quote & Create Booking" (gold, large)
  - "? Cancel Quote Request" (charcoal, smaller)

---

### Scenario 4: Accept Quote

**Steps:**
1. Open the Responded quote (Bob Johnson - $95.50)
2. Tap "? Accept Quote & Create Booking"
3. Wait for API call

**Expected Results:**
- ? Button disables (prevents double-tap)
- ? Success dialog appears
- ? Message: "Quote accepted! Your booking has been created."
- ? Two buttons: "View Booking" and "OK"
- ? Tap "View Booking" ? navigates to BookingDetailPage
- ? Booking shows status "Requested"
- ? Booking contains same pickup/dropoff as quote
- ? Return to dashboard ? quote now shows "Booking Created" (Gray)

---

### Scenario 5: Cancel Quote

**Steps:**
1. Open a Pending quote (John Doe)
2. Tap "? Cancel Quote Request"
3. Confirmation dialog appears

**Expected Results:**
- ? Dialog title: "Cancel Quote?"
- ? Message: "Are you sure you want to cancel this quote request? This action cannot be undone."
- ? Buttons: "Yes, Cancel" and "No"
- ? Tap "Yes, Cancel" ? button disables
- ? Success message: "Your quote request has been cancelled."
- ? Navigates back to dashboard
- ? Quote now shows "Cancelled" (Red)
- ? Open cancelled quote ? no action buttons visible

---

### Scenario 6: Test Notification Banner Manually

**Setup:**
1. Open Quote Dashboard in app
2. Position PowerShell and phone side-by-side

**Steps:**
1. Run: `.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 120.00`
2. Watch the app for 30 seconds
3. When banner appears, verify content
4. Watch it auto-dismiss after 5 seconds

**Expected Results:**
- ? Banner appears within 30 seconds (polling cycle)
- ? Gold background
- ? ? icon on left
- ? Message: "Quote for John Doe has been updated"
- ? ? dismiss button on right
- ? Tapping ? immediately dismisses
- ? Auto-dismisses after 5 seconds if not tapped

---

### Scenario 7: Test Multiple Status Changes

**Steps:**
1. Open Dashboard
2. In PowerShell, rapidly change 3 quotes:
   ```powershell
   .\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge
   .\Simulate-StatusChange.ps1 -QuoteId "quote-def-456" -Action Respond -EstimatedPrice 75.00
   .\Simulate-StatusChange.ps1 -QuoteId "quote-jkl-012" -Action Cancel
   ```
3. Wait 30 seconds

**Expected Results:**
- ? Banner shows: "3 quotes have been updated"
- ? All 3 quotes update in list
- ? Each shows correct new status and color

---

### Scenario 8: Test Filter Buttons

**Steps:**
1. Open Dashboard with all quotes visible
2. Tap "Awaiting Response" filter
3. Tap "Response Received" filter
4. Tap "Cancelled" filter
5. Tap "All" filter

**Expected Results:**
- ? "Awaiting Response": Only shows Pending/Acknowledged quotes
- ? "Response Received": Only shows Responded quotes (with prices)
- ? "Cancelled": Only shows Cancelled quotes
- ? "All": Shows all quotes again
- ? Active filter button highlighted in gold

---

### Scenario 9: Test Search

**Steps:**
1. In search box, type "John"
2. Clear, type "SUV"
3. Clear, type "O'Hare"
4. Clear search

**Expected Results:**
- ? "John" ? Shows only John Doe quote
- ? "SUV" ? Shows only SUV quotes
- ? "O'Hare" ? Shows quotes with O'Hare pickup
- ? Clear ? All quotes return

---

### Scenario 10: Test Round-Trip Quote (Optional)

**Requires:** A round-trip quote created in backend

**Steps:**
1. Create round-trip quote manually or via script
2. Open quote in app

**Expected Results:**
- ? **Return** section visible below main trip
- ? Shows return pickup time & location
- ? Shows return pickup style
- ? Shows return dropoff

---

## Troubleshooting

### Issue: No quotes appear in dashboard

**Possible Causes:**
- User not logged in
- Wrong user logged in
- No quotes created for this user
- API not running

**Solutions:**
1. Verify login email matches seed email
2. Run `.\Get-QuoteInfo.ps1` to see all quotes
3. Check AdminAPI is running: `.\Test-Environment.ps1`
4. Re-seed data: `.\Seed-TestQuotes.ps1 -UserEmail "your@email.com"`

---

### Issue: Polling doesn't update quotes

**Possible Causes:**
- Not on Quote Dashboard page
- App in background
- Network issue

**Solutions:**
1. Ensure Quote Dashboard is active page
2. Keep app in foreground
3. Check network connectivity
4. Look for debug logs (if enabled)
5. Manually pull-to-refresh as workaround

---

### Issue: "Cannot accept quote" error

**Possible Causes:**
- Quote not in "Responded" status
- Quote already accepted
- Network error

**Solutions:**
1. Check quote status: `.\Get-QuoteInfo.ps1 -QuoteId "quote-123"`
2. Ensure quote has `respondedAt` timestamp
3. Verify estimated price exists
4. Try pulling to refresh in app

---

### Issue: PowerShell script fails with SSL error

**Solution for PowerShell 5.1:**
```powershell
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
```

Or upgrade to PowerShell 7+ which has `-SkipCertificateCheck` parameter.

---

### Issue: Script says "AdminAPI not running"

**Solutions:**
1. Navigate to AdminAPI project folder
2. Run: `dotnet run`
3. Verify it starts on port 5206
4. Re-run `.\Test-Environment.ps1`

---

## Test Data Reference

### Quote Status Flow

```
Pending (Orange)
    ? Acknowledge
Acknowledged (Blue)
    ? Respond with Price
Responded (Green) ??????????
    ? Accept    ? Cancel   ? Cancel
Accepted (Gray) Cancelled (Red)
```

### Expected Status Badge Colors

| Status | Display Text | Color | Hex |
|--------|--------------|-------|-----|
| Pending | Awaiting Response | Orange | #FFA500 |
| Acknowledged | Under Review | Blue | #0000FF |
| Responded | Response Received | Green | #008000 |
| Accepted | Booking Created | Gray | #808080 |
| Cancelled | Cancelled | Red | #FF0000 |

### Test Quote Templates

Use these for manual testing scenarios:

**Airport Pickup:**
- Pickup: O'Hare Airport Terminal 1
- Dropoff: Downtown Chicago
- Vehicle: Sedan
- Passengers: 2

**Meet & Greet:**
- Pickup: Midway Airport
- Pickup Style: Meet & Greet
- Sign Text: "Smith Family"
- Vehicle: SUV

**Executive Service:**
- Pickup: Union Station
- Dropoff: Rosemont Convention Center
- Vehicle: Executive Sedan
- Notes: Corporate travel

---

## Testing Completion Checklist

### Phase 1: Setup (? = Done)
- [ ] Environment health check passed
- [ ] Test quotes seeded (5+ quotes)
- [ ] Quotes in various statuses created
- [ ] Mobile app deployed to test device

### Phase 2: Core Functionality
- [ ] View Quote Dashboard
- [ ] Filter by status
- [ ] Search quotes
- [ ] View quote details (all statuses)
- [ ] Accept quote successfully
- [ ] Cancel quote successfully

### Phase 3: Advanced Features
- [ ] Automatic polling (30s)
- [ ] Notification banner appears
- [ ] Notification auto-dismisses
- [ ] Manual dismiss works
- [ ] Multiple status changes detected
- [ ] Navigate to booking from accepted quote

### Phase 4: Edge Cases
- [ ] Error handling (network errors)
- [ ] Cannot accept Pending quote
- [ ] Cannot cancel Accepted quote
- [ ] Pull-to-refresh works
- [ ] Empty state (no quotes)

### Phase 5: Multi-User (Optional)
- [ ] User A sees only their quotes
- [ ] User B sees only their quotes
- [ ] Cannot access other user's quotes
- [ ] Quote ownership enforced

---

## Quick Command Reference

### Daily Testing Workflow

```powershell
# 1. Check environment
.\Test-Environment.ps1

# 2. List all quotes
.\Get-QuoteInfo.ps1

# 3. Change a quote status (for polling test)
.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond -EstimatedPrice 85.00 -WaitForPolling

# 4. View updated quote
.\Get-QuoteInfo.ps1 -QuoteId "quote-123"

# 5. Clean slate - seed fresh data
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
```

---

## Support & Documentation

- **Full Testing Checklist**: See `Testing-Checklist.md` for exhaustive manual test cases
- **API Documentation**: See AdminAPI project docs
- **Script Help**: Run any script with `-?` for parameter help
  ```powershell
  .\Simulate-StatusChange.ps1 -?
  ```

---

**Happy Testing!** ??

If you encounter issues not covered here, check the AdminAPI logs and mobile app debug output for additional insights.

---

**Document Version**: 1.0  
**Last Updated**: January 2026  
**Maintained By**: Bellwood Global Mobile Team
