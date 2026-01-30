<#
.SYNOPSIS
    Phase Alpha - Multi-User Test Setup
.DESCRIPTION
    Creates multiple test users and their isolated quote data for testing
.NOTES
    Compatible with PowerShell 5.1+
    Author: Bellwood Global Mobile Team
    Version: 1.0
.EXAMPLE
    .\Setup-MultiUserTest.ps1
    Creates 3 test users (default)
.EXAMPLE
    .\Setup-MultiUserTest.ps1 -UserCount 5
    Creates 5 test users
#>

param(
    [int]$UserCount = 3,
    [string]$AdminApiUrl = "https://localhost:5206"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Multi-User Test Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$testUsers = @()

for ($i = 1; $i -le $UserCount; $i++) {
    $user = [PSCustomObject]@{
        UserId = "test-user-$($i.ToString('000'))"
        Username = "testuser$i"
        FirstName = "Test$i"
        LastName = "User"
        Email = "testuser$i@example.com"
        Password = "Test123!"
        QuoteCount = Get-Random -Minimum 2 -Maximum 5
    }
    
    $testUsers += $user
    
    Write-Host "User $i" -ForegroundColor Cyan
    Write-Host "  Username:  $($user.Username)" -ForegroundColor Gray
    Write-Host "  Email:     $($user.Email)" -ForegroundColor Gray
    Write-Host "  Password:  $($user.Password)" -ForegroundColor Gray
    Write-Host "  Name:      $($user.FirstName) $($user.LastName)" -ForegroundColor Gray
    Write-Host "  Quotes:    $($user.QuoteCount) quotes will be created" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Created $UserCount test user profiles" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Export to CSV for reference
$csvPath = "TestUsers.csv"
$testUsers | Export-Csv -Path $csvPath -NoTypeInformation
Write-Host "? User list exported to $csvPath" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps for Multi-User Testing:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Create User Accounts (Manual)" -ForegroundColor Yellow
Write-Host "   - Register each user in the Auth Server" -ForegroundColor Gray
Write-Host "   - Or use existing test accounts and update CSV" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Seed Quotes for Each User" -ForegroundColor Yellow
Write-Host "   Run this command for each user:" -ForegroundColor Gray
Write-Host ""

foreach ($user in $testUsers) {
    Write-Host "   .\Seed-TestQuotes.ps1 ``" -ForegroundColor DarkGray
    Write-Host "       -UserEmail '$($user.Email)' ``" -ForegroundColor DarkGray
    Write-Host "       -UserFirstName '$($user.FirstName)' ``" -ForegroundColor DarkGray
    Write-Host "       -UserLastName '$($user.LastName)'" -ForegroundColor DarkGray
    Write-Host ""
}

Write-Host "3. Test Multi-User Isolation (Manual)" -ForegroundColor Yellow
Write-Host "   a. Login to mobile app as User 1 ($($testUsers[0].Email))" -ForegroundColor Gray
Write-Host "   b. Navigate to Quote Dashboard" -ForegroundColor Gray
Write-Host "   c. Note the Quote IDs displayed" -ForegroundColor Gray
Write-Host "   d. Logout and login as User 2 ($($testUsers[1].Email))" -ForegroundColor Gray
Write-Host "   e. Verify User 1's quotes DO NOT appear" -ForegroundColor Gray
Write-Host "   f. Verify only User 2's quotes are visible" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Verify Quote Actions Are User-Specific" -ForegroundColor Yellow
Write-Host "   - Try to accept/cancel quotes owned by current user ? Should work" -ForegroundColor Gray
Write-Host "   - Try to access another user's quote by ID ? Should fail (403 Forbidden)" -ForegroundColor Gray
Write-Host ""

Write-Host "Test User Credentials Summary:" -ForegroundColor Cyan
$testUsers | Select-Object Username, Email, Password | Format-Table -AutoSize

Write-Host "? SECURITY NOTE: These are test credentials only!" -ForegroundColor Yellow
Write-Host "  Do not use in production environments." -ForegroundColor Yellow
Write-Host ""
