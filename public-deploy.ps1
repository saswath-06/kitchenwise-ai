# KitchenWise Public Deployment Script
param(
    [string]$DeploymentTarget = "azure", # azure, railway, manual
    [string]$ApiUrl = "",
    [string]$ResourceGroupName = "KitchenWise-RG",
    [string]$AppName = "kitchenwise-api",
    [switch]$Production,
    [switch]$SkipBuild
)

Write-Host "🚀 KitchenWise Public Deployment" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Validate parameters
if ($Production -and [string]::IsNullOrEmpty($ApiUrl)) {
    Write-Host "❌ Production deployment requires -ApiUrl parameter" -ForegroundColor Red
    exit 1
}

# Step 1: Build applications
if (-not $SkipBuild) {
    Write-Host "🔨 Building applications..." -ForegroundColor Cyan
    
    # Build API for production
    Write-Host "Building API..." -ForegroundColor Yellow
    dotnet publish KitchenWise.Api -c Release -o ./publish/api-production
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ API build failed!" -ForegroundColor Red
        exit 1
    }
    
    # Build Desktop app
    Write-Host "Building Desktop app..." -ForegroundColor Yellow
    dotnet publish KitchenWise.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/desktop-production
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Desktop build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
}

# Step 2: Create production configuration
Write-Host "⚙️ Creating production configurations..." -ForegroundColor Cyan

$apiConfig = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "REPLACE_WITH_YOUR_DATABASE_CONNECTION_STRING"
  },
  "OpenAI": {
    "ApiKey": "REPLACE_WITH_YOUR_OPENAI_API_KEY"
  },
  "Auth0": {
    "Domain": "REPLACE_WITH_YOUR_AUTH0_DOMAIN",
    "Audience": "https://kitchenwise-api",
    "ClientId": "REPLACE_WITH_YOUR_AUTH0_CLIENT_ID",
    "ClientSecret": "REPLACE_WITH_YOUR_AUTH0_CLIENT_SECRET"
  },
  "AllowedHosts": "*"
}
"@

$desktopConfig = @"
{
  "ApiSettings": {
    "BaseUrl": "$ApiUrl"
  },
  "Auth0": {
    "Domain": "REPLACE_WITH_YOUR_AUTH0_DOMAIN",
    "ClientId": "REPLACE_WITH_YOUR_AUTH0_DESKTOP_CLIENT_ID",
    "RedirectUri": "http://localhost:8080/callback",
    "PostLogoutRedirectUri": "http://localhost:8080"
  }
}
"@

# Save configurations
$apiConfig | Out-File -FilePath "./publish/api-production/appsettings.Production.json" -Encoding UTF8
$desktopConfig | Out-File -FilePath "./publish/desktop-production/appsettings.json" -Encoding UTF8

Write-Host "✅ Configuration files created" -ForegroundColor Green

# Step 3: Deployment-specific actions
switch ($DeploymentTarget.ToLower()) {
    "azure" {
        Write-Host "☁️ Preparing for Azure deployment..." -ForegroundColor Cyan
        
        # Create deployment package
        Compress-Archive -Path "./publish/api-production/*" -DestinationPath "./publish/kitchenwise-api-azure.zip" -Force
        
        Write-Host "📦 Azure deployment package created: ./publish/kitchenwise-api-azure.zip" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps for Azure:" -ForegroundColor Yellow
        Write-Host "1. Create Azure resources using azure-deploy.md guide" -ForegroundColor White
        Write-Host "2. Configure environment variables in Azure Portal" -ForegroundColor White
        Write-Host "3. Upload kitchenwise-api-azure.zip to your Azure App Service" -ForegroundColor White
    }
    
    "railway" {
        Write-Host "🚂 Preparing for Railway deployment..." -ForegroundColor Cyan
        
        # Check if railway CLI is available
        if (Get-Command railway -ErrorAction SilentlyContinue) {
            Write-Host "Railway CLI found. You can now run:" -ForegroundColor Green
            Write-Host "  railway up" -ForegroundColor White
        } else {
            Write-Host "⚠️ Railway CLI not found. Install with: npm install -g @railway/cli" -ForegroundColor Yellow
        }
        
        Write-Host "📦 API ready for Railway deployment" -ForegroundColor Green
        Write-Host "See railway-deploy.md for complete instructions" -ForegroundColor Cyan
    }
    
    "manual" {
        Write-Host "📁 Manual deployment package prepared" -ForegroundColor Cyan
        Write-Host "API files: ./publish/api-production/" -ForegroundColor White
        Write-Host "Desktop app: ./publish/desktop-production/KitchenWise.Desktop.exe" -ForegroundColor White
    }
}

# Step 4: Create distribution package
Write-Host "📦 Creating distribution package..." -ForegroundColor Cyan

$distributionPath = "./publish/KitchenWise-Distribution"
New-Item -ItemType Directory -Path $distributionPath -Force | Out-Null

# Copy desktop app
Copy-Item "./publish/desktop-production/KitchenWise.Desktop.exe" -Destination "$distributionPath/"
Copy-Item "./publish/desktop-production/appsettings.json" -Destination "$distributionPath/"

# Copy documentation
Copy-Item "DEPLOYMENT_GUIDE.md" -Destination "$distributionPath/"
Copy-Item "README.md" -Destination "$distributionPath/"

# Create installer script
$installerScript = @"
@echo off
echo 🍳 KitchenWise Installation
echo ========================
echo.
echo Copying files...
if not exist "%LOCALAPPDATA%\KitchenWise" mkdir "%LOCALAPPDATA%\KitchenWise"
copy "KitchenWise.Desktop.exe" "%LOCALAPPDATA%\KitchenWise\"
copy "appsettings.json" "%LOCALAPPDATA%\KitchenWise\"

echo Creating shortcuts...
powershell -Command "& {`$WshShell = New-Object -comObject WScript.Shell; `$Shortcut = `$WshShell.CreateShortcut('%USERPROFILE%\Desktop\KitchenWise.lnk'); `$Shortcut.TargetPath = '%LOCALAPPDATA%\KitchenWise\KitchenWise.Desktop.exe'; `$Shortcut.Save()}"

echo ✅ Installation complete!
echo KitchenWise has been installed and a desktop shortcut created.
pause
"@

$installerScript | Out-File -FilePath "$distributionPath/install.bat" -Encoding ASCII

Write-Host "✅ Distribution package created: $distributionPath" -ForegroundColor Green

# Step 5: Summary
Write-Host ""
Write-Host "🎉 Public Deployment Preparation Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Files created:" -ForegroundColor Yellow
Write-Host "  • API: ./publish/api-production/" -ForegroundColor White
Write-Host "  • Desktop: ./publish/desktop-production/" -ForegroundColor White
Write-Host "  • Distribution: $distributionPath" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Configuration needed:" -ForegroundColor Yellow
Write-Host "  • Database connection string" -ForegroundColor White
Write-Host "  • OpenAI API key" -ForegroundColor White
Write-Host "  • Auth0 credentials" -ForegroundColor White
Write-Host ""
Write-Host "📖 Next steps:" -ForegroundColor Yellow
Write-Host "  1. Deploy API using $DeploymentTarget-deploy.md guide" -ForegroundColor White
Write-Host "  2. Configure all environment variables" -ForegroundColor White
Write-Host "  3. Test API endpoints" -ForegroundColor White
Write-Host "  4. Distribute desktop app to users" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Your KitchenWise app will be publicly accessible!" -ForegroundColor Green
