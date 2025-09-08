using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using System.Text.Json;
using KitchenWise.Api.Controllers;

namespace KitchenWise.Api.Services
{
    public class OpenAIServiceFixed : IOpenAIService
    {
        private readonly OpenAIClient _openAiClient;
        private readonly ILogger<OpenAIServiceFixed> _logger;

        public OpenAIServiceFixed(IConfiguration configuration, ILogger<OpenAIServiceFixed> logger)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured");
            }

            _openAiClient = new OpenAIClient(apiKey);
            _logger = logger;
        }

        public async Task<List<RecipeDto>> GenerateRecipesAsync(List<string> ingredients, string? cuisine = null, string? difficulty = null)
        {
            try
            {
                _logger.LogInformation($"Generating recipes for ingredients: {string.Join(", ", ingredients)}");

                var prompt = BuildRecipeGenerationPrompt(ingredients, cuisine, difficulty);
                
                var chatClient = _openAiClient.GetChatClient("gpt-4o-mini");
                var response = await chatClient.CompleteChatAsync(prompt);

                var content = response.Value.Content[0].Text;
                _logger.LogInformation($"OpenAI response received: {content.Length} characters");

                // Parse the JSON response
                var recipes = ParseRecipesFromResponse(content);
                
                _logger.LogInformation($"Successfully generated {recipes.Count} recipes");
                return recipes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recipes with OpenAI");
                
                // Return fallback recipes if OpenAI fails
                return GenerateFallbackRecipes(ingredients);
            }
        }

        public async Task<DetailedRecipeDto?> GetRecipeDetailsAsync(string recipeName, List<string> ingredients)
        {
            try
            {
                _logger.LogInformation($"Getting detailed recipe for: {recipeName}");

                var prompt = BuildDetailedRecipePrompt(recipeName, ingredients);
                
                var chatClient = _openAiClient.GetChatClient("gpt-4o-mini");
                var response = await chatClient.CompleteChatAsync(prompt);

                var content = response.Value.Content[0].Text;
                _logger.LogInformation($"OpenAI detailed response received: {content.Length} characters");

                // Parse the detailed recipe response
                var detailedRecipe = ParseDetailedRecipeFromResponse(content, recipeName);
                
                _logger.LogInformation($"Successfully generated detailed recipe for: {recipeName}");
                return detailedRecipe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting detailed recipe for {recipeName}");
                return null;
            }
        }

        private string BuildRecipeGenerationPrompt(List<string> ingredients, string? cuisine, string? difficulty)
        {
            var prompt = $@"You are a professional chef AI assistant. Generate 3-5 creative and practical recipes using ONLY the following ingredients (you can suggest common pantry staples like salt, pepper, oil if needed):

Available ingredients: {string.Join(", ", ingredients)}";

            if (!string.IsNullOrEmpty(cuisine))
            {
                prompt += $"\nCuisine preference: {cuisine}";
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                prompt += $"\nDifficulty level: {difficulty}";
            }

            prompt += @"

Please respond with a JSON array of recipes in this exact format:
[
  {
    ""name"": ""Recipe Name"",
    ""description"": ""Brief description"",
    ""cuisine"": ""Cuisine type"",
    ""difficultyLevel"": ""Easy|Medium|Hard"",
    ""prepTimeMinutes"": 15,
    ""cookTimeMinutes"": 25,
    ""servings"": 4,
    ""ingredients"": [""ingredient with quantity"", ""another ingredient""],
    ""instructions"": [""Step 1"", ""Step 2"", ""Step 3""],
    ""tags"": [""tag1"", ""tag2""]
  }
]

Make sure:
- Only use ingredients from the available list (plus basic seasonings)
- Instructions are clear and concise
- Times are realistic
- Recipes are practical and delicious
- Response is valid JSON only, no other text";

            return prompt;
        }

        private string BuildDetailedRecipePrompt(string recipeName, List<string> ingredients)
        {
            return $@"You are a professional chef AI assistant. Create a detailed version of the recipe ""{recipeName}"" using these ingredients: {string.Join(", ", ingredients)}.

Please respond with a JSON object in this exact format:
{{
  ""name"": ""{recipeName}"",
  ""description"": ""Detailed description"",
  ""cuisine"": ""Cuisine type"",
  ""difficultyLevel"": ""Easy|Medium|Hard"",
  ""prepTimeMinutes"": 15,
  ""cookTimeMinutes"": 25,
  ""servings"": 4,
  ""ingredients"": [""ingredient with quantity"", ""another ingredient""],
  ""instructions"": [""Detailed step 1"", ""Detailed step 2""],
  ""nutrition"": {{
    ""calories"": 350,
    ""protein"": 15,
    ""carbs"": 45,
    ""fat"": 12,
    ""fiber"": 6,
    ""sugar"": 8
  }},
  ""tips"": [""Cooking tip 1"", ""Cooking tip 2""],
  ""tags"": [""tag1"", ""tag2""]
}}

Make the instructions detailed and professional. Include realistic nutrition estimates. Response should be valid JSON only.";
        }

        private List<RecipeDto> ParseRecipesFromResponse(string content)
        {
            try
            {
                // Clean the response to extract JSON
                var jsonStart = content.IndexOf('[');
                var jsonEnd = content.LastIndexOf(']') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart);
                    var jsonDocument = JsonDocument.Parse(jsonContent);
                    
                    var recipes = new List<RecipeDto>();
                    
                    foreach (var element in jsonDocument.RootElement.EnumerateArray())
                    {
                        var recipe = new RecipeDto
                        {
                            Id = Guid.NewGuid(),
                            Name = SafeGetString(element, "name") ?? "Unknown Recipe",
                            Description = SafeGetString(element, "description") ?? "",
                            Cuisine = SafeGetString(element, "cuisine") ?? "International",
                            DifficultyLevel = SafeGetString(element, "difficultyLevel") ?? "Medium",
                            PrepTimeMinutes = SafeGetInt(element, "prepTimeMinutes") ?? 15,
                            CookTimeMinutes = SafeGetInt(element, "cookTimeMinutes") ?? 25,
                            Servings = SafeGetInt(element, "servings") ?? 4,
                            Ingredients = SafeGetStringArray(element, "ingredients"),
                            Instructions = SafeGetStringArray(element, "instructions"),
                            CreatedAt = DateTime.UtcNow,
                            Nutrition = new NutritionDto
                            {
                                Calories = 300,
                                Protein = 15,
                                Carbs = 40,
                                Fat = 10,
                                Fiber = 5
                            },
                            ImageUrl = null
                        };
                        
                        recipes.Add(recipe);
                    }
                    
                    return recipes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing recipes from OpenAI response");
            }
            
            return new List<RecipeDto>();
        }

        private DetailedRecipeDto? ParseDetailedRecipeFromResponse(string content, string recipeName)
        {
            try
            {
                // Clean the response to extract JSON
                var jsonStart = content.IndexOf('{');
                var jsonEnd = content.LastIndexOf('}') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart);
                    var jsonDocument = JsonDocument.Parse(jsonContent);
                    var element = jsonDocument.RootElement;
                    
                    var recipe = new DetailedRecipeDto
                    {
                        Id = Guid.NewGuid(),
                        Name = SafeGetString(element, "name") ?? recipeName,
                        Description = SafeGetString(element, "description") ?? "",
                        Cuisine = SafeGetString(element, "cuisine") ?? "International",
                        DifficultyLevel = SafeGetString(element, "difficultyLevel") ?? "Medium",
                        PrepTimeMinutes = SafeGetInt(element, "prepTimeMinutes") ?? 15,
                        CookTimeMinutes = SafeGetInt(element, "cookTimeMinutes") ?? 25,
                        Servings = SafeGetInt(element, "servings") ?? 4,
                        Ingredients = SafeGetStringArray(element, "ingredients"),
                        Instructions = SafeGetStringArray(element, "instructions"),
                        CreatedAt = DateTime.UtcNow,
                        ImageUrl = null,
                        DetailedInstructions = SafeGetStringArray(element, "instructions"),
                        Tips = SafeGetStringArray(element, "tips"),
                        Variations = new string[0],
                        Equipment = new string[0],
                        EstimatedCost = 0
                    };

                    // Parse nutrition if available
                    if (element.TryGetProperty("nutrition", out var nutritionElement))
                    {
                        recipe.Nutrition = new NutritionDto
                        {
                            Calories = nutritionElement.TryGetProperty("calories", out var cal) ? cal.GetDouble() : 300,
                            Protein = nutritionElement.TryGetProperty("protein", out var prot) ? prot.GetDouble() : 15,
                            Carbs = nutritionElement.TryGetProperty("carbs", out var carb) ? carb.GetDouble() : 40,
                            Fat = nutritionElement.TryGetProperty("fat", out var fat) ? fat.GetDouble() : 10,
                            Fiber = nutritionElement.TryGetProperty("fiber", out var fib) ? fib.GetDouble() : 5
                        };
                    }
                    else
                    {
                        recipe.Nutrition = new NutritionDto
                        {
                            Calories = 300,
                            Protein = 15,
                            Carbs = 40,
                            Fat = 10,
                            Fiber = 5
                        };
                    }
                    
                    return recipe;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing detailed recipe from OpenAI response");
            }
            
            return null;
        }

        private string? SafeGetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String 
                ? prop.GetString() 
                : null;
        }

        private int? SafeGetInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var intValue))
                    return intValue;
            }
            return null;
        }

        private string[] SafeGetStringArray(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                return prop.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? "")
                    .ToArray();
            }
            return new string[0];
        }

        private List<RecipeDto> GenerateFallbackRecipes(List<string> ingredients)
        {
            _logger.LogInformation("Generating fallback recipes due to OpenAI failure");
            
            return new List<RecipeDto>
            {
                new RecipeDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"Simple {ingredients.FirstOrDefault() ?? "Ingredient"} Recipe",
                    Description = "A simple recipe created from your available ingredients",
                    Cuisine = "International",
                    DifficultyLevel = "Easy",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 20,
                    Servings = 2,
                    Ingredients = ingredients.Take(5).Select(i => $"1 portion {i}").ToArray(),
                    Instructions = new[]
                    {
                        "Prepare your ingredients",
                        "Cook according to your preference",
                        "Season to taste",
                        "Serve and enjoy!"
                    },
                    CreatedAt = DateTime.UtcNow,
                    Nutrition = new NutritionDto
                    {
                        Calories = 250,
                        Protein = 10,
                        Carbs = 30,
                        Fat = 8,
                        Fiber = 3
                    },
                    ImageUrl = null
                }
            };
        }

        public async Task<string?> GenerateRecipeImageAsync(string recipeName, string recipeDescription, string cuisine)
        {
            try
            {
                _logger.LogInformation($"Generating image for recipe: {recipeName}");

                // Create a detailed prompt for DALL-E
                var imagePrompt = BuildImageGenerationPrompt(recipeName, recipeDescription, cuisine);
                
                var imageClient = _openAiClient.GetImageClient("dall-e-3");
                var options = new ImageGenerationOptions
                {
                    Quality = GeneratedImageQuality.Standard,
                    Size = GeneratedImageSize.W1024xH1024,
                    Style = GeneratedImageStyle.Natural,
                    ResponseFormat = GeneratedImageFormat.Uri
                };

                var response = await imageClient.GenerateImageAsync(imagePrompt, options);
                
                if (response.Value?.ImageUri != null)
                {
                    var imageUrl = response.Value.ImageUri.ToString();
                    _logger.LogInformation($"Successfully generated image for {recipeName}: {imageUrl}");
                    return imageUrl;
                }
                
                _logger.LogWarning($"No image URL returned for recipe: {recipeName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating image for recipe: {recipeName}");
                return null;
            }
        }

        private string BuildImageGenerationPrompt(string recipeName, string recipeDescription, string cuisine)
        {
            return $"A professional, appetizing photograph of {recipeName}, a {cuisine.ToLower()} dish. " +
                   $"{recipeDescription} " +
                   $"The image should show the finished dish beautifully plated, with vibrant colors and appealing presentation. " +
                   $"Professional food photography style, well-lit, restaurant quality presentation, overhead or 45-degree angle view.";
        }
    }
}

