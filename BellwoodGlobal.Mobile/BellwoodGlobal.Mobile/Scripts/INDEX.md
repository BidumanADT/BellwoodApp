# ?? Phase Alpha Testing - File Index

**Quick Navigation**: Find what you need instantly!

---

## ?? START HERE

### For Testers (First Time)
1. **[PACKAGE-SUMMARY.md](PACKAGE-SUMMARY.md)** - Overview of what's included
2. **[README.md](README.md)** - Quick reference for Scripts folder
3. **[Testing-Guide.md](Testing-Guide.md)** - Step-by-step walkthrough ? **MOST IMPORTANT**

### For Developers
1. **[Testing-Guide.md](Testing-Guide.md)** - Technical setup and scripting
2. **[Testing-Checklist.md](Testing-Checklist.md)** - Exhaustive test case list

---

## ?? PowerShell Scripts

### Core Testing Scripts
| File | Use When... | Command |
|------|-------------|---------|
| **Test-Environment.ps1** | You want to verify services are running | `.\Test-Environment.ps1` |
| **Seed-TestQuotes.ps1** | You need to create test data | `.\Seed-TestQuotes.ps1 -UserEmail "user@example.com"` |
| **Get-QuoteInfo.ps1** | You want to see quote details | `.\Get-QuoteInfo.ps1 -QuoteId "quote-123"` |

### Advanced Scripts
| File | Use When... | Command |
|------|-------------|---------|
| **Simulate-StatusChange.ps1** | Testing polling and notifications | `.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond` |
| **Setup-MultiUserTest.ps1** | Testing multi-user isolation | `.\Setup-MultiUserTest.ps1 -UserCount 3` |

---

## ?? Documentation Files

### Primary Documents
| File | Purpose | When to Read |
|------|---------|--------------|
| **[Testing-Guide.md](Testing-Guide.md)** | Complete walkthrough with examples | **Start here** for step-by-step guidance |
| **[Testing-Checklist.md](Testing-Checklist.md)** | All 150+ test cases | When doing comprehensive QA testing |
| **[PACKAGE-SUMMARY.md](PACKAGE-SUMMARY.md)** | Feature overview and status | For project managers and stakeholders |

### Reference Documents
| File | Purpose |
|------|---------|
| **[README.md](README.md)** | Quick reference for Scripts folder |
| **THIS FILE (INDEX.md)** | Navigation and file index |

---

## ?? Learning Path

### Path 1: "I want to test the app quickly"
1. Read **PACKAGE-SUMMARY.md** (5 min)
2. Run **Test-Environment.ps1** (1 min)
3. Run **Seed-TestQuotes.ps1** (2 min)
4. Open app and test manually (30 min)

### Path 2: "I want comprehensive testing"
1. Read **Testing-Guide.md** (20 min)
2. Follow all "Quick Start" steps (10 min)
3. Work through **Testing-Checklist.md** (2-3 hours)
4. Document findings

### Path 3: "I want to understand the automation"
1. Read **README.md** (5 min)
2. Read **Testing-Guide.md** ? "Automated Test Scripts" (10 min)
3. Review each `.ps1` script source code (30 min)
4. Customize scripts for your needs

---

## ?? Find What You Need

### I want to...

**...verify my environment is ready**
? Run `.\Test-Environment.ps1`

**...create test data**
? Run `.\Seed-TestQuotes.ps1 -UserEmail "your@email.com"`

**...see all quotes**
? Run `.\Get-QuoteInfo.ps1`

**...see one quote's details**
? Run `.\Get-QuoteInfo.ps1 -QuoteId "quote-123"`

**...test polling (status updates)**
? Run `.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Acknowledge -WaitForPolling`

**...test notifications**
? Read **Testing-Guide.md** ? "Scenario 6: Test Notification Banner"

**...test accept quote**
? Read **Testing-Guide.md** ? "Scenario 4: Accept Quote"

**...test cancel quote**
? Read **Testing-Guide.md** ? "Scenario 5: Cancel Quote"

**...test multi-user isolation**
? Run `.\Setup-MultiUserTest.ps1` then read **Testing-Guide.md** ? "Multi-User Testing"

**...get script help**
? Run `Get-Help .\[ScriptName].ps1 -Full`

**...troubleshoot issues**
? Read **Testing-Guide.md** ? "Troubleshooting" section

**...see all test cases**
? Open **Testing-Checklist.md**

**...understand what's new**
? Read **PACKAGE-SUMMARY.md** ? "Feature Summary"

---

## ?? File Statistics

- **PowerShell Scripts**: 5 files (7 total including legacy)
- **Documentation**: 5 markdown files
- **Total Lines**: ~3,500+ lines of documentation and code
- **Test Cases Documented**: 150+ individual steps across 20 scenarios
- **Automation Coverage**: ~30% scriptable, ~70% manual

---

## ?? Recommended Reading Order

### For First-Time Setup (30 minutes)
1. **PACKAGE-SUMMARY.md** (5 min) - Understand what you're testing
2. **README.md** (3 min) - Quick script reference
3. **Testing-Guide.md** ? "Quick Start" (10 min) - Set up test data
4. **Testing-Guide.md** ? "Scenario 1-5" (12 min) - Core scenarios

### For Comprehensive Testing (3 hours)
1. Complete "First-Time Setup" above
2. **Testing-Guide.md** ? All scenarios (1 hour)
3. **Testing-Checklist.md** ? Work through all 20 scenarios (2 hours)

### For Automation Engineers (1 hour)
1. **README.md** (5 min)
2. Review all 5 `.ps1` scripts (30 min)
3. **Testing-Guide.md** ? "Automated Test Scripts" (15 min)
4. Customize/extend scripts (10 min)

---

## ?? Common Questions

**Q: Where do I start?**  
A: Read **Testing-Guide.md** from top to bottom. It has everything.

**Q: How do I create test data?**  
A: Run `.\Seed-TestQuotes.ps1 -UserEmail "your@email.com"`

**Q: How do I test polling?**  
A: Open app on dashboard, then run `.\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond -WaitForPolling`

**Q: Which scripts can I run safely?**  
A: All of them! They only affect test data, not production.

**Q: What if a script fails?**  
A: Check **Testing-Guide.md** ? "Troubleshooting" section.

**Q: How long does testing take?**  
A: Quick test: 30 min. Comprehensive: 3 hours.

**Q: Do I need to understand PowerShell?**  
A: No! Just copy and paste the commands from the guides.

**Q: Can I run scripts on Mac/Linux?**  
A: Yes, if you have PowerShell Core (7+) installed.

---

## ?? Need Help?

1. Check **Testing-Guide.md** ? "Troubleshooting"
2. Review script help: `Get-Help .\[ScriptName].ps1 -Full`
3. Check AdminAPI and mobile app debug logs
4. Contact development team

---

## ? Quick Verification

Before you start testing, verify you have:

- [ ] PowerShell 5.1+ installed
- [ ] AdminAPI running on port 5206
- [ ] Auth Server running on port 5001
- [ ] Mobile app deployed to test device
- [ ] Test user account created
- [ ] Read **Testing-Guide.md** ? "Prerequisites"

If all checked, you're ready to go! ??

---

**Last Updated**: January 2026  
**Version**: 1.0  
**Maintained By**: Bellwood Global Mobile Team

**Happy Testing!** ??
