# Decode-ReleaseKeystore.ps1 - Decode base64 keystore from env for release signing
#
# Reads ANDROID_KEYSTORE_BASE64 and writes the .jks file to a specified path.
# Safe for local and CI use. Never prints the raw secret.

param(
    [string]$OutputPath = "$env:TEMP\bellwood-release.jks"
)

Write-Host "Android Release Keystore Decoder" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# 1. Validate the env var exists and is not empty
$base64 = $env:ANDROID_KEYSTORE_BASE64
if ([string]::IsNullOrWhiteSpace($base64)) {
    Write-Host "ANDROID_KEYSTORE_BASE64 is not set or is empty." -ForegroundColor Red
    Write-Host "  Set it before running this script:" -ForegroundColor Yellow
    Write-Host '  $env:ANDROID_KEYSTORE_BASE64 = Get-Content .\bellwood-release.jks.b64 -Raw' -ForegroundColor Gray
    exit 1
}

Write-Host "ANDROID_KEYSTORE_BASE64 found ($($base64.Length) chars)" -ForegroundColor Green

# 2. Ensure output directory exists
$outputDir = Split-Path -Parent $OutputPath
if (-not (Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    Write-Host "Created output directory: $outputDir" -ForegroundColor Green
}

# 3. Decode and write
try {
    $bytes = [Convert]::FromBase64String($base64)
    [IO.File]::WriteAllBytes($OutputPath, $bytes)
} catch {
    Write-Host "Failed to decode base64: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 4. Verify the file was written and has content
if (-not (Test-Path $OutputPath)) {
    Write-Host "Keystore file was not created at: $OutputPath" -ForegroundColor Red
    exit 1
}

$size = (Get-Item $OutputPath).Length
if ($size -eq 0) {
    Remove-Item $OutputPath -Force
    Write-Host "Decoded file was empty. Check your base64 input." -ForegroundColor Red
    exit 1
}

Write-Host "Keystore decoded: $OutputPath ($size bytes)" -ForegroundColor Green
Write-Host ""
Write-Host "Use with dotnet publish:" -ForegroundColor Cyan
Write-Host "  -p:AndroidSigningKeyStore=`"$OutputPath`"" -ForegroundColor White
