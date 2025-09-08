# Azure Deployment Guide for KitchenWise

## 🌐 Deploy API to Azure App Service

### Prerequisites
- Azure account (free tier available)
- Azure CLI installed

### Step 1: Create Azure Resources
```bash
# Login to Azure
az login

# Create resource group
az group create --name KitchenWise-RG --location "East US"

# Create App Service Plan (Free tier for testing)
az appservice plan create --name KitchenWise-Plan --resource-group KitchenWise-RG --sku F1 --is-linux

# Create Web App
az webapp create --resource-group KitchenWise-RG --plan KitchenWise-Plan --name kitchenwise-api-[YOUR-UNIQUE-ID] --runtime "DOTNETCORE:8.0"
```

### Step 2: Create Azure SQL Database
```bash
# Create SQL Server
az sql server create --name kitchenwise-server-[YOUR-ID] --resource-group KitchenWise-RG --location "East US" --admin-user kitchenadmin --admin-password "YourSecurePassword123!"

# Create SQL Database (Basic tier)
az sql db create --resource-group KitchenWise-RG --server kitchenwise-server-[YOUR-ID] --name KitchenWiseDB --service-objective Basic

# Configure firewall (allow Azure services)
az sql server firewall-rule create --resource-group KitchenWise-RG --server kitchenwise-server-[YOUR-ID] --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

### Step 3: Configure App Settings
```bash
# Set connection string
az webapp config connection-string set --resource-group KitchenWise-RG --name kitchenwise-api-[YOUR-ID] --connection-string-type SQLAzure --settings DefaultConnection="Server=tcp:kitchenwise-server-[YOUR-ID].database.windows.net,1433;Initial Catalog=KitchenWiseDB;Persist Security Info=False;User ID=kitchenadmin;Password=YourSecurePassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Set OpenAI API Key
az webapp config appsettings set --resource-group KitchenWise-RG --name kitchenwise-api-[YOUR-ID] --settings OpenAI__ApiKey="your-openai-api-key"

# Set Auth0 settings
az webapp config appsettings set --resource-group KitchenWise-RG --name kitchenwise-api-[YOUR-ID] --settings Auth0__Domain="your-domain.auth0.com" Auth0__ClientId="your-client-id" Auth0__ClientSecret="your-client-secret"
```

### Step 4: Deploy the API
```bash
# Zip the published API
cd publish/api
zip -r ../kitchenwise-api.zip .

# Deploy using Azure CLI
az webapp deployment source config-zip --resource-group KitchenWise-RG --name kitchenwise-api-[YOUR-ID] --src ../kitchenwise-api.zip
```

### Your API will be available at:
`https://kitchenwise-api-[YOUR-ID].azurewebsites.net`
