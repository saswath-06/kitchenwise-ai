# Simple script to connect desktop app to existing Railway deployment
param(
    [Parameter(Mandatory=$true)]
    [string]$RailwayUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$Auth0Domain,
    
    [Parameter(Mandatory=$true)]
    [string]$Auth0ClientId
)

Write-Host "🔗 Connecting KitchenWise Desktop to Railway" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Validate URL format
if (-not $RailwayUrl.StartsWith("https://")) {
    Write-Host "⚠️ Railway URL should start with https://" -ForegroundColor Yellow
    $RailwayUrl = "https://" + $RailwayUrl.TrimStart("http://").TrimStart("https://")
}

$RailwayUrl = $RailwayUrl.TrimEnd('/')

Write-Host "🌐 Railway API URL: $RailwayUrl" -ForegroundColor Cyan
Write-Host "🔐 Auth0 Domain: $Auth0Domain" -ForegroundColor Cyan
Write-Host "🆔 Auth0 Client ID: $Auth0ClientId" -ForegroundColor Cyan
Write-Host ""

# Create the configuration
Write-Host "⚙️ Creating configuration..." -ForegroundColor Yellow

$config = @{
    "ApiSettings" = @{
        "BaseUrl" = $RailwayUrl
    }
    "Auth0" = @{
        "Domain" = $Auth0Domain
        "ClientId" = $Auth0ClientId
        "RedirectUri" = "http://localhost:8080/callback"
        "PostLogoutRedirectUri" = "http://localhost:8080"
    }
}

$configJson = $config | ConvertTo-Json -Depth 3

# Update source configuration
$sourcePath = "KitchenWise.Desktop/appsettings.json"
$configJson | Out-File -FilePath $sourcePath -Encoding UTF8
Write-Host "✅ Updated source config: $sourcePath" -ForegroundColor Green

# Build desktop app with new configuration
Write-Host "🔨 Building desktop app..." -ForegroundColor Yellow

$outputPath = "./publish/desktop-connected"
dotnet publish KitchenWise.Desktop `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputPath

if ($LASTEXITCODE -eq 0) {
    # Copy config to output
    $configJson | Out-File -FilePath "$outputPath/appsettings.json" -Encoding UTF8
    
    Write-Host "✅ Desktop app built successfully!" -ForegroundColor Green
    Write-Host "📱 Location: $outputPath/KitchenWise.Desktop.exe" -ForegroundColor Yellow
    
    # Create test batch file
    $testBatch = @"
@echo off
echo 🧪 Testing KitchenWise Connection
echo ===============================
echo.
echo Railway API: $RailwayUrl
echo Auth0 Domain: $Auth0Domain
echo.
echo Opening KitchenWise...
start KitchenWise.Desktop.exe
echo.
echo ✅ If you can login and use the app, connection is working!
echo ❌ If you get errors, check:
echo   - Railway API is running: $RailwayUrl/swagger
echo   - Auth0 credentials are correct
echo   - Internet connection is working
echo.
pause
"@
    
    $testBatch | Out-File -FilePath "$outputPath/test-connection.bat" -Encoding ASCII
    
    Write-Host ""
    Write-Host "🎉 Connection Complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🧪 Test your connection:" -ForegroundColor Yellow
    Write-Host "  1. Run: $outputPath/test-connection.bat" -ForegroundColor White
    Write-Host "  2. Or directly: $outputPath/KitchenWise.Desktop.exe" -ForegroundColor White
    Write-Host ""
    Write-Host "🌐 Verify API is running: $RailwayUrl/swagger" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📦 Ready for distribution:" -ForegroundColor Green
    Write-Host "  • Share: $outputPath/KitchenWise.Desktop.exe" -ForegroundColor White
    Write-Host "  • File size: ~179 MB (self-contained)" -ForegroundColor White
    Write-Host "  • No installation required" -ForegroundColor White
} else {
    Write-Host "❌ Build failed! Check the error messages above." -ForegroundColor Red
    exit 1
}
