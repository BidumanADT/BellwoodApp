<#
.SYNOPSIS
    Phase Alpha - Status Change Simulator
.DESCRIPTION
    Simulates quote status changes to test automatic polling and notifications
.NOTES
    Compatible with PowerShell 5.1+
    Author: Bellwood Global Mobile Team
    Version: 1.0
.EXAMPLE
    .\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Acknowledge
    Acknowledges a quote (Pending ? Acknowledged)
.EXAMPLE
    .\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Respond -EstimatedPrice 95.50
    Responds to a quote with price (Acknowledged ? Responded)
.EXAMPLE
    .\Simulate-StatusChange.ps1 -QuoteId "quote-123" -Action Cancel
    Cancels a quote
#>

param(
    [string]$AdminApiUrl = "https://localhost:5206",
    [Parameter(Mandatory=$true)]
    [string]$QuoteId,
    [Parameter(Mandatory=$true)]
    [ValidateSet("Acknowledge", "Respond", "Cancel")]
    [string]$Action,
    [decimal]$EstimatedPrice = 0,
    [string]$Notes = "",
    [switch]$WaitForPolling
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Quote Status Change Simulator" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

if ($PSVersionTable.PSVersion.Major -lt 6) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
}

$headers = @{
    "Content-Type" = "application/json"
}

try {
    switch ($Action) {
        "Acknowledge" {
            Write-Host "Acknowledging quote $QuoteId..." -NoNewline
            $url = "$AdminApiUrl/quotes/$QuoteId/acknowledge"
            
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -SkipCertificateCheck
            } else {
                $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers
            }
            
            Write-Host " ? Done" -ForegroundColor Green
            Write-Host ""
            Write-Host "Status changed: Pending ? Acknowledged" -ForegroundColor Cyan
            Write-Host "Display in app: 'Awaiting Response' ? 'Under Review' (Blue)" -ForegroundColor Gray
        }
        
        "Respond" {
            Write-Host "Responding to quote $QuoteId with price..." -NoNewline
            
            if ($EstimatedPrice -eq 0) {
                $EstimatedPrice = Get-Random -Minimum 50 -Maximum 200
                Write-Host ""
                Write-Host "  No price specified, using random: `$$EstimatedPrice" -ForegroundColor Yellow
            }
            
            $estimatedPickupTime = (Get-Date).AddDays(5).AddMinutes(-15).ToString("o")
            
            $responseData = @{
                estimatedPrice = $EstimatedPrice
                estimatedPickupTime = $estimatedPickupTime
                notes = if ($Notes) { $Notes } else { "Estimated based on standard route pricing. Final price subject to confirmation." }
            } | ConvertTo-Json
            
            $url = "$AdminApiUrl/quotes/$QuoteId/respond"
            
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $responseData -SkipCertificateCheck
            } else {
                $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $responseData
            }
            
            Write-Host " ? Done" -ForegroundColor Green
            Write-Host ""
            Write-Host "Status changed: Acknowledged ? Responded" -ForegroundColor Cyan
            Write-Host "Estimated Price: `$$EstimatedPrice" -ForegroundColor Green
            Write-Host "Display in app: 'Under Review' ? 'Response Received - `$$EstimatedPrice' (Green)" -ForegroundColor Gray
        }
        
"Cancel" {
            Write-Host "Cancelling quote $QuoteId..." -NoNewline
            $url = "$AdminApiUrl/quotes/$QuoteId/cancel"
            
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -SkipCertificateCheck
            } else {
                $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers
            }
            
            Write-Host " ? Done" -ForegroundColor Green
            Write-Host ""
            Write-Host "Status changed: [Current] ? Cancelled" -ForegroundColor Cyan
            Write-Host "Display in app: 'Cancelled' (Red)" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "? Status change triggered successfully!" -ForegroundColor Green
    Write-Host ""
    
    if ($WaitForPolling) {
        Write-Host "Waiting for automatic polling to detect change..." -ForegroundColor Cyan
        Write-Host "App polls every 30 seconds when Quote Dashboard is open" -ForegroundColor Gray
        Write-Host ""
        
        for ($i = 30; $i -gt 0; $i--) {
            Write-Host "`rCountdown: $i seconds remaining... " -NoNewline -ForegroundColor Yellow
            Start-Sleep -Seconds 1
        }
        
        Write-Host ""
        Write-Host ""
        Write-Host "? Polling window complete!" -ForegroundColor Green
        Write-Host "Check the mobile app for:" -ForegroundColor Cyan
        Write-Host "  - Updated quote status in dashboard" -ForegroundColor Gray
        Write-Host "  - Notification banner showing status change" -ForegroundColor Gray
    } else {
        Write-Host "Manual Testing Steps:" -ForegroundColor Cyan
        Write-Host "1. Open Quote Dashboard in mobile app" -ForegroundColor Gray
        Write-Host "2. Wait up to 30 seconds for automatic polling" -ForegroundColor Gray
        Write-Host "3. Verify notification banner appears" -ForegroundColor Gray
        Write-Host "4. Verify quote status updated in list" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Tip: Add -WaitForPolling to auto-wait for 30 seconds" -ForegroundColor Yellow
    }
}
catch {
    Write-Host " ? FAILED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Yellow
    
    if ($_.ErrorDetails.Message) {
        try {
            $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
            if ($errorObj.error) {
                Write-Host "API Error: $($errorObj.error)" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "- Quote ID not found (verify QuoteId parameter)" -ForegroundColor Gray
    Write-Host "- Quote already in terminal status (Accepted/Cancelled)" -ForegroundColor Gray
    Write-Host "- AdminAPI not running" -ForegroundColor Gray
    Write-Host "- Network connectivity issues" -ForegroundColor Gray
    
    exit 1
}
