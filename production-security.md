# Production Security & Configuration Guide

## 🔐 Security Checklist for Public Deployment

### API Security
- [ ] **HTTPS Only**: Ensure SSL/TLS certificates are configured
- [ ] **CORS Configuration**: Restrict to your domain only
- [ ] **API Rate Limiting**: Prevent abuse
- [ ] **Input Validation**: Sanitize all user inputs
- [ ] **Error Handling**: Don't expose internal errors to users
- [ ] **Authentication**: Secure JWT token handling
- [ ] **Database**: Use connection pooling and parameterized queries

### Environment Variables (Never hardcode!)
```bash
# API Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# Database
ConnectionStrings__DefaultConnection="your-secure-connection-string"

# OpenAI
OpenAI__ApiKey="your-openai-api-key"

# Auth0
Auth0__Domain="your-domain.auth0.com"
Auth0__ClientId="your-api-client-id"
Auth0__ClientSecret="your-secure-client-secret"
Auth0__Audience="https://kitchenwise-api"

# Optional: Application Insights for monitoring
APPINSIGHTS_INSTRUMENTATIONKEY="your-insights-key"
```

### CORS Configuration (Add to Program.cs)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("https://yourdomain.com") // Your website domain
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Use in production
app.UseCors("ProductionPolicy");
```

### Rate Limiting (Add to Program.cs)
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

app.UseRateLimiter();
```

## 🔧 Production appsettings.json Template
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "KitchenWise": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=KitchenWiseDB;Persist Security Info=False;User ID=your-user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Organization": "your-org-id",
    "MaxTokens": 4000,
    "Temperature": 0.7
  },
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "Audience": "https://kitchenwise-api",
    "ClientId": "your-api-client-id",
    "ClientSecret": "your-client-secret"
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 10485760
    }
  }
}
```

## 📊 Monitoring & Analytics
- **Application Insights**: Monitor API performance
- **Log Analytics**: Track errors and usage
- **Health Checks**: API availability monitoring
- **Usage Metrics**: Track OpenAI API costs
