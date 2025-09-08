using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KitchenWise.Api.Services;

namespace KitchenWise.Api.Controllers
{
    /// <summary>
    /// Recipe generation API controller using OpenAI
    /// Handles AI-powered recipe suggestions and image generation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RecipeController : ControllerBase
    {
        private readonly ILogger<RecipeController> _logger;
        private readonly IOpenAIService _openAIService;

        // Mock recipe data for testing - will be replaced with OpenAI integration
        private static readonly List<RecipeDto> _mockRecipes = new()
        {
            new RecipeDto
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Tomato Rice Bowl",
                Description = "A simple and delicious rice bowl with fresh tomatoes and herbs",
                Cuisine = "Mediterranean",
                DifficultyLevel = "Easy",
                PrepTimeMinutes = 15,
                CookTimeMinutes = 20,
                Servings = 2,
                Ingredients = new[]
                {
                    "2 cups Basmati Rice",
                    "3 piece Tomatoes",
                    "1 tbsp Olive Oil",
                    "Salt to taste",
                    "Fresh herbs (optional)"
                },
                Instructions = new[]
                {
                    "Cook rice according to package instructions",
                    "Dice tomatoes into small pieces",
                    "Heat olive oil in a pan",
                    "Saut� tomatoes until soft",
                    "Season with salt and herbs",
                    "Serve over rice"
                },
                Nutrition = new NutritionDto
                {
                    Calories = 320,
                    Protein = 6.2,
                    Carbs = 65.4,
                    Fat = 4.8,
                    Fiber = 2.1
                },
                ImageUrl = null,
                CreatedAt = DateTime.UtcNow
            },
            new RecipeDto
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Cheesy Ground Beef Skillet",
                Description = "A hearty one-pan meal with ground beef and melted cheese",
                Cuisine = "American",
                DifficultyLevel = "Medium",
                PrepTimeMinutes = 10,
                CookTimeMinutes = 25,
                Servings = 4,
                Ingredients = new[]
                {
                    "2 lb Ground Beef",
                    "1 lb Cheddar Cheese",
                    "1 tbsp Olive Oil",
                    "Onion powder",
                    "Salt and pepper"
                },
                Instructions = new[]
                {
                    "Heat olive oil in a large skillet",
                    "Brown ground beef, breaking it apart",
                    "Season with salt, pepper, and onion powder",
                    "Top with cheese and cover until melted",
                    "Serve hot"
                },
                Nutrition = new NutritionDto
                {
                    Calories = 485,
                    Protein = 35.8,
                    Carbs = 3.2,
                    Fat = 36.4,
                    Fiber = 0.1
                },
                ImageUrl = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        public RecipeController(ILogger<RecipeController> logger, IOpenAIService openAIService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
        }

        /// <summary>
        /// Generate recipe suggestions based on available pantry items
        /// </summary>
        /// <param name="request">Recipe generation request</param>
        /// <returns>List of recipe suggestions</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> GenerateRecipes([FromBody] GenerateRecipesRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating recipes for user: {request.UserId} with {request.AvailableIngredients?.Length ?? 0} ingredients");

                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                if (request.AvailableIngredients == null || request.AvailableIngredients.Length == 0)
                {
                    return BadRequest(new { error = "At least one ingredient is required" });
                }

                // ✅ REAL OPENAI GPT-4 INTEGRATION
                _logger.LogInformation("Calling OpenAI GPT-4 for recipe generation...");
                
                var ingredients = request.AvailableIngredients.ToList();
                var suggestedRecipes = await _openAIService.GenerateRecipesAsync(
                    ingredients, 
                    request.CuisineFilter, 
                    request.DifficultyFilter
                );

                // Add generation metadata - the OpenAI service already returns proper DTOs
                var response = suggestedRecipes.Select(recipe => new RecipeDto
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Description = recipe.Description,
                    Cuisine = recipe.Cuisine,
                    DifficultyLevel = recipe.DifficultyLevel,
                    PrepTimeMinutes = recipe.PrepTimeMinutes,
                    CookTimeMinutes = recipe.CookTimeMinutes,
                    Servings = recipe.Servings,
                    Ingredients = recipe.Ingredients,
                    Instructions = recipe.Instructions,
                    // Note: OpenAI service RecipeDto doesn't have Nutrition/ImageUrl, will add later
                    CreatedAt = DateTime.UtcNow,
                    GeneratedWith = request.AvailableIngredients,
                    CuisineRequested = request.CuisineFilter
                }).ToList();

                _logger.LogInformation($"Generated {response.Count} recipe suggestions");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recipes");
                return StatusCode(500, new { error = "Failed to generate recipes", message = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed recipe with full instructions and nutrition
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="request">Recipe detail request</param>
        /// <returns>Detailed recipe information</returns>
        [HttpPost("{recipeId}/details")]
        public async Task<ActionResult<DetailedRecipeDto>> GetRecipeDetails(Guid recipeId, [FromBody] RecipeDetailRequest request)
        {
            try
            {
                _logger.LogInformation($"Getting detailed recipe: {recipeId}");

                // ✅ REAL OPENAI GPT-4 INTEGRATION FOR DETAILED RECIPE
                _logger.LogInformation("Calling OpenAI GPT-4 for detailed recipe generation...");
                
                var ingredients = request.AvailableIngredients?.ToList() ?? new List<string>();
                var detailedRecipe = await _openAIService.GetRecipeDetailsAsync(request.RecipeName ?? "Custom Recipe", ingredients);
                
                if (detailedRecipe == null)
                {
                    return NotFound(new { error = "Could not generate detailed recipe", recipeName = request.RecipeName });
                }

                // Set the requested recipe ID
                detailedRecipe.Id = recipeId;

                _logger.LogInformation($"Generated detailed recipe: {detailedRecipe.Name}");
                return Ok(detailedRecipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recipe details for {recipeId}");
                return StatusCode(500, new { error = "Failed to get recipe details", message = ex.Message });
            }
        }

        /// <summary>
        /// Generate recipe image using DALL-E
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="request">Image generation request</param>
        /// <returns>Recipe with generated image URL</returns>
        [HttpPost("{recipeId}/generate-image")]
        public async Task<ActionResult<RecipeImageDto>> GenerateRecipeImage(Guid recipeId, [FromBody] ImageGenerationRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating image for recipe: {recipeId}");

                var recipe = _mockRecipes.FirstOrDefault(r => r.Id == recipeId);
                if (recipe == null)
                {
                    return NotFound(new { error = "Recipe not found", recipeId });
                }

                // TODO: Replace with actual DALL-E integration
                await Task.Delay(3000); // Simulate image generation time

                // Mock image generation
                var mockImageUrl = $"https://mock-dalle-images.com/recipe-{recipeId}.jpg";

                var result = new RecipeImageDto
                {
                    RecipeId = recipeId,
                    RecipeName = recipe.Name,
                    ImageUrl = mockImageUrl,
                    ImagePrompt = $"A beautiful, appetizing photo of {recipe.Name}, professional food photography, well-lit, restaurant quality",
                    GeneratedAt = DateTime.UtcNow,
                    ImageStyle = request.Style ?? "realistic"
                };

                // Update the recipe with the new image URL
                recipe.ImageUrl = mockImageUrl;

                _logger.LogInformation($"Generated image for recipe: {recipe.Name}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating image for recipe {recipeId}");
                return StatusCode(500, new { error = "Failed to generate recipe image", message = ex.Message });
            }
        }

        /// <summary>
        /// Mark ingredients as used after cooking a recipe
        /// </summary>
        /// <param name="request">Ingredient consumption request</param>
        /// <returns>Success result</returns>
        [HttpPost("consume-ingredients")]
        public async Task<ActionResult> ConsumeIngredientsForRecipe([FromBody] ConsumeIngredientsRequest request)
        {
            try
            {
                _logger.LogInformation($"Consuming ingredients for user: {request.UserId}, recipe: {request.RecipeName}");

                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                if (request.IngredientsToConsume == null || request.IngredientsToConsume.Length == 0)
                {
                    return BadRequest(new { error = "Ingredients to consume are required" });
                }

                // TODO: Integration with PantryController to actually consume ingredients
                await Task.Delay(500);

                var result = new
                {
                    message = "Ingredients consumption recorded successfully",
                    userId = request.UserId,
                    recipeName = request.RecipeName,
                    consumedIngredients = request.IngredientsToConsume.Select(ing => new
                    {
                        name = ing.IngredientName,
                        quantity = ing.Quantity,
                        unit = ing.Unit
                    }).ToArray(),
                    processedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"Processed ingredient consumption for {request.IngredientsToConsume.Length} ingredients");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming ingredients");
                return StatusCode(500, new { error = "Failed to consume ingredients", message = ex.Message });
            }
        }

        /// <summary>
        /// Get recipe suggestions by cuisine type
        /// </summary>
        /// <param name="cuisine">Cuisine type</param>
        /// <param name="limit">Maximum number of recipes to return</param>
        /// <returns>Recipes filtered by cuisine</returns>
        [HttpGet("cuisine/{cuisine}")]
        public ActionResult<IEnumerable<RecipeDto>> GetRecipesByCuisine(string cuisine, [FromQuery] int limit = 10)
        {
            try
            {
                _logger.LogInformation($"Getting recipes for cuisine: {cuisine}");

                var filteredRecipes = _mockRecipes
                    .Where(r => r.Cuisine.Equals(cuisine, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .ToList();

                return Ok(filteredRecipes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recipes for cuisine: {cuisine}");
                return StatusCode(500, new { error = "Failed to get recipes by cuisine", message = ex.Message });
            }
        }

        #region Helper Methods

        private List<RecipeDto> GenerateMockRecipes(string[] availableIngredients, string? cuisineFilter)
        {
            var ingredientSet = availableIngredients.Select(i => i.ToLowerInvariant()).ToHashSet();

            return _mockRecipes
                .Where(recipe =>
                {
                    // Check if recipe uses available ingredients
                    var recipeIngredients = recipe.Ingredients
                        .SelectMany(ing => ing.Split(' '))
                        .Select(word => word.ToLowerInvariant().Trim(',', '.', '(', ')'))
                        .ToHashSet();

                    var matches = ingredientSet.Intersect(recipeIngredients).Count();
                    return matches >= 1; // At least one matching ingredient
                })
                .Where(recipe => string.IsNullOrEmpty(cuisineFilter) ||
                               recipe.Cuisine.Equals(cuisineFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private string[] GenerateDetailedInstructions(RecipeDto recipe)
        {
            // Mock detailed instructions
            return recipe.Instructions.Select((instruction, index) =>
                $"Step {index + 1}: {instruction} (Allow 2-3 minutes for this step)").ToArray();
        }

        private string[] GenerateCookingTips(RecipeDto recipe)
        {
            return new[]
            {
                "Prep all ingredients before starting to cook",
                "Taste and adjust seasoning as needed",
                "Let the dish rest for a few minutes before serving"
            };
        }

        private string[] GenerateRecipeVariations(RecipeDto recipe)
        {
            return new[]
            {
                "Add your favorite herbs for extra flavor",
                "Substitute ingredients based on dietary preferences",
                "Adjust spice levels to your taste"
            };
        }

        private string[] GenerateRequiredEquipment(RecipeDto recipe)
        {
            return new[]
            {
                "Large skillet or pan",
                "Knife and cutting board",
                "Measuring cups and spoons"
            };
        }

        private decimal EstimateRecipeCost(RecipeDto recipe)
        {
            // Mock cost estimation
            return Math.Round((decimal)(recipe.Servings * 2.50), 2);
        }

        /// <summary>
        /// Generate image for a recipe using DALL-E
        /// </summary>
        [HttpPost("generate-image")]
        public async Task<IActionResult> GenerateRecipeImage([FromBody] GenerateImageRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating image for recipe: {request.RecipeName}");

                if (string.IsNullOrEmpty(request.RecipeName))
                {
                    return BadRequest(new { error = "Recipe name is required" });
                }

                var imageUrl = await _openAIService.GenerateRecipeImageAsync(
                    request.RecipeName, 
                    request.RecipeDescription ?? "A delicious homemade dish", 
                    request.Cuisine ?? "International"
                );

                if (string.IsNullOrEmpty(imageUrl))
                {
                    return StatusCode(500, new { error = "Failed to generate image" });
                }

                return Ok(new { imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating image for recipe: {request.RecipeName}");
                return StatusCode(500, new { error = "An error occurred while generating the recipe image" });
            }
        }

        #endregion
    }

    #region Data Transfer Objects

    /// <summary>
    /// Recipe data transfer object
    /// </summary>
    public class RecipeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public int PrepTimeMinutes { get; set; }
        public int CookTimeMinutes { get; set; }
        public int Servings { get; set; }
        public string[] Ingredients { get; set; } = Array.Empty<string>();
        public string[] Instructions { get; set; } = Array.Empty<string>();
        public NutritionDto? Nutrition { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string[]? GeneratedWith { get; set; }
        public string? CuisineRequested { get; set; }
    }

    /// <summary>
    /// Detailed recipe with enhanced information
    /// </summary>
    public class DetailedRecipeDto : RecipeDto
    {
        public string[] DetailedInstructions { get; set; } = Array.Empty<string>();
        public string[] Tips { get; set; } = Array.Empty<string>();
        public string[] Variations { get; set; } = Array.Empty<string>();
        public string[] Equipment { get; set; } = Array.Empty<string>();
        public decimal EstimatedCost { get; set; }
    }

    /// <summary>
    /// Nutrition information
    /// </summary>
    public class NutritionDto
    {
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public double Fiber { get; set; }
    }

    /// <summary>
    /// Recipe image generation result
    /// </summary>
    public class RecipeImageDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ImagePrompt { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string ImageStyle { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for generating recipes
    /// </summary>
    public class GenerateRecipesRequest
    {
        public Guid UserId { get; set; }
        public string[] AvailableIngredients { get; set; } = Array.Empty<string>();
        public string? CuisineFilter { get; set; }
        public int MaxRecipes { get; set; } = 5;
        public string? DifficultyFilter { get; set; }
    }

    /// <summary>
    /// Request for recipe details
    /// </summary>
    public class RecipeDetailRequest
    {
        public Guid UserId { get; set; }
        public string? RecipeName { get; set; }
        public string[]? AvailableIngredients { get; set; }
        public bool IncludeNutrition { get; set; } = true;
        public bool IncludeTips { get; set; } = true;
    }

    /// <summary>
    /// Request for image generation
    /// </summary>
    public class ImageGenerationRequest
    {
        public string? Style { get; set; } = "realistic";
        public string? AdditionalPrompt { get; set; }
    }

    /// <summary>
    /// Request for consuming recipe ingredients
    /// </summary>
    public class ConsumeIngredientsRequest
    {
        public Guid UserId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public IngredientConsumption[] IngredientsToConsume { get; set; } = Array.Empty<IngredientConsumption>();
    }

    /// <summary>
    /// Individual ingredient consumption
    /// </summary>
    public class IngredientConsumption
    {
        public string IngredientName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for image generation
    /// </summary>
    public class GenerateImageRequest
    {
        public string RecipeName { get; set; } = string.Empty;
        public string? RecipeDescription { get; set; }
        public string? Cuisine { get; set; }
    }

    #endregion
}