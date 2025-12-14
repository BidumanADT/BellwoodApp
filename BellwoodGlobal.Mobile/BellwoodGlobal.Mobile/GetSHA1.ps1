# GetSHA1.ps1 - Find JDK keytool and get SHA-1 fingerprint

Write-Host "🔍 Searching for keytool..." -ForegroundColor Cyan

# Search common JDK locations
$searchPaths = @(
    "C:\Program Files\Microsoft\jdk-*\bin\keytool.exe",
    "C:\Program Files\Eclipse Adoptium\jdk-*\bin\keytool.exe",
    "C:\Program Files\Java\jdk-*\bin\keytool.exe",
    "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe",
    "$env:LOCALAPPDATA\Android\Sdk\jre\bin\keytool.exe",
    "$env:ProgramFiles\dotnet\packs\Microsoft.Android.Sdk*\*\tools\keytool.exe"
)

$keytoolPath = $null

foreach ($pattern in $searchPaths) {
    $found = Get-Item $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $keytoolPath = $found.FullName
        Write-Host "✅ Found keytool at: $keytoolPath" -ForegroundColor Green
        break
    }
}

if (-not $keytoolPath) {
    Write-Host "❌ keytool not found. Please install Java JDK." -ForegroundColor Red
    Write-Host "   Download from: https://aka.ms/download-jdk/microsoft-jdk-17-windows-x64.msi" -ForegroundColor Yellow
    exit
}

# Run keytool to get SHA-1
$keystorePath = "$env:USERPROFILE\.android\debug.keystore"

if (-not (Test-Path $keystorePath)) {
    Write-Host "❌ Debug keystore not found at: $keystorePath" -ForegroundColor Red
    Write-Host "   Build your Android app once to generate it." -ForegroundColor Yellow
    exit
}

Write-Host ""
Write-Host "📋 Getting SHA-1 fingerprint from debug keystore..." -ForegroundColor Cyan
Write-Host ""

& $keytoolPath -list -v -keystore $keystorePath -alias androiddebugkey -storepass android -keypass android

Write-Host ""
Write-Host "✅ Done! Copy the SHA1 value above." -ForegroundColor Green
Write-Host ""
Write-Host "📌 Package name: com.bellwoodglobal.mobile" -ForegroundColor Cyan
