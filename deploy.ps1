# KitchenWise Deployment Script
# This script helps deploy both the API and Desktop application

param(
    [switch]$Desktop,
    [switch]$Api,
    [switch]$All,
    [string]$OutputPath = "./publish"
)

Write-Host "🚀 KitchenWise Deployment Script" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

if (-not $Desktop -and -not $Api -and -not $All) {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -All                    # Deploy both API and Desktop" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -Desktop                # Deploy only Desktop app" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -Api                    # Deploy only API" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -All -OutputPath ./dist # Custom output directory" -ForegroundColor White
    exit
}

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

if ($Desktop -or $All) {
    Write-Host "📱 Publishing Desktop Application..." -ForegroundColor Cyan
    
    $desktopOutput = Join-Path $OutputPath "desktop"
    
    # Publish self-contained desktop app
    dotnet publish KitchenWise.Desktop `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $desktopOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Desktop app published to: $desktopOutput" -ForegroundColor Green
        
        # Copy configuration template
        $configTemplate = @"
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-domain.com"
  },
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "ClientId": "YOUR_AUTH0_CLIENT_ID"
  }
}
"@
        $configPath = Join-Path $desktopOutput "appsettings.json"
        $configTemplate | Out-File -FilePath $configPath -Encoding UTF8
        Write-Host "📄 Configuration template created: $configPath" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Desktop app publish failed!" -ForegroundColor Red
    }
}

if ($Api -or $All) {
    Write-Host "🌐 Publishing API..." -ForegroundColor Cyan
    
    $apiOutput = Join-Path $OutputPath "api"
    
    # Publish API
    dotnet publish KitchenWise.Api `
        -c Release `
        -o $apiOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ API published to: $apiOutput" -ForegroundColor Green
        
        # Create production appsettings template
        $apiConfigTemplate = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_DATABASE_CONNECTION_STRING"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  },
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "Audience": "YOUR_AUTH0_AUDIENCE",
    "ClientId": "YOUR_AUTH0_CLIENT_ID",
    "ClientSecret": "YOUR_AUTH0_CLIENT_SECRET"
  },
  "AllowedHosts": "*"
}
"@
        $apiConfigPath = Join-Path $apiOutput "appsettings.Production.json"
        $apiConfigTemplate | Out-File -FilePath $apiConfigPath -Encoding UTF8
        Write-Host "📄 Production config template created: $apiConfigPath" -ForegroundColor Yellow
    } else {
        Write-Host "❌ API publish failed!" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎉 Deployment Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Configure API keys and database connections" -ForegroundColor White
Write-Host "2. Deploy API to your hosting service (Azure, IIS, etc.)" -ForegroundColor White
Write-Host "3. Update desktop app configuration with API URL" -ForegroundColor White
Write-Host "4. Distribute KitchenWise.Desktop.exe to users" -ForegroundColor White
Write-Host ""
Write-Host "📖 See DEPLOYMENT_GUIDE.md for detailed instructions" -ForegroundColor Cyan
