<#
.SYNOPSIS
    Phase Alpha - Environment Health Check
.DESCRIPTION
    Verifies AdminAPI, Auth Server, and dependencies are running and accessible
.NOTES
    Compatible with PowerShell 5.1+
    Author: Bellwood Global Mobile Team
    Version: 1.0
#>

param(
    [string]$AdminApiUrl = "https://localhost:5206",
    [string]$AuthServerUrl = "https://localhost:5001"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Phase Alpha Environment Health Check" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$healthChecks = @()

# Function to test HTTP endpoint
function Test-HttpEndpoint {
    param(
        [string]$Url,
        [string]$Name
    )
    
    try {
        Write-Host "Testing $Name at $Url..." -NoNewline
        
        # Skip SSL validation for localhost testing
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $response = Invoke-WebRequest -Uri "$Url/health" -Method Get -SkipCertificateCheck -TimeoutSec 5 -ErrorAction Stop
        } else {
            # PowerShell 5.1 workaround for SSL
            [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
            $response = Invoke-WebRequest -Uri "$Url/health" -Method Get -TimeoutSec 5 -ErrorAction Stop
        }
        
        Write-Host " ? OK (Status: $($response.StatusCode))" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host " ? FAILED" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Test AdminAPI
$adminApiHealth = Test-HttpEndpoint -Url $AdminApiUrl -Name "AdminAPI"
$healthChecks += [PSCustomObject]@{
    Service = "AdminAPI"
    Status = if ($adminApiHealth) { "? Running" } else { "? Down" }
    Url = $AdminApiUrl
}

# Test Auth Server
$authServerHealth = Test-HttpEndpoint -Url $AuthServerUrl -Name "Auth Server"
$healthChecks += [PSCustomObject]@{
    Service = "Auth Server"
    Status = if ($authServerHealth) { "? Running" } else { "? Down" }
    Url = $AuthServerUrl
}

# Check for quote data files
Write-Host ""
Write-Host "Checking for test data files..." -NoNewline
$quotesDataPath = "..\..\AdminAPI\Data\quotes"
if (Test-Path $quotesDataPath) {
    $quoteFiles = Get-ChildItem -Path $quotesDataPath -Filter "*.json" -ErrorAction SilentlyContinue
    Write-Host " ? Found $($quoteFiles.Count) quote files" -ForegroundColor Green
    $healthChecks += [PSCustomObject]@{
        Service = "Quote Data Files"
        Status = "? Found ($($quoteFiles.Count) files)"
        Url = $quotesDataPath
    }
} else {
    Write-Host " ? Path not found" -ForegroundColor Yellow
    $healthChecks += [PSCustomObject]@{
        Service = "Quote Data Files"
        Status = "? Path not found"
        Url = $quotesDataPath
    }
}

# Summary
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Health Check Summary" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
$healthChecks | Format-Table -AutoSize

$allHealthy = $adminApiHealth -and $authServerHealth
if ($allHealthy) {
    Write-Host "? All services are healthy and ready for testing!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Run .\Seed-TestQuotes.ps1 to create test data" -ForegroundColor Gray
    Write-Host "2. Start the mobile app and login" -ForegroundColor Gray
    Write-Host "3. Follow the Testing-Guide.md for manual testing" -ForegroundColor Gray
    exit 0
} else {
    Write-Host "? Some services are not available. Please start them before testing." -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "- Ensure AdminAPI is running (dotnet run in AdminAPI project)" -ForegroundColor Gray
    Write-Host "- Ensure Auth Server is running on port 5001" -ForegroundColor Gray
    Write-Host "- Check that no other services are using these ports" -ForegroundColor Gray
    exit 1
}
