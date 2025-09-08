# Railway Deployment Guide (Easiest Option)

## 🚂 Deploy to Railway.app

Railway is perfect for quick public deployments with minimal configuration.

### Step 1: Prepare for Railway
1. Create account at https://railway.app
2. Install Railway CLI: `npm install -g @railway/cli`
3. Login: `railway login`

### Step 2: Create Railway Project
```bash
# In your project root
railway login
railway init
railway add --database postgresql
```

### Step 3: Configure Environment Variables
In Railway dashboard, add these environment variables:
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `OpenAI__ApiKey` = `your-openai-api-key`
- `Auth0__Domain` = `your-domain.auth0.com`
- `Auth0__ClientId` = `your-client-id`
- `Auth0__ClientSecret` = `your-client-secret`
- `ConnectionStrings__DefaultConnection` = (Railway provides PostgreSQL URL automatically)

### Step 4: Create Dockerfile for API
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KitchenWise.Api/KitchenWise.Api.csproj", "KitchenWise.Api/"]
RUN dotnet restore "KitchenWise.Api/KitchenWise.Api.csproj"
COPY . .
WORKDIR "/src/KitchenWise.Api"
RUN dotnet build "KitchenWise.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KitchenWise.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KitchenWise.Api.dll"]
```

### Step 5: Deploy
```bash
railway up
```

### Your API will be available at:
`https://your-project-name.up.railway.app`

### Cost: ~$5/month for hobby plan
