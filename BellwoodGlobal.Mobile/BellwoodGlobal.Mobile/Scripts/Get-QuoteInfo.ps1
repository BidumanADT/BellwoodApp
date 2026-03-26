<#
.SYNOPSIS
    Phase Alpha - Get Quote Information
.DESCRIPTION
    Retrieves and displays quote details from AdminAPI for testing
.NOTES
    Compatible with PowerShell 5.1+
    Author: Bellwood Global Mobile Team
    Version: 1.0
.EXAMPLE
    .\Get-QuoteInfo.ps1
    Lists all quotes
.EXAMPLE
    .\Get-QuoteInfo.ps1 -QuoteId "quote-123"
    Shows detailed info for a specific quote
#>

param(
    [string]$AdminApiUrl = "https://localhost:5206",
    [string]$QuoteId = ""
)

if ($PSVersionTable.PSVersion.Major -lt 6) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Quote Information Viewer" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

try {
    if ($QuoteId) {
        # Get specific quote
        Write-Host "Fetching quote: $QuoteId..." -ForegroundColor Cyan
        Write-Host ""
        
        $url = "$AdminApiUrl/quotes/$QuoteId"
        
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $quote = Invoke-RestMethod -Uri $url -Method Get -SkipCertificateCheck
        } else {
            $quote = Invoke-RestMethod -Uri $url -Method Get
        }
        
        Write-Host "Quote Details:" -ForegroundColor Green
        Write-Host "?????????????????????????????????????" -ForegroundColor Gray
        Write-Host "ID:              $($quote.id)" -ForegroundColor White
        Write-Host "Status:          $($quote.status)" -ForegroundColor $(
            switch ($quote.status) {
                "Pending" { "DarkYellow" }
                "Acknowledged" { "Blue" }
                "Responded" { "Green" }
                "Accepted" { "Gray" }
                "Cancelled" { "Red" }
                default { "White" }
            }
        )
        Write-Host "Created:         $($quote.createdUtc)" -ForegroundColor White
        Write-Host "Passenger:       $($quote.passengerName)" -ForegroundColor White
        Write-Host "Vehicle:         $($quote.vehicleClass)" -ForegroundColor White
        Write-Host "Pickup:          $($quote.pickupLocation)" -ForegroundColor White
        Write-Host "Dropoff:         $($quote.dropoffLocation)" -ForegroundColor White
        Write-Host "Pickup Time:     $($quote.pickupDateTime)" -ForegroundColor White
        
        if ($quote.acknowledgedAt) {
            Write-Host ""
            Write-Host "Acknowledged:    $($quote.acknowledgedAt)" -ForegroundColor Blue
        }
        
        if ($quote.respondedAt) {
            Write-Host ""
            Write-Host "Responded:       $($quote.respondedAt)" -ForegroundColor Green
            Write-Host "Estimated Price: `$$($quote.estimatedPrice)" -ForegroundColor Green
            Write-Host "Pickup Time:     $($quote.estimatedPickupTime)" -ForegroundColor Green
            if ($quote.notes) {
                Write-Host "Notes:           $($quote.notes)" -ForegroundColor Green
            }
        }
        
        Write-Host ""
        Write-Host "Use this Quote ID for testing:" -ForegroundColor Cyan
        Write-Host "  .\Simulate-StatusChange.ps1 -QuoteId '$($quote.id)' -Action [Acknowledge|Respond|Cancel]" -ForegroundColor Gray
        
    } else {
        # List all quotes
        Write-Host "Fetching all quotes..." -ForegroundColor Cyan
        Write-Host ""
        
        $url = "$AdminApiUrl/quotes/list?take=100"
        
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $quotes = Invoke-RestMethod -Uri $url -Method Get -SkipCertificateCheck
        } else {
            $quotes = Invoke-RestMethod -Uri $url -Method Get
        }
        
        if ($quotes.Count -eq 0) {
            Write-Host "No quotes found." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Run .\Seed-TestQuotes.ps1 to create test data" -ForegroundColor Gray
        } else {
            Write-Host "Found $($quotes.Count) quotes:" -ForegroundColor Green
            Write-Host ""
            
            $quotes | ForEach-Object {
                $statusColor = switch ($_.status) {
                    "Pending" { "DarkYellow" }
                    "Acknowledged" { "Blue" }
                    "Responded" { "Green" }
                    "Accepted" { "Gray" }
                    "Cancelled" { "Red" }
                    default { "White" }
                }
                
                Write-Host "$($_.id)" -ForegroundColor White -NoNewline
                Write-Host " ? " -ForegroundColor Gray -NoNewline
                Write-Host "$($_.status.PadRight(12))" -ForegroundColor $statusColor -NoNewline
                Write-Host " ? " -ForegroundColor Gray -NoNewline
                Write-Host "$($_.passengerName.PadRight(20))" -ForegroundColor Cyan -NoNewline
                Write-Host " ? " -ForegroundColor Gray -NoNewline
                Write-Host "$($_.vehicleClass)" -ForegroundColor White
            }
            
            Write-Host ""
            Write-Host "To view details, run:" -ForegroundColor Gray
            Write-Host "  .\Get-QuoteInfo.ps1 -QuoteId '<quote-id>'" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "? Error fetching quote information" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Yellow
    
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "- AdminAPI not running" -ForegroundColor Gray
    Write-Host "- Invalid Quote ID" -ForegroundColor Gray
    Write-Host "- Network connectivity issues" -ForegroundColor Gray
    
    exit 1
}
