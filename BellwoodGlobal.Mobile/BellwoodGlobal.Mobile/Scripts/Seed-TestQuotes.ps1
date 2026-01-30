<#
.SYNOPSIS
    Phase Alpha - Test Data Seeder
.DESCRIPTION
    Creates test quotes with various statuses for Phase Alpha testing
.NOTES
    Compatible with PowerShell 5.1+
    Author: Bellwood Global Mobile Team
    Version: 1.0
.EXAMPLE
    .\Seed-TestQuotes.ps1
    Creates test quotes with default settings
.EXAMPLE
    .\Seed-TestQuotes.ps1 -UserEmail "testuser@example.com"
    Creates quotes for a specific user
#>

param(
    [string]$AdminApiUrl = "https://localhost:5206",
    [string]$UserEmail = "testuser@example.com",
    [string]$UserFirstName = "Test",
    [string]$UserLastName = "User"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Phase Alpha Test Data Seeder" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Skip SSL validation for localhost
if ($PSVersionTable.PSVersion.Major -lt 6) {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
}

# Sample test quotes with different scenarios
$testQuotes = @(
    @{
        PassengerName = "John Doe"
        VehicleClass = "Sedan"
        PickupLocation = "O'Hare International Airport, Terminal 1"
        DropoffLocation = "Downtown Chicago, 100 N LaSalle St"
        PickupDateTime = (Get-Date).AddDays(7)
        PassengerCount = 2
        CheckedBags = 2
        CarryOnBags = 1
        PickupStyle = "Curbside"
    },
    @{
        PassengerName = "Jane Smith"
        VehicleClass = "SUV"
        PickupLocation = "Midway Airport"
        DropoffLocation = "Oak Park, 150 N Oak Park Ave"
        PickupDateTime = (Get-Date).AddDays(5)
        PassengerCount = 4
        CheckedBags = 3
        CarryOnBags = 2
        PickupStyle = "MeetAndGreet"
        PickupSignText = "Smith Family"
    },
    @{
        PassengerName = "Bob Johnson"
        VehicleClass = "Executive Sedan"
        PickupLocation = "Union Station Chicago"
        DropoffLocation = "Rosemont Convention Center"
        PickupDateTime = (Get-Date).AddDays(3)
        PassengerCount = 1
        CheckedBags = 1
        CarryOnBags = 1
        PickupStyle = "Curbside"
    },
    @{
        PassengerName = "Alice Williams"
        VehicleClass = "Luxury SUV"
        PickupLocation = "Downtown Chicago - 100 N LaSalle"
        DropoffLocation = "O'Hare International Airport, Terminal 5"
        PickupDateTime = (Get-Date).AddDays(10)
        PassengerCount = 3
        CheckedBags = 4
        CarryOnBags = 2
        PickupStyle = "Curbside"
        OutboundFlight = "UA1234"
    },
    @{
        PassengerName = "Charlie Brown"
        VehicleClass = "Sedan"
        PickupLocation = "Navy Pier"
        DropoffLocation = "Schaumburg - Woodfield Mall"
        PickupDateTime = (Get-Date).AddDays(14)
        PassengerCount = 2
        CheckedBags = 0
        CarryOnBags = 1
        PickupStyle = "Curbside"
    }
)

Write-Host "This script will create $($testQuotes.Count) test quotes for user: $UserEmail" -ForegroundColor Cyan
Write-Host ""
Write-Host "? NOTE: These quotes will be created in 'Pending' status." -ForegroundColor Yellow
Write-Host "   Use the AdminPortal or Simulate-StatusChange.ps1 to:" -ForegroundColor Yellow
Write-Host "   - Acknowledge quotes" -ForegroundColor Yellow
Write-Host "   - Respond with price/ETA" -ForegroundColor Yellow
Write-Host "   - Test the full quote lifecycle" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to continue or Ctrl+C to cancel..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "Creating test quotes..." -ForegroundColor Cyan
Write-Host ""

$created = 0
$failed = 0

foreach ($quote in $testQuotes) {
    try {
        Write-Host "Creating quote: $($quote.PassengerName) - $($quote.VehicleClass)..." -NoNewline
        
        # Build quote draft payload
        $quoteDraft = @{
            booker = @{
                firstName = $UserFirstName
                lastName = $UserLastName
                phoneNumber = "312-555-0001"
                emailAddress = $UserEmail
            }
            passenger = @{
                firstName = $quote.PassengerName.Split(' ')[0]
                lastName = if ($quote.PassengerName.Split(' ').Count -gt 1) { $quote.PassengerName.Split(' ')[-1] } else { "Passenger" }
                phoneNumber = "312-555-010$created"
                emailAddress = "$($quote.PassengerName.Replace(' ', '').ToLower())@example.com"
            }
            vehicleClass = $quote.VehicleClass
            pickupDateTime = $quote.PickupDateTime.ToString("yyyy-MM-ddTHH:mm:ss")
            pickupLocation = $quote.PickupLocation
            pickupStyle = $quote.PickupStyle
            dropoffLocation = $quote.DropoffLocation
            roundTrip = $false
            passengerCount = $quote.PassengerCount
            checkedBags = $quote.CheckedBags
            carryOnBags = $quote.CarryOnBags
        }
        
        # Add optional fields
        if ($quote.PickupSignText) {
            $quoteDraft.pickupSignText = $quote.PickupSignText
        }
        
        if ($quote.OutboundFlight) {
            $quoteDraft.outboundFlight = @{
                flightNumber = $quote.OutboundFlight
            }
        }
        
        $body = $quoteDraft | ConvertTo-Json -Depth 10
        
        $url = "$AdminApiUrl/quotes"
        
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json" -SkipCertificateCheck
        } else {
            $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json"
        }
        
        Write-Host " ? Created (ID: $($response.id))" -ForegroundColor Green
        $created++
        
    }
    catch {
        Write-Host " ? Failed" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
        if ($_.ErrorDetails.Message) {
            Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
        }
        $failed++
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Created:  $created quotes" -ForegroundColor Green
Write-Host "Failed:   $failed quotes" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Gray" })
Write-Host ""

if ($created -gt 0) {
    Write-Host "? Test data created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Open AdminPortal to acknowledge/respond to quotes" -ForegroundColor Gray
    Write-Host "   OR use .\Simulate-StatusChange.ps1 to trigger status changes" -ForegroundColor Gray
    Write-Host "2. Open mobile app and navigate to Quote Dashboard" -ForegroundColor Gray
    Write-Host "3. Verify all $created quotes appear in the list" -ForegroundColor Gray
    Write-Host "4. Follow Testing-Guide.md for comprehensive testing" -ForegroundColor Gray
} else {
    Write-Host "? No quotes were created. Please check the errors above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "- AdminAPI not running (run dotnet run in AdminAPI project)" -ForegroundColor Gray
    Write-Host "- Invalid authentication (ensure user exists in Auth Server)" -ForegroundColor Gray
    Write-Host "- Network connectivity issues" -ForegroundColor Gray
}
