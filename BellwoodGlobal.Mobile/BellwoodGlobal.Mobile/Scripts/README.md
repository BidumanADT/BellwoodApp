# ?? Phase Alpha Test Automation Scripts

This folder contains PowerShell scripts for testing the **Phase Alpha Quote Lifecycle** feature in the Bellwood Global Passenger App.

---

## ?? Files in This Folder

| File | Purpose |
|------|---------|
| **Test-Environment.ps1** | Verifies AdminAPI and Auth Server are running |
| **Seed-TestQuotes.ps1** | Creates test quotes with various details |
| **Simulate-StatusChange.ps1** | Changes quote status (Acknowledge/Respond/Cancel) |
| **Get-QuoteInfo.ps1** | Views quote details from API |
| **Setup-MultiUserTest.ps1** | Creates multi-user test scenarios |
| **Testing-Guide.md** | ?? **START HERE** - Complete walkthrough guide |
| **Testing-Checklist.md** | Comprehensive manual testing checklist (150+ test steps) |

---

## ?? Quick Start

### 1. Check Your Environment

```powershell
.\Test-Environment.ps1
```

This verifies AdminAPI and Auth Server are running.

### 2. Create Test Data

```powershell
.\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
```

This creates 5 test quotes in Pending status.

### 3. Prepare Different Statuses

```powershell
# Create an Acknowledged quote
.\Simulate-StatusChange.ps1 -QuoteId "quote-abc-123" -Action Acknowledge

# Create a Responded quote with price
.\Simulate-StatusChange.ps1 -QuoteId "quote-def-456" -Action Acknowledge
.\Simulate-StatusChange.ps1 -QuoteId "quote-def-456" -Action Respond -EstimatedPrice 95.50

# Create a Cancelled quote
.\Simulate-StatusChange.ps1 -QuoteId "quote-ghi-789" -Action Cancel
```

### 4. View Your Test Data

```powershell
.\Get-QuoteInfo.ps1
```

Lists all quotes with their IDs and statuses.

---

## ?? Documentation

**For detailed testing instructions, see:**
- **[Testing-Guide.md](Testing-Guide.md)** - Step-by-step walkthrough with screenshots
- **[Testing-Checklist.md](Testing-Checklist.md)** - Exhaustive test case list

---

## ?? Common Usage Patterns

### Test Automatic Polling

```powershell
# Change a quote status and wait for app to detect it
.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond -EstimatedPrice 125.00 -WaitForPolling
```

The script waits 30 seconds while the app polls for updates.

### Check Specific Quote

```powershell
.\Get-QuoteInfo.ps1 -QuoteId "quote-abc-123"
```

Shows full details including status, timestamps, and price.

### Multi-User Testing

```powershell
# Create 3 test users
.\Setup-MultiUserTest.ps1 -UserCount 3

# Then seed quotes for each user
.\Seed-TestQuotes.ps1 -UserEmail "testuser1@example.com" -UserFirstName "Test1" -UserLastName "User"
.\Seed-TestQuotes.ps1 -UserEmail "testuser2@example.com" -UserFirstName "Test2" -UserLastName "User"
```

---

## ?? Requirements

- **PowerShell**: 5.1 or higher (PowerShell 7+ recommended)
- **AdminAPI**: Running on `https://localhost:5206`
- **Auth Server**: Running on `https://localhost:5001`
- **Mobile App**: Deployed to test device/emulator

---

## ?? Troubleshooting

### Script Fails with SSL Error

**PowerShell 5.1:**
```powershell
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
```

**PowerShell 7+:**
Scripts automatically use `-SkipCertificateCheck` parameter.

### "AdminAPI not running" Error

1. Navigate to AdminAPI project
2. Run: `dotnet run`
3. Verify it starts on port 5206
4. Re-run `.\Test-Environment.ps1`

### Get Script Help

```powershell
Get-Help .\Test-Environment.ps1 -Full
Get-Help .\Simulate-StatusChange.ps1 -Examples
```

---

## ?? What's Automated vs. Manual

### ? Automated (via Scripts)
- Environment health checks
- Test data creation
- Backend status transitions
- Quote information retrieval
- Multi-user setup

### ?? Manual (via App Testing)
- UI/UX verification
- Touch interactions
- Navigation flows
- Visual appearance
- Notification banner display
- Error dialog formatting

**Automation Coverage**: ~30% scriptable, ~70% manual UI testing

This is expected for mobile app testing where user experience is critical!

---

## ?? Testing Workflow

1. **Run Environment Check**
   ```powershell
   .\Test-Environment.ps1
   ```

2. **Seed Test Data**
   ```powershell
   .\Seed-TestQuotes.ps1
   ```

3. **Create Various Statuses** (using `Simulate-StatusChange.ps1`)

4. **Open Mobile App** and perform manual UI testing

5. **Trigger Changes During Testing** to test polling and notifications

---

## ?? Support

For issues or questions:
- Check **Testing-Guide.md** for detailed walkthroughs
- Review **Testing-Checklist.md** for specific test scenarios
- Check AdminAPI and mobile app debug logs

---

**Version**: 1.0  
**Last Updated**: January 2026  
**Feature**: Phase Alpha Quote Lifecycle Integration

**Happy Testing!** ??
