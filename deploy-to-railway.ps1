# KitchenWise Railway Connection Script
param(
    [Parameter(Mandatory=$false)]
    [string]$RailwayUrl = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Auth0Domain = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Auth0DesktopClientId = "",
    
    [switch]$ConfigureOnly,
    [switch]$BuildOnly
)

Write-Host "🚂 KitchenWise Railway Deployment & Connection" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Check if Railway CLI is installed
if (-not (Get-Command railway -ErrorAction SilentlyContinue)) {
    Write-Host "⚠️ Railway CLI not found!" -ForegroundColor Yellow
    Write-Host "Install with: npm install -g @railway/cli" -ForegroundColor White
    Write-Host "Then run: railway login" -ForegroundColor White
    
    if (-not $ConfigureOnly) {
        Write-Host "❌ Cannot deploy without Railway CLI" -ForegroundColor Red
        exit 1
    }
}

# Step 1: Deploy API to Railway (if not ConfigureOnly)
if (-not $ConfigureOnly) {
    Write-Host "🚀 Deploying API to Railway..." -ForegroundColor Cyan
    
    # Create Dockerfile if it doesn't exist
    if (-not (Test-Path "Dockerfile")) {
        Write-Host "📄 Creating Dockerfile..." -ForegroundColor Yellow
        
        $dockerfile = @"
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KitchenWise.Api/KitchenWise.Api.csproj", "KitchenWise.Api/"]
RUN dotnet restore "KitchenWise.Api/KitchenWise.Api.csproj"
COPY . .
WORKDIR "/src/KitchenWise.Api"
RUN dotnet build "KitchenWise.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KitchenWise.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KitchenWise.Api.dll"]
"@
        
        $dockerfile | Out-File -FilePath "Dockerfile" -Encoding UTF8
        Write-Host "✅ Dockerfile created" -ForegroundColor Green
    }
    
    # Initialize Railway project if needed
    if (-not (Test-Path ".railway")) {
        Write-Host "🔧 Initializing Railway project..." -ForegroundColor Yellow
        railway init
        
        Write-Host "🗄️ Adding PostgreSQL database..." -ForegroundColor Yellow
        railway add --database postgresql
    }
    
    # Deploy to Railway
    Write-Host "📤 Deploying to Railway..." -ForegroundColor Yellow
    railway up
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ API deployed successfully!" -ForegroundColor Green
        
        # Get the Railway URL
        $railwayInfo = railway status --json | ConvertFrom-Json
        if ($railwayInfo -and $railwayInfo.deployments -and $railwayInfo.deployments.Count -gt 0) {
            $RailwayUrl = $railwayInfo.deployments[0].url
            Write-Host "🌐 Your API URL: $RailwayUrl" -ForegroundColor Cyan
        }
    } else {
        Write-Host "❌ Railway deployment failed!" -ForegroundColor Red
        exit 1
    }
    
    # Display environment variables that need to be set
    Write-Host ""
    Write-Host "⚙️ Configure these environment variables in Railway dashboard:" -ForegroundColor Yellow
    Write-Host "  • ASPNETCORE_ENVIRONMENT = Production" -ForegroundColor White
    Write-Host "  • ASPNETCORE_URLS = http://0.0.0.0:8080" -ForegroundColor White
    Write-Host "  • OpenAI__ApiKey = your-openai-api-key" -ForegroundColor White
    Write-Host "  • Auth0__Domain = your-domain.auth0.com" -ForegroundColor White
    Write-Host "  • Auth0__ClientId = your-api-client-id" -ForegroundColor White
    Write-Host "  • Auth0__ClientSecret = your-client-secret" -ForegroundColor White
    Write-Host "  • Auth0__Audience = https://kitchenwise-api" -ForegroundColor White
    Write-Host ""
}

# Step 2: Configure Desktop App
if (-not $BuildOnly -and -not [string]::IsNullOrEmpty($RailwayUrl)) {
    Write-Host "🖥️ Configuring Desktop App..." -ForegroundColor Cyan
    
    # Prompt for missing parameters
    if ([string]::IsNullOrEmpty($Auth0Domain)) {
        $Auth0Domain = Read-Host "Enter your Auth0 Domain (e.g., your-domain.auth0.com)"
    }
    
    if ([string]::IsNullOrEmpty($Auth0DesktopClientId)) {
        $Auth0DesktopClientId = Read-Host "Enter your Auth0 Desktop Client ID"
    }
    
    # Create desktop app configuration
    $desktopConfig = @{
        "ApiSettings" = @{
            "BaseUrl" = $RailwayUrl.TrimEnd('/')
        }
        "Auth0" = @{
            "Domain" = $Auth0Domain
            "ClientId" = $Auth0DesktopClientId
            "RedirectUri" = "http://localhost:8080/callback"
            "PostLogoutRedirectUri" = "http://localhost:8080"
        }
        "Logging" = @{
            "LogLevel" = @{
                "Default" = "Information"
                "Microsoft" = "Warning"
            }
        }
    }
    
    $configJson = $desktopConfig | ConvertTo-Json -Depth 3
    $configPath = "KitchenWise.Desktop/appsettings.json"
    $configJson | Out-File -FilePath $configPath -Encoding UTF8
    
    Write-Host "✅ Desktop configuration updated: $configPath" -ForegroundColor Green
    Write-Host "🌐 API URL: $($RailwayUrl)" -ForegroundColor Yellow
    Write-Host "🔐 Auth0 Domain: $Auth0Domain" -ForegroundColor Yellow
}

# Step 3: Build Desktop App with Railway Connection
if (-not $ConfigureOnly) {
    Write-Host "🔨 Building Desktop App with Railway connection..." -ForegroundColor Cyan
    
    $outputPath = "./publish/desktop-railway"
    
    dotnet publish KitchenWise.Desktop `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $outputPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Desktop app built successfully!" -ForegroundColor Green
        Write-Host "📱 Executable: $outputPath/KitchenWise.Desktop.exe" -ForegroundColor Yellow
        
        # Copy the configuration file to the output directory
        if (Test-Path "KitchenWise.Desktop/appsettings.json") {
            Copy-Item "KitchenWise.Desktop/appsettings.json" -Destination "$outputPath/appsettings.json"
            Write-Host "📄 Configuration copied to executable directory" -ForegroundColor Green
        }
        
        # Create a simple test script
        $testScript = @"
@echo off
echo 🧪 Testing KitchenWise Connection to Railway
echo ==========================================
echo.
echo API URL: $RailwayUrl
echo.
echo Starting KitchenWise...
start KitchenWise.Desktop.exe
echo.
echo If the app opens and you can login, the connection is working!
pause
"@
        
        $testScript | Out-File -FilePath "$outputPath/test-connection.bat" -Encoding ASCII
        Write-Host "🧪 Test script created: $outputPath/test-connection.bat" -ForegroundColor Green
    } else {
        Write-Host "❌ Desktop app build failed!" -ForegroundColor Red
        exit 1
    }
}

# Final Summary
Write-Host ""
Write-Host "🎉 Railway Deployment & Connection Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Configure environment variables in Railway dashboard" -ForegroundColor White
Write-Host "2. Set up Auth0 applications (API + Desktop)" -ForegroundColor White
Write-Host "3. Test the desktop app connection" -ForegroundColor White
Write-Host "4. Distribute the desktop app to users" -ForegroundColor White
Write-Host ""

if (-not [string]::IsNullOrEmpty($RailwayUrl)) {
    Write-Host "🌐 Your API is live at: $RailwayUrl" -ForegroundColor Cyan
    Write-Host "📱 Desktop app ready for distribution!" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🧪 Test your connection:" -ForegroundColor Yellow
    Write-Host "  1. Open: $RailwayUrl/swagger" -ForegroundColor White
    Write-Host "  2. Run: ./publish/desktop-railway/KitchenWise.Desktop.exe" -ForegroundColor White
    Write-Host "  3. Try logging in and generating a recipe" -ForegroundColor White
}

Write-Host ""
Write-Host "📖 For detailed configuration help, see: connect-frontend-backend.md" -ForegroundColor Cyan
