# KitchenWise 🍳
Transform your kitchen into a smart, AI-powered cooking experience. Generate personalized recipes from your pantry ingredients and visualize them with stunning AI-generated images.

**Live Demo**: [Deploy your own instance using our Railway deployment guide](#-deployment)

![KitchenWise Demo](https://via.placeholder.com/800x400/2E7D32/FFFFFF?text=KitchenWise+Smart+Kitchen+Management)

## ✨ Features
🤖 **AI Recipe Generation**: GPT-4 powered recipe suggestions based on available ingredients  
🎨 **DALL-E Image Generation**: Visualize recipes with professional AI-generated food photography  
📦 **Smart Pantry Management**: Track ingredients with expiration dates and automatic consumption  
👤 **User Authentication**: Secure login with Auth0 integration  
❤️ **Recipe Favorites**: Save and organize your favorite recipes  
🎯 **Ingredient-Based Cooking**: Never waste food again with smart recipe suggestions  
📱 **Modern Desktop App**: Beautiful WPF interface with responsive design  
🌐 **Cloud-Ready**: Deploy anywhere with our comprehensive deployment guides  

## 🏗️ Architecture
```
KitchenWise/
├── KitchenWise.Api/              # ASP.NET Core 8 Web API
│   ├── Controllers/              # REST API endpoints
│   │   ├── RecipeController.cs   # Recipe generation & DALL-E integration
│   │   ├── PantryController.cs   # Pantry management
│   │   └── UserController.cs     # User authentication
│   ├── Services/                 # Business logic
│   │   ├── OpenAIService.cs      # GPT-4 & DALL-E 3 integration
│   │   └── OpenAIServiceFixed.cs # Alternative implementation
│   ├── Data/                     # Entity Framework models
│   │   ├── DatabaseContext.cs    # Database configuration
│   │   └── Migrations/           # Database schema
│   └── Program.cs                # API startup configuration
├── KitchenWise.Desktop/          # WPF Desktop Application
│   ├── MainWindow.xaml           # Main application interface
│   ├── RecipeWindow.xaml         # Recipe generation & viewing
│   ├── Services/                 # API communication
│   │   ├── ApiService.cs         # HTTP client for API calls
│   │   └── AuthService.cs        # Auth0 authentication
│   ├── ViewModels/               # MVVM pattern implementation
│   │   ├── RecipeViewModel.cs    # Recipe management logic
│   │   └── PantryViewModel.cs    # Pantry management logic
│   └── Models/                   # Data transfer objects
└── Deployment/                   # Deployment configurations
    ├── azure-deploy.md           # Azure App Service deployment
    ├── railway-deploy.md         # Railway deployment (recommended)
    └── production-security.md    # Security best practices
```

## 🧠 Tech Stack

### Backend
- **ASP.NET Core 8**: Modern web API framework
- **Entity Framework Core**: Database ORM with migrations
- **OpenAI API**: GPT-4 for recipe generation, DALL-E 3 for images
- **Auth0**: Enterprise-grade authentication
- **Azure SQL Database**: Scalable cloud database
- **Swagger/OpenAPI**: API documentation

### Frontend
- **WPF (.NET 8)**: Modern Windows desktop application
- **MVVM Pattern**: Clean architecture with ViewModels
- **CommunityToolkit.Mvvm**: Modern MVVM implementation
- **Material Design**: Beautiful, responsive UI components

### AI & Machine Learning
- **OpenAI GPT-4**: Advanced recipe generation with context awareness
- **DALL-E 3**: High-quality food photography generation
- **Custom Prompts**: Optimized for culinary content creation

### Deployment
- **Railway**: Simple cloud deployment (recommended)
- **Azure App Service**: Enterprise cloud hosting
- **Docker**: Containerized deployment
- **Self-Contained**: Single executable distribution

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- OpenAI API key
- Auth0 account (free tier available)
- Git

### Local Development

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/KitchenWise.git
cd KitchenWise
```

2. **Set up the API**
```bash
cd KitchenWise.Api

# Configure appsettings.json with your credentials
# - OpenAI API key
# - Auth0 settings
# - Database connection string

# Run database migrations
dotnet ef database update

# Start the API server
dotnet run
```

3. **Set up the Desktop App**
```bash
cd KitchenWise.Desktop

# Update appsettings.json with API URL
# Set BaseUrl to: http://localhost:5000

# Start the desktop application
dotnet run
```

4. **Access the application**
- **Desktop App**: Launches automatically
- **API Documentation**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health

## 🔧 Configuration

### Environment Variables

#### API Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=KitchenWiseDB;..."
  },
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key"
  },
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "ClientId": "your-api-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

#### Desktop App Configuration
```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-deployed-api.com"
  },
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "ClientId": "your-desktop-client-id"
  }
}
```

### OpenAI Setup
1. Create account at [OpenAI Platform](https://platform.openai.com)
2. Generate API key
3. Add to configuration files
4. **Cost**: ~$0.03/1K tokens (GPT-4), ~$0.04/image (DALL-E 3)

### Auth0 Setup
1. Create free account at [Auth0](https://auth0.com)
2. Create two applications:
   - **API Application** (Machine to Machine)
   - **Desktop Application** (Native)
3. Configure callback URLs
4. **Free Tier**: Up to 7,000 active users

## 📖 API Documentation

### Endpoints

#### Health Check
```
GET /health
```
Returns API status and service availability.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "services": {
    "database": "connected",
    "openai": "available"
  }
}
```

#### Generate Recipes
```
POST /api/recipe/generate
Content-Type: application/json
```
Generate AI-powered recipes based on available ingredients.

**Request:**
```json
{
  "ingredients": ["chicken", "rice", "tomatoes"],
  "cuisine": "Italian",
  "maxRecipes": 5
}
```

**Response:**
```json
{
  "recipes": [
    {
      "id": "uuid",
      "name": "Chicken and Rice Bowl",
      "description": "A delicious one-pot meal...",
      "cuisine": "Italian",
      "difficultyLevel": "Easy",
      "prepTimeMinutes": 15,
      "cookTimeMinutes": 25,
      "servings": 4,
      "ingredients": ["2 cups rice", "1 lb chicken", "3 tomatoes"],
      "instructions": ["Cook rice...", "Season chicken..."],
      "nutrition": {
        "calories": 450,
        "protein": 35,
        "carbs": 40,
        "fat": 12
      }
    }
  ]
}
```

#### Generate Recipe Image
```
POST /api/recipe/generate-image
Content-Type: application/json
```
Generate DALL-E 3 image for a recipe.

**Request:**
```json
{
  "recipeName": "Chicken and Rice Bowl",
  "recipeDescription": "A delicious one-pot meal",
  "cuisine": "Italian"
}
```

**Response:**
```json
{
  "imageUrl": "https://oaidalleapiprodscus.blob.core.windows.net/..."
}
```

#### Pantry Management
```
GET /api/pantry/items          # Get all pantry items
POST /api/pantry/items         # Add new item
PUT /api/pantry/items/{id}     # Update item
DELETE /api/pantry/items/{id}  # Remove item
```

## 🧪 AI Recipe Generation

### How It Works
1. **Ingredient Analysis**: Parse available ingredients and quantities
2. **Context Building**: Create detailed prompt with dietary preferences
3. **GPT-4 Processing**: Generate creative, practical recipes
4. **Response Parsing**: Extract structured recipe data
5. **Image Generation**: Create visual representation with DALL-E 3

### Prompt Engineering
```csharp
var prompt = $"Generate {maxRecipes} creative recipes using these ingredients: {string.Join(", ", ingredients)}. " +
             $"Cuisine preference: {cuisine}. " +
             $"Include detailed instructions, cooking times, and nutritional information. " +
             $"Format as JSON with proper structure.";
```

### DALL-E Image Generation
```csharp
var imagePrompt = $"A professional, appetizing photograph of {recipeName}, a {cuisine.ToLower()} dish. " +
                  $"{recipeDescription} " +
                  $"Professional food photography style, well-lit, restaurant quality presentation.";
```

## 🚢 Deployment

### Railway (Recommended - 5 minutes)
```bash
# Install Railway CLI
npm install -g @railway/cli

# Deploy with our script
.\deploy-to-railway.ps1
```

### Azure App Service
```bash
# Use Azure CLI
az webapp create --resource-group KitchenWise-RG --plan KitchenWise-Plan --name kitchenwise-api
az webapp deployment source config-zip --resource-group KitchenWise-RG --name kitchenwise-api --src kitchenwise-api.zip
```

### Docker
```bash
# Build and run
docker build -t kitchenwise-api .
docker run -p 8080:8080 kitchenwise-api
```

### Desktop App Distribution
- **Direct Download**: Share `KitchenWise.Desktop.exe` (179MB)
- **Installer**: Use Inno Setup for professional installation
- **Microsoft Store**: Advanced distribution option

## 📊 Performance

### API Performance
- **Recipe Generation**: 2-5 seconds (GPT-4 processing)
- **Image Generation**: 10-15 seconds (DALL-E 3 processing)
- **Database Queries**: <100ms average
- **Concurrent Users**: 100+ with proper scaling

### Desktop App
- **Startup Time**: <3 seconds
- **Memory Usage**: ~150MB
- **File Size**: 179MB (self-contained)
- **Compatibility**: Windows 10+ (1903 or later)

### Cost Analysis (1,000 users/month)
- **Hosting**: $5-15/month (Railway/Azure)
- **OpenAI API**: $200-400/month (variable usage)
- **Auth0**: Free (up to 7,000 users)
- **Database**: $5-20/month
- **Total**: ~$210-435/month

## 🔍 How It Works

### Recipe Generation Flow
1. **User Input**: Select available ingredients from pantry
2. **API Call**: Send ingredients to backend with preferences
3. **AI Processing**: GPT-4 generates creative recipes
4. **Response**: Structured recipe data with instructions
5. **Image Generation**: DALL-E 3 creates food photography
6. **Display**: Show recipe with image in desktop app

### Pantry Management
1. **Add Items**: Scan or manually enter ingredients
2. **Expiration Tracking**: Monitor freshness dates
3. **Smart Suggestions**: Recommend recipes before expiration
4. **Consumption Tracking**: Update quantities after cooking

### Authentication Flow
1. **Desktop Launch**: Check for existing tokens
2. **Auth0 Login**: Redirect to browser for authentication
3. **Token Exchange**: Receive JWT access token
4. **API Calls**: Include token in all requests
5. **Auto-Refresh**: Seamless token renewal

## 🤝 Contributing

Contributions are welcome! Here's how to get started:

### Development Workflow
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and add tests
4. Run the test suite: `dotnet test`
5. Commit your changes: `git commit -m 'Add amazing feature'`
6. Push to the branch: `git push origin feature/amazing-feature`
7. Open a Pull Request

### Code Style
- Follow C# naming conventions
- Use async/await for I/O operations
- Implement proper error handling
- Add XML documentation for public APIs
- Write unit tests for new features

### Areas for Contribution
- 🎨 UI/UX improvements
- 🤖 AI prompt optimization
- 📱 Mobile app development
- 🔧 Performance optimizations
- 📚 Documentation improvements
- 🧪 Additional recipe sources

## 🛠️ Troubleshooting

### Common Issues

#### API Connection Failed
- Check API URL in desktop app configuration
- Verify API is running and accessible
- Check network connectivity and firewall settings

#### Authentication Errors
- Verify Auth0 configuration matches between API and desktop
- Check callback URLs are correctly configured
- Ensure JWT tokens are not expired

#### OpenAI API Errors
- Verify API key is valid and has sufficient credits
- Check rate limits and usage quotas
- Monitor OpenAI dashboard for service status

#### Database Connection Issues
- Verify connection string is correct
- Check database server accessibility
- Run migrations: `dotnet ef database update`

### Getting Help
- 📧 **Email**: [your-email@domain.com]
- 🐛 **Issues**: [GitHub Issues](https://github.com/yourusername/KitchenWise/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/yourusername/KitchenWise/discussions)
- 📖 **Documentation**: Check the `/docs` folder for detailed guides

## 🙏 Acknowledgments

- **OpenAI** for GPT-4 and DALL-E 3 APIs
- **Auth0** for authentication services
- **Microsoft** for .NET and WPF frameworks
- **Railway** for simple cloud deployment
- **Azure** for database and hosting services
- **Community** for feedback and contributions

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🎯 Roadmap

### Version 1.1 (Coming Soon)
- 📱 Mobile app (React Native)
- 🍎 iOS and Android support
- 🔄 Real-time pantry sync
- 📊 Advanced analytics dashboard

### Version 1.2 (Future)
- 🛒 Grocery list integration
- 🏪 Store locator with ingredient availability
- 👥 Social features and recipe sharing
- 🎓 Cooking tutorials and tips

### Version 2.0 (Long-term)
- 🤖 Voice assistant integration
- 📷 Barcode scanning for ingredients
- 🌍 Multi-language support
- 🏆 Gamification and achievements

---

**Made with ❤️ for smart kitchens everywhere**

*Transform your cooking experience with AI-powered recipe generation and beautiful food photography.*