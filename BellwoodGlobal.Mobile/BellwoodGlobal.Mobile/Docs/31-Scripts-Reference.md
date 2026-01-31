# Scripts Reference

**Document Type**: Living Document - Scripts & Automation  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document provides complete reference for all PowerShell testing and automation scripts in the `Scripts/` folder. These scripts help automate environment setup, data seeding, and testing workflows for the Bellwood Global Mobile App.

**Script Categories**:
- ?? **Environment Scripts** - Service health checks
- ?? **Data Seeding** - Create test data
- ?? **Testing Utilities** - Status changes, debugging
- ?? **Multi-User Setup** - Create test users

**Prerequisites**:
- PowerShell 5.1+ (Windows) or PowerShell 7+ (cross-platform)
- AdminAPI running on `https://localhost:5206`
- AuthServer running on `https://localhost:5001`

---

## ?? Environment Scripts

### Test-Environment.ps1

**Purpose**: Verify all required services are running

**Usage**:
```powershell
.\Test-Environment.ps1
```

**What It Checks**:
- ? AdminAPI health (`https://localhost:5206/health`)
- ? AuthServer health (`https://localhost:5001/.well-known/openid-configuration`)
- ? Test data files exist

**Output Example**:
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

**Troubleshooting**:
```powershell
# If AdminAPI is down
cd ../AdminAPI
dotnet run

# If AuthServer is down
# Ensure AuthServer is running on port 5001
```

---

## ?? Data Seeding Scripts

### Seed-TestQuotes.ps1

**Purpose**: Create test quotes for mobile app testing

**Usage**:
```powershell
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
```

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `UserEmail` | string | Yes | - | Email of test user |
| `UserFirstName` | string | No | "Test" | User's first name |
| `UserLastName` | string | No | "User" | User's last name |

**What It Creates**:
- 5 test quotes in **Pending** status
- Different passengers, vehicles, locations
- Various pickup times (next 7 days)

**Output Example**:
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

**Created Quotes**:
1. **John Doe** - Sedan, O'Hare ? Downtown Chicago
2. **Jane Smith** - SUV, Midway ? Oak Park
3. **Bob Johnson** - Executive Sedan, Union Station ? Rosemont
4. **Alice Williams** - Luxury SUV, Downtown ? O'Hare
5. **Charlie Brown** - Sedan, O'Hare ? Evanston

---

### Setup-MultiUserTest.ps1

**Purpose**: Create multiple test users for isolation testing

**Usage**:
```powershell
.\Setup-MultiUserTest.ps1 -UserCount 3
```

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `UserCount` | int | No | 3 | Number of users to create |

**What It Does**:
1. Creates test user accounts (TestUser1, TestUser2, TestUser3)
2. Exports credentials to `TestUsers.csv`
3. Provides commands to seed quotes for each user

**Output Example**:
```
======================================
Multi-User Test Setup
======================================

Creating 3 test users...

TestUser1 (testuser1@example.com) - Password: Test123!
TestUser2 (testuser2@example.com) - Password: Test123!
TestUser3 (testuser3@example.com) - Password: Test123!

Credentials saved to: TestUsers.csv

======================================
Next Steps
======================================

To seed quotes for each user, run:

.\Seed-TestQuotes.ps1 -UserEmail "testuser1@example.com" -UserFirstName "Test" -UserLastName "User1"
.\Seed-TestQuotes.ps1 -UserEmail "testuser2@example.com" -UserFirstName "Test" -UserLastName "User2"
.\Seed-TestQuotes.ps1 -UserEmail "testuser3@example.com" -UserFirstName "Test" -UserLastName "User3"
```

**TestUsers.csv Format**:
```csv
Email,Password,FirstName,LastName
testuser1@example.com,Test123!,Test,User1
testuser2@example.com,Test123!,Test,User2
testuser3@example.com,Test123!,Test,User3
```

---

## ?? Testing Utility Scripts

### Simulate-StatusChange.ps1

**Purpose**: Change quote status for testing lifecycle

**Usage**:
```powershell
# Acknowledge a quote
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge

# Respond with price
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 85.50

# Cancel a quote
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Cancel

# With auto-wait for polling (30 seconds)
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 95.50 -WaitForPolling
```

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `QuoteId` | string | Yes | Quote ID to update |
| `Action` | string | Yes | "Acknowledge", "Respond", or "Cancel" |
| `EstimatedPrice` | decimal | If Respond | Estimated price (e.g., 85.50) |
| `EstimatedPickupTime` | DateTime | No | Estimated pickup time (optional) |
| `Notes` | string | No | Dispatcher notes |
| `WaitForPolling` | switch | No | Wait 30s for app polling |

**Actions**:

**1. Acknowledge** (Pending ? Acknowledged):
```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge
```

**Output**:
```
======================================
Quote Status Change Simulator
======================================

Acknowledging quote quote-abc-123... ? Done

Status changed: Pending ? Acknowledged
Display in app: 'Awaiting Response' ? 'Under Review' (Blue)
```

---

**2. Respond** (Acknowledged ? Responded):
```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 95.50 -Notes "Standard route pricing"
```

**Output**:
```
Responding to quote quote-abc-123 with price... ? Done

Status changed: Acknowledged ? Responded
Estimated Price: $95.50
Notes: Standard route pricing
Display in app: 'Under Review' ? 'Response Received - $95.50' (Green)
```

---

**3. Cancel** (Any ? Cancelled):
```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Cancel
```

**Output**:
```
Cancelling quote quote-abc-123... ? Done

Status changed: [Current] ? Cancelled
Display in app: 'Cancelled' (Red)
```

---

**4. With Polling Wait**:
```powershell
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 85.50 -WaitForPolling
```

**Output**:
```
Responding to quote quote-abc-123 with price... ? Done

Status changed: Acknowledged ? Responded
Estimated Price: $85.50

Waiting 30 seconds for app polling to detect change...
30... 29... 28... ... 3... 2... 1...

? App should now show updated status!
```

---

### Get-QuoteInfo.ps1

**Purpose**: View quote details

**Usage**:
```powershell
# List all quotes
.\Get-QuoteInfo.ps1

# View specific quote
.\Get-QuoteInfo.ps1 -QuoteId "quote-abc-123"
```

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `QuoteId` | string | No | Specific quote to view (omit to list all) |

**Output (List All)**:
```
Found 5 quotes:

quote-abc-123 ? Pending      ? John Doe             ? Sedan
quote-def-456 ? Acknowledged ? Jane Smith           ? SUV
quote-ghi-789 ? Responded    ? Bob Johnson          ? Executive Sedan ($95.50)
quote-jkl-012 ? Pending      ? Alice Williams       ? Luxury SUV
quote-mno-345 ? Cancelled    ? Charlie Brown        ? Sedan
```

**Output (Specific Quote)**:
```
======================================
Quote Details: quote-abc-123
======================================

Status:           Pending
Passenger:        John Doe
Vehicle:          Sedan
Pickup:           O'Hare Airport Terminal 1
Dropoff:          Downtown Chicago, 100 N LaSalle St
Pickup Time:      2026-02-15 14:30:00 UTC
Created:          2026-01-27 10:30:00 UTC

Lifecycle:
  Created By:     user-abc-123
  Acknowledged:   (not yet)
  Responded:      (not yet)

Estimated Price:  (not yet responded)
```

---

## ?? Common Workflows

### Workflow 1: Fresh Test Environment

**Goal**: Start with clean test data

```powershell
# 1. Verify services running
.\Test-Environment.ps1

# 2. Seed test quotes
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"

# 3. View created quotes
.\Get-QuoteInfo.ps1
```

---

### Workflow 2: Test Quote Lifecycle

**Goal**: Test complete quote flow

```powershell
# 1. Get quote IDs
.\Get-QuoteInfo.ps1

# 2. Acknowledge quote
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge

# 3. Wait 30 seconds, verify app shows "Under Review"

# 4. Respond with price
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 85.50 -WaitForPolling

# 5. Verify app shows "Response Received - $85.50"

# 6. Accept quote in mobile app

# 7. Verify booking created
```

---

### Workflow 3: Test Automatic Polling

**Goal**: Verify 30-second polling updates UI

```powershell
# 1. Open mobile app to Quote Dashboard

# 2. Position PowerShell and phone side-by-side

# 3. Change status with auto-wait
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge -WaitForPolling

# 4. Watch mobile app auto-refresh within 30 seconds

# 5. Verify gold notification banner appears
```

---

### Workflow 4: Multi-User Isolation Testing

**Goal**: Verify users only see their own quotes

```powershell
# 1. Create test users
.\Setup-MultiUserTest.ps1 -UserCount 2

# 2. Seed quotes for User1
.\Seed-TestQuotes.ps1 -UserEmail "testuser1@example.com"

# 3. Seed quotes for User2
.\Seed-TestQuotes.ps1 -UserEmail "testuser2@example.com"

# 4. Login as User1 in mobile app
#    ? Verify only User1's quotes appear

# 5. Login as User2 in mobile app
#    ? Verify only User2's quotes appear
```

---

## ?? Troubleshooting Scripts

### Common Issues

**Issue: "Cannot connect to AdminAPI"**

**Check**:
```powershell
.\Test-Environment.ps1
```

**Fix**:
```powershell
# Start AdminAPI
cd ../AdminAPI
dotnet run
```

---

**Issue: "SSL certificate validation failed"**

**PowerShell 5.1 Workaround**:
```powershell
# Add before running scripts
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
```

**PowerShell 7+ Solution**:
```powershell
# Use -SkipCertificateCheck parameter (built-in)
```

---

**Issue: "Quote not found"**

**Check**:
```powershell
# List all quotes
.\Get-QuoteInfo.ps1

# Copy correct Quote ID from output
```

---

**Issue: "Estimated price required for Respond action"**

**Fix**:
```powershell
# Include -EstimatedPrice parameter
.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond -EstimatedPrice 95.50
```

---

## ?? Script Output Reference

### Status Change Flow

```
Pending (Orange)
    ? Acknowledge
Acknowledged (Blue)
    ? Respond with Price
Responded (Green) ??????????
    ? Accept             ? Cancel
Accepted (Gray)      Cancelled (Red)
```

---

### Expected Quote Statuses

| Status | Badge Color | Display Text |
|--------|-------------|--------------|
| **Pending** | ?? Orange | "Awaiting Response" |
| **Acknowledged** | ?? Blue | "Under Review" |
| **Responded** | ?? Green | "Response Received - $XX.XX" |
| **Accepted** | ? Gray | "Booking Created" |
| **Cancelled** | ?? Red | "Cancelled" |

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[02-Testing-Guide.md](02-Testing-Guide.md)** - Testing strategies & scenarios
- **[12-Quote-Lifecycle.md](12-Quote-Lifecycle.md)** - Quote status flow
- **[20-API-Integration.md](20-API-Integration.md)** - API endpoints used by scripts
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common issues & solutions

---

## ?? Tips & Best Practices

### Script Best Practices

**1. Always verify environment first**:
```powershell
.\Test-Environment.ps1
```

**2. Use descriptive quote IDs**:
```powershell
# Copy exact ID from Get-QuoteInfo output
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Respond -EstimatedPrice 85.50
```

**3. Test polling with -WaitForPolling**:
```powershell
# Watch app auto-refresh
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge -WaitForPolling
```

**4. Create fresh data for each test cycle**:
```powershell
# Delete old test data, seed new
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
```

---

### Testing Tips

**For Developers**:
- Run `Test-Environment.ps1` before starting development
- Use `Seed-TestQuotes.ps1` to populate quote dashboard quickly
- Use `Simulate-StatusChange.ps1` to test UI updates without admin portal

**For QA**:
- Use `Setup-MultiUserTest.ps1` for isolation testing
- Use `-WaitForPolling` to verify automatic refresh
- Document test scenarios using script commands

**For Demos**:
- Pre-seed quotes in various statuses
- Use `Simulate-StatusChange.ps1` to show real-time updates
- Position PowerShell and mobile device side-by-side

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
