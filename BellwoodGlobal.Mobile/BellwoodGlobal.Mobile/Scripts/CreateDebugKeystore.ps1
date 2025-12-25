# CreateDebugKeystore.ps1 - Create Android debug keystore

Write-Host "?? Android Debug Keystore Creator" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Create the .android directory if it doesn't exist
$androidDir = "$env:USERPROFILE\.android"
if (-not (Test-Path $androidDir)) {
    New-Item -Path $androidDir -ItemType Directory -Force | Out-Null
    Write-Host "? Created .android directory at: $androidDir" -ForegroundColor Green
}

# Path to keytool
$keytoolPath = "C:\Program Files\Microsoft\jdk-17.0.17.10-hotspot\bin\keytool.exe"
$keystorePath = "$androidDir\debug.keystore"

# Check if keystore already exists
if (Test-Path $keystorePath) {
    Write-Host "??  Debug keystore already exists at: $keystorePath" -ForegroundColor Yellow
    $response = Read-Host "Do you want to delete and recreate it? (y/n)"
    if ($response -ne 'y') {
        Write-Host "? Aborted. Keeping existing keystore." -ForegroundColor Red
        exit
    }
    Remove-Item $keystorePath -Force
    Write-Host "???  Deleted old keystore" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? Creating new debug keystore..." -ForegroundColor Cyan
Write-Host ""

# Create the keystore
& $keytoolPath -genkeypair `
    -keystore $keystorePath `
    -storepass android `
    -alias androiddebugkey `
    -keypass android `
    -keyalg RSA `
    -keysize 2048 `
    -validity 10000 `
    -dname "CN=Android Debug,O=Android,C=US" `
    -v

Write-Host ""

# Verify creation
if (Test-Path $keystorePath) {
    Write-Host "? Debug keystore created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Keystore Information:" -ForegroundColor Cyan
    Write-Host "  Location: $keystorePath" -ForegroundColor White
    Write-Host "  Store Password: android" -ForegroundColor White
    Write-Host "  Key Alias: androiddebugkey" -ForegroundColor White
    Write-Host "  Key Password: android" -ForegroundColor White
    Write-Host "  Validity: 10,000 days" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Clean your project in Visual Studio" -ForegroundColor White
    Write-Host "  2. Press F5 to rebuild and deploy" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "? Failed to create keystore!" -ForegroundColor Red
    Write-Host "Check that keytool is installed at: $keytoolPath" -ForegroundColor Yellow
}

Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
