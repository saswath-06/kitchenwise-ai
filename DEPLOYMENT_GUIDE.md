# 🚀 KitchenWise Deployment Guide

## Overview
KitchenWise is a smart kitchen management application with AI-powered recipe generation and DALL-E image generation capabilities.

## Architecture
- **Desktop Application**: WPF .NET 8 application for Windows
- **API Backend**: ASP.NET Core 8 Web API
- **Database**: Azure SQL Database (or SQL Server)
- **AI Services**: OpenAI GPT-4 and DALL-E 3

## 📦 Published Files Location
- **Desktop App**: `KitchenWise.Desktop/publish/win-x64/KitchenWise.Desktop.exe`
- **API**: `publish/api/` (complete folder)

---

## 🖥️ Desktop Application Deployment

### Option 1: Standalone Executable (Recommended)
✅ **Already published** as self-contained executable

**Location**: `KitchenWise.Desktop/publish/win-x64/KitchenWise.Desktop.exe`

**Features**:
- Single executable file (~150MB)
- No .NET runtime required on target machine
- All dependencies included
- Ready to run on any Windows 10+ machine

**Distribution**:
1. Copy `KitchenWise.Desktop.exe` to target machine
2. Run directly - no installation required
3. Optionally create desktop shortcut

### System Requirements
- **OS**: Windows 10 version 1903 or later
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 200MB free space
- **Network**: Internet connection for API communication

---

## 🌐 API Deployment Options

### Option 1: Local Development Server
```bash
cd publish/api
dotnet KitchenWise.Api.dll
```
**Access**: http://localhost:5000

### Option 2: IIS Deployment (Windows Server)
1. Copy `publish/api/` folder to IIS wwwroot
2. Create new IIS application
3. Configure application pool for .NET 8
4. Update `appsettings.json` with production settings

### Option 3: Azure App Service (Recommended for Production)
1. Create Azure App Service (.NET 8)
2. Upload `publish/api/` contents via FTP/Git
3. Configure environment variables
4. Set up Azure SQL Database

### Option 4: Docker Deployment
Create `Dockerfile` in API root:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/api/ .
EXPOSE 80
ENTRYPOINT ["dotnet", "KitchenWise.Api.dll"]
```

---

## ⚙️ Configuration Setup

### 1. API Configuration (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_DATABASE_CONNECTION_STRING"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  },
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "ClientId": "YOUR_AUTH0_CLIENT_ID",
    "ClientSecret": "YOUR_AUTH0_CLIENT_SECRET"
  }
}
```

### 2. Desktop App Configuration (`appsettings.json`)
```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-domain.com"
  },
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "ClientId": "YOUR_AUTH0_CLIENT_ID"
  }
}
```

---

## 🗄️ Database Setup

### Azure SQL Database (Recommended)
1. Create Azure SQL Database
2. Run migrations: `dotnet ef database update`
3. Update connection string in API configuration

### Local SQL Server
1. Install SQL Server Express
2. Update connection string to local instance
3. Run migrations

---

## 🔐 Required API Keys & Services

### 1. OpenAI API Key
- Sign up at https://platform.openai.com/
- Generate API key
- Add to API configuration
- **Cost**: Pay-per-use (GPT-4: ~$0.03/1K tokens, DALL-E 3: ~$0.04/image)

### 2. Auth0 Setup
- Create Auth0 account
- Configure application settings
- Set up callback URLs
- Add credentials to both API and Desktop app

### 3. Azure SQL Database (if using Azure)
- Create Azure account
- Set up SQL Database
- Configure firewall rules

---

## 🚀 Quick Start Deployment

### For Testing/Development:
1. **Run API locally**:
   ```bash
   cd publish/api
   dotnet KitchenWise.Api.dll
   ```

2. **Update Desktop app config**:
   - Edit `appsettings.json` next to `KitchenWise.Desktop.exe`
   - Set API URL to `http://localhost:5000`

3. **Run Desktop app**:
   - Double-click `KitchenWise.Desktop.exe`

### For Production:
1. **Deploy API** to Azure App Service or IIS
2. **Configure database** and API keys
3. **Update Desktop app** with production API URL
4. **Distribute** `KitchenWise.Desktop.exe` to users

---

## 📋 Deployment Checklist

### Pre-Deployment:
- [ ] OpenAI API key obtained and configured
- [ ] Auth0 application set up
- [ ] Database server accessible
- [ ] API configuration updated for production
- [ ] Desktop app configuration updated with production API URL

### API Deployment:
- [ ] API published and deployed
- [ ] Database migrations run
- [ ] HTTPS configured
- [ ] Environment variables set
- [ ] API accessible and responding

### Desktop Deployment:
- [ ] Desktop app published as self-contained
- [ ] Configuration file updated
- [ ] Tested on target machines
- [ ] Distribution method chosen

### Post-Deployment:
- [ ] End-to-end testing completed
- [ ] User authentication working
- [ ] Recipe generation functional
- [ ] Image generation working
- [ ] Pantry management operational

---

## 🔧 Troubleshooting

### Common Issues:
1. **API Connection Failed**: Check API URL and network connectivity
2. **Authentication Error**: Verify Auth0 configuration
3. **Database Connection**: Confirm connection string and database accessibility
4. **OpenAI API Error**: Check API key and usage limits
5. **Image Generation Failed**: Verify DALL-E API access and billing

### Logs:
- **API Logs**: Check application logs in hosting environment
- **Desktop Logs**: Console output visible during development

---

## 📞 Support
- Check configuration files for correct URLs and keys
- Ensure all required services are running
- Verify network connectivity between components
- Test API endpoints directly using browser or Postman

---

## 🎉 Features Available After Deployment
- ✅ User authentication and profile management
- ✅ Pantry inventory tracking with expiration dates
- ✅ AI-powered recipe generation based on available ingredients
- ✅ DALL-E 3 recipe image generation
- ✅ Recipe favorites and management
- ✅ Ingredient consumption tracking
- ✅ Professional UI with modern design

Your KitchenWise application is now ready for deployment! 🚀
