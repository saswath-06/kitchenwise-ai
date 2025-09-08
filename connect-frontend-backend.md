# 🔗 Connecting Desktop Frontend to Railway Backend

## Step 1: Deploy API to Railway

### Install Railway CLI
```bash
npm install -g @railway/cli
```

### Deploy Your API
```bash
# Login to Railway
railway login

# Initialize project in your KitchenWise folder
railway init

# Add PostgreSQL database
railway add --database postgresql

# Create Dockerfile for the API (Railway will auto-detect it)
```

### Create Dockerfile for Railway
Create `Dockerfile` in your project root:

```dockerfile
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
```

### Deploy to Railway
```bash
railway up
```

After deployment, Railway will give you a URL like:
`https://your-project-name.up.railway.app`

## Step 2: Configure Environment Variables in Railway

In Railway dashboard, add these environment variables:
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ASPNETCORE_URLS` = `http://0.0.0.0:8080`
- `OpenAI__ApiKey` = `your-openai-api-key`
- `Auth0__Domain` = `your-domain.auth0.com`
- `Auth0__ClientId` = `your-client-id`
- `Auth0__ClientSecret` = `your-client-secret`
- `Auth0__Audience` = `https://kitchenwise-api`

Railway automatically provides the PostgreSQL connection string.

## Step 3: Update Desktop App Configuration

### Method A: Update appsettings.json (Simple)
Edit the `appsettings.json` file next to your `KitchenWise.Desktop.exe`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-project-name.up.railway.app"
  },
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "ClientId": "your-desktop-client-id",
    "RedirectUri": "http://localhost:8080/callback",
    "PostLogoutRedirectUri": "http://localhost:8080"
  }
}
```

### Method B: Rebuild with Production Config (Recommended)
1. Update the config file in your source
2. Rebuild the desktop app:

```bash
# Update appsettings.json in KitchenWise.Desktop folder first
dotnet publish KitchenWise.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/desktop-railway
```

## Step 4: Test the Connection

### Verify API is Running
Open browser and go to: `https://your-project-name.up.railway.app/swagger`

You should see the Swagger API documentation.

### Test Desktop App
1. Run `KitchenWise.Desktop.exe`
2. Try to login - this will test Auth0 connection
3. Try to add pantry items - this will test database connection
4. Try to generate recipes - this will test OpenAI integration

## Step 5: Troubleshooting Connection Issues

### Common Issues and Solutions:

#### 1. CORS Errors
Add CORS configuration to your API's `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktopApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Use CORS
app.UseCors("AllowDesktopApp");
```

#### 2. HTTPS Certificate Issues
In desktop app's `ApiService.cs`, add this to handle SSL:

```csharp
private readonly HttpClient _httpClient;

public ApiService(string baseUrl)
{
    var handler = new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    
    _httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(60)
    };
}
```

#### 3. Authentication Issues
Ensure Auth0 application settings match:
- **Desktop App Type**: Native Application
- **Allowed Callback URLs**: `http://localhost:8080/callback`
- **Allowed Logout URLs**: `http://localhost:8080`

#### 4. Database Connection Issues
Check Railway logs:
```bash
railway logs
```

## Step 6: Production Deployment Script

Here's a complete script to deploy and connect everything:

```powershell
# deploy-to-railway.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$RailwayUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$Auth0Domain,
    
    [Parameter(Mandatory=$true)]
    [string]$Auth0DesktopClientId
)

Write-Host "🚂 Connecting KitchenWise Desktop to Railway Backend" -ForegroundColor Green

# Update desktop app configuration
$desktopConfig = @{
    "ApiSettings" = @{
        "BaseUrl" = $RailwayUrl
    }
    "Auth0" = @{
        "Domain" = $Auth0Domain
        "ClientId" = $Auth0DesktopClientId
        "RedirectUri" = "http://localhost:8080/callback"
        "PostLogoutRedirectUri" = "http://localhost:8080"
    }
}

$configJson = $desktopConfig | ConvertTo-Json -Depth 3
$configJson | Out-File -FilePath "KitchenWise.Desktop/appsettings.json" -Encoding UTF8

Write-Host "✅ Configuration updated" -ForegroundColor Green

# Rebuild desktop app with new configuration
Write-Host "🔨 Rebuilding desktop app..." -ForegroundColor Cyan
dotnet publish KitchenWise.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/desktop-connected

Write-Host "✅ Desktop app rebuilt and ready!" -ForegroundColor Green
Write-Host "📱 Executable location: ./publish/desktop-connected/KitchenWise.Desktop.exe" -ForegroundColor Yellow
Write-Host "🌐 Connected to: $RailwayUrl" -ForegroundColor Yellow
```

## Usage Example:

```bash
# After deploying to Railway and getting your URL
.\deploy-to-railway.ps1 -RailwayUrl "https://kitchenwise-production-1234.up.railway.app" -Auth0Domain "your-domain.auth0.com" -Auth0DesktopClientId "your-desktop-client-id"
```

## Final Checklist:
- [ ] API deployed to Railway and accessible
- [ ] Environment variables configured in Railway
- [ ] Desktop app configuration updated with Railway URL
- [ ] Auth0 configured for both API and Desktop
- [ ] CORS configured in API
- [ ] SSL/HTTPS working
- [ ] End-to-end testing completed

Your desktop app will now connect to your Railway-hosted backend! 🎉
