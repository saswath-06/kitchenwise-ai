# Contributing to KitchenWise 🍳

Thank you for your interest in contributing to KitchenWise! This document provides guidelines and information for contributors.

## 🤝 How to Contribute

### Reporting Bugs
- Use the GitHub issue tracker
- Include detailed reproduction steps
- Provide system information (OS, .NET version, etc.)
- Include error messages and logs

### Suggesting Features
- Check existing issues first
- Provide clear use cases and benefits
- Consider implementation complexity
- Discuss in GitHub Discussions before coding

### Code Contributions
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Commit with clear messages
7. Push to your fork
8. Open a Pull Request

## 🛠️ Development Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Git
- OpenAI API key (for testing)
- Auth0 account (for testing)

### Local Development
```bash
# Clone your fork
git clone https://github.com/yourusername/KitchenWise.git
cd KitchenWise

# Set up API
cd KitchenWise.Api
dotnet restore
dotnet ef database update

# Set up Desktop App
cd ../KitchenWise.Desktop
dotnet restore
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test KitchenWise.Api.Tests
```

## 📝 Coding Standards

### C# Style Guidelines
- Follow Microsoft C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Use async/await for I/O operations
- Implement proper error handling

### Code Formatting
- Use 4 spaces for indentation
- Use PascalCase for public members
- Use camelCase for private members
- Use meaningful names for variables and methods

### Example Code Style
```csharp
/// <summary>
/// Generates AI-powered recipes based on available ingredients
/// </summary>
/// <param name="ingredients">List of available ingredients</param>
/// <param name="cuisine">Preferred cuisine type</param>
/// <returns>List of generated recipes</returns>
public async Task<List<RecipeDto>> GenerateRecipesAsync(
    List<string> ingredients, 
    string? cuisine = null)
{
    try
    {
        // Implementation here
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating recipes");
        throw;
    }
}
```

## 🧪 Testing Guidelines

### Unit Tests
- Write tests for all new functionality
- Aim for >80% code coverage
- Use descriptive test names
- Test both success and failure scenarios

### Integration Tests
- Test API endpoints
- Test database operations
- Test external service integrations (mocked)

### Example Test
```csharp
[Test]
public async Task GenerateRecipesAsync_WithValidIngredients_ReturnsRecipes()
{
    // Arrange
    var ingredients = new List<string> { "chicken", "rice" };
    var service = new OpenAIService(_configuration, _logger);

    // Act
    var result = await service.GenerateRecipesAsync(ingredients);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Count, Is.GreaterThan(0));
}
```

## 📋 Pull Request Guidelines

### Before Submitting
- [ ] Code follows style guidelines
- [ ] All tests pass
- [ ] New functionality has tests
- [ ] Documentation is updated
- [ ] No breaking changes (or clearly documented)

### PR Description Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No breaking changes
```

## 🏗️ Architecture Guidelines

### API Design
- Follow RESTful principles
- Use appropriate HTTP status codes
- Implement proper error handling
- Add input validation
- Document all endpoints

### Desktop App
- Follow MVVM pattern
- Use dependency injection
- Implement proper error handling
- Maintain responsive UI
- Follow WPF best practices

### Database
- Use Entity Framework migrations
- Follow naming conventions
- Add proper indexes
- Implement soft deletes where appropriate

## 🎯 Areas for Contribution

### High Priority
- 🐛 Bug fixes
- 📚 Documentation improvements
- 🧪 Test coverage improvements
- 🔧 Performance optimizations

### Medium Priority
- 🎨 UI/UX improvements
- 🔒 Security enhancements
- 📱 Mobile app development
- 🌍 Internationalization

### Low Priority
- 🎮 Gamification features
- 📊 Advanced analytics
- 🔌 Plugin system
- 🎓 Tutorial system

## 🚫 What Not to Contribute

- Code that breaks existing functionality
- Features without tests
- Code that doesn't follow style guidelines
- Dependencies on paid services without alternatives
- Code with security vulnerabilities

## 📞 Getting Help

### Communication Channels
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and ideas
- **Pull Request Comments**: Code review discussions

### Response Times
- Bug reports: 1-2 business days
- Feature requests: 1 week
- Pull requests: 2-3 business days
- General questions: 3-5 business days

## 🏆 Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- GitHub contributors page
- Project documentation

## 📄 License

By contributing to KitchenWise, you agree that your contributions will be licensed under the MIT License.

## 🙏 Thank You

Thank you for contributing to KitchenWise! Your efforts help make smart kitchen management accessible to everyone.

---

*Happy coding! 🍳✨*
