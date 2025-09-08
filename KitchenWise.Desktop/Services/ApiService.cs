using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KitchenWise.Desktop.Services
{
    /// <summary>
    /// HTTP client service for communicating with KitchenWise API
    /// Handles all API requests including recipe generation from the desktop application
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(string baseUrl)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(60) // Increased timeout for AI operations
            };

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            // Set default headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "KitchenWise-Desktop/1.0.0");
            
            Console.WriteLine($"ApiService initialized with base URL: {_baseUrl}");
        }

        /// <summary>
        /// Set authorization header for authenticated requests
        /// </summary>
        /// <param name="accessToken">JWT access token</param>
        public void SetAuthorizationHeader(string? accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                Console.WriteLine("Authorization header cleared");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                Console.WriteLine("Authorization header set");
            }
        }

        /// <summary>
        /// Test API connectivity
        /// </summary>
        /// <returns>API status information</returns>
        public async Task<ApiStatusResponse?> GetApiStatusAsync()
        {
            try
            {
                Console.WriteLine("Testing API connectivity...");

                var response = await _httpClient.GetAsync("/");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<ApiStatusResponse>(content, _jsonOptions);

                Console.WriteLine($"API Status: {status?.Status} - Version: {status?.Version}");
                return status;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API connectivity test failed: {ex.Message}");
                return null;
            }
        }

        #region User Management

        /// <summary>
        /// Get all users from API
        /// </summary>
        /// <returns>List of users</returns>
        public async Task<UserDto[]?> GetAllUsersAsync()
        {
            try
            {
                Console.WriteLine("Fetching all users from API...");

                var response = await _httpClient.GetAsync("/api/users");
                response.EnsureSuccessStatusCode();

                var users = await response.Content.ReadFromJsonAsync<UserDto[]>(_jsonOptions);

                Console.WriteLine($"Retrieved {users?.Length ?? 0} users from API");
                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get user by Auth0 ID
        /// </summary>
        /// <param name="auth0UserId">Auth0 user ID</param>
        /// <returns>User data or null if not found</returns>
        public async Task<UserDto?> GetUserByAuth0IdAsync(string auth0UserId)
        {
            try
            {
                Console.WriteLine($"Fetching user by Auth0 ID: {auth0UserId}");

                var encodedUserId = Uri.EscapeDataString(auth0UserId);
                var response = await _httpClient.GetAsync($"/api/users/auth0/{encodedUserId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"User not found: {auth0UserId}");
                    return null;
                }

                response.EnsureSuccessStatusCode();
                var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);

                Console.WriteLine($"Retrieved user: {user?.Email}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user by Auth0 ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create or update user profile
        /// </summary>
        /// <param name="request">User creation request</param>
        /// <returns>Created/updated user</returns>
        public async Task<UserDto?> CreateOrUpdateUserAsync(CreateUserRequest request)
        {
            try
            {
                Console.WriteLine($"Creating/updating user: {request.Email}");

                var response = await _httpClient.PostAsJsonAsync("/api/users", request, _jsonOptions);
                response.EnsureSuccessStatusCode();

                var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);

                Console.WriteLine($"User created/updated successfully: {user?.Email}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating/updating user: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update user's last login time
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        public async Task<bool> UpdateLastLoginAsync(Guid userId)
        {
            try
            {
                Console.WriteLine($"Updating last login for user: {userId}");

                var response = await _httpClient.PutAsync($"/api/users/{userId}/login", null);
                response.EnsureSuccessStatusCode();

                Console.WriteLine("Last login updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating last login: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get user statistics from API
        /// </summary>
        /// <returns>User statistics</returns>
        public async Task<UserStatsResponse?> GetUserStatsAsync()
        {
            try
            {
                Console.WriteLine("Fetching user statistics...");

                var response = await _httpClient.GetAsync("/api/users/stats");
                response.EnsureSuccessStatusCode();

                var stats = await response.Content.ReadFromJsonAsync<UserStatsResponse>(_jsonOptions);

                Console.WriteLine($"Retrieved stats: {stats?.TotalUsers} total users");
                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user stats: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Pantry Management

        /// <summary>
        /// Get all pantry items for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of pantry items</returns>
        public async Task<PantryItemDto[]?> GetUserPantryItemsAsync(Guid userId)
        {
            try
            {
                Console.WriteLine($"Fetching pantry items for user: {userId}");

                var response = await _httpClient.GetAsync($"/api/pantry/user/{userId}");
                response.EnsureSuccessStatusCode();

                var items = await response.Content.ReadFromJsonAsync<PantryItemDto[]>(_jsonOptions);

                Console.WriteLine($"Retrieved {items?.Length ?? 0} pantry items");
                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pantry items: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Add new pantry item
        /// </summary>
        /// <param name="request">Pantry item creation request</param>
        /// <returns>Created pantry item</returns>
        public async Task<PantryItemDto?> AddPantryItemAsync(CreatePantryItemRequest request)
        {
            try
            {
                Console.WriteLine($"Adding pantry item: {request.Name}");

                var response = await _httpClient.PostAsJsonAsync("/api/pantry", request, _jsonOptions);
                response.EnsureSuccessStatusCode();

                var item = await response.Content.ReadFromJsonAsync<PantryItemDto>(_jsonOptions);

                Console.WriteLine($"Pantry item added successfully: {item?.Name}");
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding pantry item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update existing pantry item
        /// </summary>
        /// <param name="itemId">Pantry item ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated pantry item</returns>
        public async Task<PantryItemDto?> UpdatePantryItemAsync(Guid itemId, UpdatePantryItemRequest request)
        {
            try
            {
                Console.WriteLine($"Updating pantry item: {itemId}");

                var response = await _httpClient.PutAsJsonAsync($"/api/pantry/{itemId}", request, _jsonOptions);
                response.EnsureSuccessStatusCode();

                var item = await response.Content.ReadFromJsonAsync<PantryItemDto>(_jsonOptions);

                Console.WriteLine($"Pantry item updated successfully: {item?.Name}");
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating pantry item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete pantry item
        /// </summary>
        /// <param name="itemId">Pantry item ID</param>
        /// <returns>Success status</returns>
        public async Task<bool> DeletePantryItemAsync(Guid itemId)
        {
            try
            {
                Console.WriteLine($"Deleting pantry item: {itemId}");

                var response = await _httpClient.DeleteAsync($"/api/pantry/{itemId}");
                response.EnsureSuccessStatusCode();

                Console.WriteLine("Pantry item deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting pantry item: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Consume quantity of pantry item
        /// </summary>
        /// <param name="itemId">Pantry item ID</param>
        /// <param name="amount">Amount to consume</param>
        /// <returns>Updated item or null if item was removed</returns>
        public async Task<PantryItemDto?> ConsumePantryItemAsync(Guid itemId, double amount)
        {
            try
            {
                Console.WriteLine($"Consuming pantry item: {itemId}, amount: {amount}");

                var request = new ConsumePantryItemRequest { Amount = amount };
                var response = await _httpClient.PostAsJsonAsync($"/api/pantry/{itemId}/consume", request, _jsonOptions);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Check if response contains an item (partial consumption) or a message (complete consumption)
                if (responseContent.Contains("\"message\""))
                {
                    Console.WriteLine("Pantry item consumed completely and removed");
                    return null; // Item was completely consumed and removed
                }
                else
                {
                    var item = JsonSerializer.Deserialize<PantryItemDto>(responseContent, _jsonOptions);
                    Console.WriteLine($"Pantry item partially consumed: {item?.Name}");
                    return item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consuming pantry item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Search pantry items for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="searchTerm">Search term</param>
        /// <param name="category">Category filter</param>
        /// <returns>Filtered pantry items</returns>
        public async Task<PantryItemDto[]?> SearchUserPantryItemsAsync(Guid userId, string? searchTerm = null, string? category = null)
        {
            try
            {
                Console.WriteLine($"Searching pantry for user: {userId}");

                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
                }
                if (!string.IsNullOrWhiteSpace(category))
                {
                    queryParams.Add($"category={Uri.EscapeDataString(category)}");
                }

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/api/pantry/user/{userId}/search{queryString}");
                response.EnsureSuccessStatusCode();

                var items = await response.Content.ReadFromJsonAsync<PantryItemDto[]>(_jsonOptions);

                Console.WriteLine($"Search returned {items?.Length ?? 0} items");
                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching pantry items: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get pantry statistics for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Pantry statistics</returns>
        public async Task<PantryStatsResponse?> GetUserPantryStatsAsync(Guid userId)
        {
            try
            {
                Console.WriteLine($"Fetching pantry stats for user: {userId}");

                var response = await _httpClient.GetAsync($"/api/pantry/user/{userId}/stats");
                response.EnsureSuccessStatusCode();

                var stats = await response.Content.ReadFromJsonAsync<PantryStatsResponse>(_jsonOptions);

                Console.WriteLine($"Retrieved pantry stats: {stats?.TotalItems} items");
                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pantry stats: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Health and Diagnostics

        /// <summary>
        /// Check API health
        /// </summary>
        /// <returns>True if API is healthy</returns>
        public async Task<bool> IsApiHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get detailed API information
        /// </summary>
        /// <returns>API information</returns>
        public async Task<string?> GetApiInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting API info: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Recipe Generation

        /// <summary>
        /// Generate recipe suggestions based on pantry items
        /// </summary>
        /// <param name="request">Recipe generation request</param>
        /// <returns>Generated recipe suggestions</returns>
        public async Task<RecipeDto[]?> GenerateRecipesAsync(GenerateRecipesRequest request)
        {
            try
            {
                Console.WriteLine($"Generating recipes with {request.AvailableIngredients?.Length ?? 0} ingredients");
                
                var response = await _httpClient.PostAsJsonAsync("/api/recipe/generate", request, _jsonOptions);
                response.EnsureSuccessStatusCode();
                
                var recipes = await response.Content.ReadFromJsonAsync<RecipeDto[]>(_jsonOptions);
                
                Console.WriteLine($"Generated {recipes?.Length ?? 0} recipe suggestions");
                return recipes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating recipes: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get detailed recipe information
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="request">Recipe detail request</param>
        /// <returns>Detailed recipe</returns>
        public async Task<DetailedRecipeDto?> GetRecipeDetailsAsync(Guid recipeId, RecipeDetailRequest request)
        {
            try
            {
                Console.WriteLine($"Getting detailed recipe: {recipeId}");
                
                var response = await _httpClient.PostAsJsonAsync($"/api/recipe/{recipeId}/details", request, _jsonOptions);
                response.EnsureSuccessStatusCode();
                
                var recipe = await response.Content.ReadFromJsonAsync<DetailedRecipeDto>(_jsonOptions);
                
                Console.WriteLine($"Retrieved detailed recipe: {recipe?.Name}");
                return recipe;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recipe details: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate image for a recipe
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="request">Image generation request</param>
        /// <returns>Recipe image information</returns>
        public async Task<RecipeImageDto?> GenerateRecipeImageAsync(Guid recipeId, ImageGenerationRequest request)
        {
            try
            {
                Console.WriteLine($"Generating image for recipe: {recipeId}");
                
                var response = await _httpClient.PostAsJsonAsync($"/api/recipe/{recipeId}/generate-image", request, _jsonOptions);
                response.EnsureSuccessStatusCode();
                
                var recipeImage = await response.Content.ReadFromJsonAsync<RecipeImageDto>(_jsonOptions);
                
                Console.WriteLine($"Generated image for recipe: {recipeImage?.RecipeName}");
                return recipeImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating recipe image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Consume ingredients used in a recipe
        /// </summary>
        /// <param name="request">Ingredient consumption request</param>
        /// <returns>Success status</returns>
        public async Task<bool> ConsumeRecipeIngredientsAsync(ConsumeIngredientsRequest request)
        {
            try
            {
                Console.WriteLine($"Consuming ingredients for recipe: {request.RecipeName}");
                
                var response = await _httpClient.PostAsJsonAsync("/api/recipe/consume-ingredients", request, _jsonOptions);
                response.EnsureSuccessStatusCode();
                
                Console.WriteLine("Recipe ingredients consumed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consuming recipe ingredients: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get recipes by cuisine type
        /// </summary>
        /// <param name="cuisine">Cuisine type</param>
        /// <param name="limit">Maximum number of recipes</param>
        /// <returns>Filtered recipes</returns>
        public async Task<RecipeDto[]?> GetRecipesByCuisineAsync(string cuisine, int limit = 10)
        {
            try
            {
                Console.WriteLine($"Getting recipes for cuisine: {cuisine}");
                
                var response = await _httpClient.GetAsync($"/api/recipe/cuisine/{Uri.EscapeDataString(cuisine)}?limit={limit}");
                response.EnsureSuccessStatusCode();
                
                var recipes = await response.Content.ReadFromJsonAsync<RecipeDto[]>(_jsonOptions);
                
                Console.WriteLine($"Retrieved {recipes?.Length ?? 0} recipes for {cuisine} cuisine");
                return recipes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recipes by cuisine: {ex.Message}");
                return null;
            }
        }

        #endregion

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            Console.WriteLine("ApiService disposed");
        }

        /// <summary>
        /// Generate image for a recipe using DALL-E
        /// </summary>
        /// <param name="recipeName">Name of the recipe</param>
        /// <param name="recipeDescription">Description of the recipe</param>
        /// <param name="cuisine">Cuisine type</param>
        /// <returns>Generated image URL or null if failed</returns>
        public async Task<string?> GenerateRecipeImageAsync(string recipeName, string? recipeDescription = null, string? cuisine = null)
        {
            try
            {
                Console.WriteLine($"Generating image for recipe: {recipeName}");

                var request = new GenerateImageRequest
                {
                    RecipeName = recipeName,
                    RecipeDescription = recipeDescription,
                    Cuisine = cuisine
                };

                var response = await _httpClient.PostAsJsonAsync("/api/recipe/generate-image", request, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GenerateImageResponse>(_jsonOptions);
                    Console.WriteLine($"Image generated successfully: {result?.ImageUrl}");
                    return result?.ImageUrl;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to generate image. Status: {response.StatusCode}, Error: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception generating image: {ex.Message}");
                return null;
            }
        }
    }

    #region Data Transfer Objects (Updated with Recipe Types)

    /// <summary>
    /// API status response
    /// </summary>
    public class ApiStatusResponse
    {
        public string Service { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Documentation { get; set; } = string.Empty;
    }

    /// <summary>
    /// User data transfer object (matches API model)
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Auth0UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public int PantryItemCount { get; set; }
        public string[] PreferredCuisines { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// User creation request (matches API model)
    /// </summary>
    public class CreateUserRequest
    {
        public string Auth0UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool? IsEmailVerified { get; set; }
        public string[]? PreferredCuisines { get; set; }
    }

    /// <summary>
    /// User statistics response
    /// </summary>
    public class UserStatsResponse
    {
        public int TotalUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalPantryItems { get; set; }
        public PopularCuisine[] PopularCuisines { get; set; } = Array.Empty<PopularCuisine>();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Popular cuisine data
    /// </summary>
    public class PopularCuisine
    {
        public string Cuisine { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }

    /// <summary>
    /// Pantry item data transfer object (matches API model)
    /// </summary>
    public class PantryItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Request for creating new pantry item (matches API model)
    /// </summary>
    public class CreatePantryItemRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Request for updating pantry item (matches API model)
    /// </summary>
    public class UpdatePantryItemRequest
    {
        public string? Name { get; set; }
        public double? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Request for consuming pantry item (matches API model)
    /// </summary>
    public class ConsumePantryItemRequest
    {
        public double Amount { get; set; }
    }

    /// <summary>
    /// Pantry statistics response
    /// </summary>
    public class PantryStatsResponse
    {
        public int TotalItems { get; set; }
        public double TotalQuantity { get; set; }
        public int CategoriesCount { get; set; }
        public int ExpiringItems { get; set; }
        public int ExpiredItems { get; set; }
        public int RecentlyAdded { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    #region Recipe DTOs

    /// <summary>
    /// Recipe data transfer object (matches API model)
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
    /// Detailed recipe with enhanced information (matches API model)
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
    /// Nutrition information (matches API model)
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
    /// Recipe image generation result (matches API model)
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
    /// Request for generating recipes (matches API model)
    /// </summary>
    public class GenerateRecipesRequest
    {
        public Guid UserId { get; set; }
        public string[] AvailableIngredients { get; set; } = Array.Empty<string>();
        public string? CuisineFilter { get; set; }
        public int MaxRecipes { get; set; } = 5;
        public string DifficultyPreference { get; set; } = "Any";
    }

    /// <summary>
    /// Request for recipe details (matches API model)
    /// </summary>
    public class RecipeDetailRequest
    {
        public Guid UserId { get; set; }
        public bool IncludeNutrition { get; set; } = true;
        public bool IncludeTips { get; set; } = true;
    }

    /// <summary>
    /// Request for image generation (matches API model)
    /// </summary>
    public class ImageGenerationRequest
    {
        public string? Style { get; set; } = "realistic";
        public string? AdditionalPrompt { get; set; }
    }

    /// <summary>
    /// Request for consuming recipe ingredients (matches API model)
    /// </summary>
    public class ConsumeIngredientsRequest
    {
        public Guid UserId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public IngredientConsumption[] IngredientsToConsume { get; set; } = Array.Empty<IngredientConsumption>();
    }

    /// <summary>
    /// Individual ingredient consumption (matches API model)
    /// </summary>
    public class IngredientConsumption
    {
        public string IngredientName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    #region Image Generation Models

    /// <summary>
    /// Request model for image generation
    /// </summary>
    public class GenerateImageRequest
    {
        public string RecipeName { get; set; } = string.Empty;
        public string? RecipeDescription { get; set; }
        public string? Cuisine { get; set; }
    }

    /// <summary>
    /// Response model for image generation
    /// </summary>
    public class GenerateImageResponse
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    #endregion

    #endregion

    #endregion
}