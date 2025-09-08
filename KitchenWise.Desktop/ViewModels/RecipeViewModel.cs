using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using KitchenWise.Desktop.Services;
using KitchenWise.Desktop.Utilities;

namespace KitchenWise.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for recipe generation and management with AI integration
    /// Handles recipe discovery, detailed views, and ingredient consumption
    /// </summary>
    public class RecipeViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;

        private ObservableCollection<RecipeDto> _suggestedRecipes;
        private ObservableCollection<RecipeDto> _favoriteRecipes;
        private RecipeDto? _selectedRecipe;
        private DetailedRecipeDto? _selectedDetailedRecipe;
        private string _selectedCuisine = "Any";
        private string _difficultyPreference = "Any";
        private int _maxRecipes = 5;
        private bool _isGeneratingRecipes;
        private bool _isLoadingRecipeDetails;
        private bool _showRecipeDetails;

        public RecipeViewModel(ApiService apiService, AuthService authService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _suggestedRecipes = new ObservableCollection<RecipeDto>();
            _favoriteRecipes = new ObservableCollection<RecipeDto>();

            InitializeCommands();

            Console.WriteLine("RecipeViewModel initialized with AI integration");
        }

        #region Properties

        /// <summary>
        /// AI-generated recipe suggestions based on pantry items
        /// </summary>
        public ObservableCollection<RecipeDto> SuggestedRecipes
        {
            get => _suggestedRecipes;
            set => SetProperty(ref _suggestedRecipes, value);
        }

        /// <summary>
        /// User's favorite recipes
        /// </summary>
        public ObservableCollection<RecipeDto> FavoriteRecipes
        {
            get => _favoriteRecipes;
            set => SetProperty(ref _favoriteRecipes, value);
        }

        /// <summary>
        /// Currently selected recipe from the list
        /// </summary>
        public RecipeDto? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    OnSelectedRecipeChanged();
                }
            }
        }

        /// <summary>
        /// Detailed information for the selected recipe
        /// </summary>
        public DetailedRecipeDto? SelectedDetailedRecipe
        {
            get => _selectedDetailedRecipe;
            set => SetProperty(ref _selectedDetailedRecipe, value);
        }

        /// <summary>
        /// Selected cuisine filter for recipe generation
        /// </summary>
        public string SelectedCuisine
        {
            get => _selectedCuisine;
            set => SetProperty(ref _selectedCuisine, value);
        }

        /// <summary>
        /// Difficulty preference for recipe generation
        /// </summary>
        public string DifficultyPreference
        {
            get => _difficultyPreference;
            set => SetProperty(ref _difficultyPreference, value);
        }

        /// <summary>
        /// Maximum number of recipes to generate
        /// </summary>
        public int MaxRecipes
        {
            get => _maxRecipes;
            set => SetProperty(ref _maxRecipes, Math.Max(1, Math.Min(10, value)));
        }

        /// <summary>
        /// Whether recipe generation is in progress
        /// </summary>
        public bool IsGeneratingRecipes
        {
            get => _isGeneratingRecipes;
            set => SetProperty(ref _isGeneratingRecipes, value);
        }

        /// <summary>
        /// Whether recipe details are being loaded
        /// </summary>
        public bool IsLoadingRecipeDetails
        {
            get => _isLoadingRecipeDetails;
            set => SetProperty(ref _isLoadingRecipeDetails, value);
        }

        /// <summary>
        /// Whether to show detailed recipe view
        /// </summary>
        public bool ShowRecipeDetails
        {
            get => _showRecipeDetails;
            set => SetProperty(ref _showRecipeDetails, value);
        }

        /// <summary>
        /// Available cuisine options for filtering
        /// </summary>
        public string[] AvailableCuisines => new[]
        {
            "Any", "Italian", "Mexican", "Asian", "American", "Mediterranean", 
            "Indian", "French", "Japanese", "Chinese", "Thai", "Greek"
        };

        /// <summary>
        /// Available difficulty levels
        /// </summary>
        public string[] DifficultyLevels => new[] { "Any", "Easy", "Medium", "Hard" };

        /// <summary>
        /// Total number of suggested recipes
        /// </summary>
        public int TotalSuggestedRecipes => SuggestedRecipes.Count;

        /// <summary>
        /// Total number of favorite recipes
        /// </summary>
        public int TotalFavoriteRecipes => FavoriteRecipes.Count;

        /// <summary>
        /// Whether there are any suggested recipes
        /// </summary>
        public bool HasSuggestedRecipes => SuggestedRecipes.Any();

        /// <summary>
        /// Whether there are any favorite recipes
        /// </summary>
        public bool HasFavoriteRecipes => FavoriteRecipes.Any();

        #endregion

        #region Commands

        public ICommand GenerateRecipesCommand { get; private set; } = null!;
        public ICommand LoadRecipeDetailsCommand { get; private set; } = null!;
        public ICommand GenerateRecipeImageCommand { get; private set; } = null!;
        public ICommand AddToFavoritesCommand { get; private set; } = null!;
        public ICommand RemoveFromFavoritesCommand { get; private set; } = null!;
        public ICommand CookRecipeCommand { get; private set; } = null!;
        public ICommand BackToRecipeListCommand { get; private set; } = null!;
        public ICommand LoadCuisineRecipesCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            GenerateRecipesCommand = new AsyncRelayCommand(GenerateRecipesAsync, CanGenerateRecipes);
            LoadRecipeDetailsCommand = new AsyncRelayCommand(LoadRecipeDetailsAsync, () => SelectedRecipe != null);
            GenerateRecipeImageCommand = new AsyncRelayCommand(GenerateRecipeImageAsync, () => SelectedRecipe != null);
            AddToFavoritesCommand = new RelayCommand(AddToFavorites, () => SelectedRecipe != null && !IsFavorite(SelectedRecipe));
            RemoveFromFavoritesCommand = new RelayCommand(RemoveFromFavorites, () => SelectedRecipe != null && IsFavorite(SelectedRecipe));
            CookRecipeCommand = new AsyncRelayCommand(CookRecipeAsync, () => SelectedDetailedRecipe != null);
            BackToRecipeListCommand = new RelayCommand(BackToRecipeList, () => ShowRecipeDetails);
            LoadCuisineRecipesCommand = new AsyncRelayCommand(LoadCuisineRecipesAsync, () => SelectedCuisine != "Any");
        }

        #endregion

        #region Recipe Generation

        /// <summary>
        /// Generate AI-powered recipe suggestions based on available pantry items
        /// </summary>
        private async Task GenerateRecipesAsync()
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUserData == null)
            {
                SetErrorState(true, "Please sign in to generate recipes");
                return;
            }

            try
            {
                SetBusyState(true, "Generating AI-powered recipe suggestions...");
                IsGeneratingRecipes = true;
                ClearErrors();

                // Get user's current pantry items
                var pantryItems = await _apiService.GetUserPantryItemsAsync(_authService.CurrentUserData.Id);
                if (pantryItems == null || pantryItems.Length == 0)
                {
                    SetErrorState(true, "No pantry items found. Please add some ingredients to your pantry first.");
                    return;
                }

                // Extract ingredient names for recipe generation
                var availableIngredients = pantryItems.Select(item => item.Name).ToArray();

                var request = new GenerateRecipesRequest
                {
                    UserId = _authService.CurrentUserData.Id,
                    AvailableIngredients = availableIngredients,
                    CuisineFilter = SelectedCuisine == "Any" ? null : SelectedCuisine,
                    MaxRecipes = MaxRecipes,
                    DifficultyPreference = DifficultyPreference
                };

                Log($"Generating recipes with {availableIngredients.Length} ingredients");

                var recipes = await _apiService.GenerateRecipesAsync(request);

                if (recipes != null && recipes.Length > 0)
                {
                    SuggestedRecipes.Clear();
                    foreach (var recipe in recipes)
                    {
                        SuggestedRecipes.Add(recipe);
                    }

                    Log($"Generated {recipes.Length} recipe suggestions");
                    UpdateRecipeCounts();
                }
                else
                {
                    SetErrorState(true, "Failed to generate recipes. Please try again.");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to generate recipe suggestions");
            }
            finally
            {
                SetBusyState(false);
                IsGeneratingRecipes = false;
            }
        }

        private bool CanGenerateRecipes()
        {
            return _authService.IsAuthenticated && 
                   !IsGeneratingRecipes && 
                   !IsBusy;
        }

        #endregion

        #region Recipe Details

        /// <summary>
        /// Load detailed information for the selected recipe
        /// </summary>
        private async Task LoadRecipeDetailsAsync()
        {
            if (SelectedRecipe == null || !_authService.IsAuthenticated || _authService.CurrentUserData == null)
                return;

            try
            {
                SetBusyState(true, "Loading detailed recipe information...");
                IsLoadingRecipeDetails = true;
                ClearErrors();

                var request = new RecipeDetailRequest
                {
                    UserId = _authService.CurrentUserData.Id,
                    IncludeNutrition = true,
                    IncludeTips = true
                };

                Log($"Loading details for recipe: {SelectedRecipe.Name}");

                var detailedRecipe = await _apiService.GetRecipeDetailsAsync(SelectedRecipe.Id, request);

                if (detailedRecipe != null)
                {
                    SelectedDetailedRecipe = detailedRecipe;
                    ShowRecipeDetails = true;
                    Log($"Loaded detailed recipe: {detailedRecipe.Name}");
                }
                else
                {
                    SetErrorState(true, "Failed to load recipe details");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to load recipe details");
            }
            finally
            {
                SetBusyState(false);
                IsLoadingRecipeDetails = false;
            }
        }

        #endregion

        #region Recipe Images

        /// <summary>
        /// Generate AI-powered image for the selected recipe
        /// </summary>
        private async Task GenerateRecipeImageAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                SetBusyState(true, "Generating recipe image with AI...");
                ClearErrors();

                var request = new ImageGenerationRequest
                {
                    Style = "realistic",
                    AdditionalPrompt = "professional food photography, well-lit, appetizing"
                };

                Log($"Generating image for recipe: {SelectedRecipe.Name}");

                var recipeImage = await _apiService.GenerateRecipeImageAsync(SelectedRecipe.Id, request);

                if (recipeImage != null)
                {
                    // Update the recipe with the generated image URL
                    SelectedRecipe.ImageUrl = recipeImage.ImageUrl;
                    
                    // Update detailed recipe if loaded
                    if (SelectedDetailedRecipe != null && SelectedDetailedRecipe.Id == SelectedRecipe.Id)
                    {
                        SelectedDetailedRecipe.ImageUrl = recipeImage.ImageUrl;
                    }

                    Log($"Generated image for recipe: {SelectedRecipe.Name}");
                }
                else
                {
                    SetErrorState(true, "Failed to generate recipe image");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to generate recipe image");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        #endregion

        #region Favorites Management

        /// <summary>
        /// Add selected recipe to favorites
        /// </summary>
        private void AddToFavorites()
        {
            if (SelectedRecipe == null || IsFavorite(SelectedRecipe)) return;

            try
            {
                FavoriteRecipes.Add(SelectedRecipe);
                Log($"Added recipe to favorites: {SelectedRecipe.Name}");
                UpdateRecipeCounts();
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to add recipe to favorites");
            }
        }

        /// <summary>
        /// Remove selected recipe from favorites
        /// </summary>
        private void RemoveFromFavorites()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var favoriteToRemove = FavoriteRecipes.FirstOrDefault(r => r.Id == SelectedRecipe.Id);
                if (favoriteToRemove != null)
                {
                    FavoriteRecipes.Remove(favoriteToRemove);
                    Log($"Removed recipe from favorites: {SelectedRecipe.Name}");
                    UpdateRecipeCounts();
                    UpdateCommandStates();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to remove recipe from favorites");
            }
        }

        /// <summary>
        /// Check if a recipe is in favorites
        /// </summary>
        private bool IsFavorite(RecipeDto recipe)
        {
            return FavoriteRecipes.Any(r => r.Id == recipe.Id);
        }

        #endregion

        #region Cooking & Ingredient Consumption

        /// <summary>
        /// Mark recipe as cooked and consume ingredients from pantry
        /// </summary>
        private async Task CookRecipeAsync()
        {
            if (SelectedDetailedRecipe == null || !_authService.IsAuthenticated || _authService.CurrentUserData == null)
                return;

            try
            {
                SetBusyState(true, "Recording cooking session and updating pantry...");
                ClearErrors();

                // Create ingredient consumption request based on recipe
                var ingredientsToConsume = SelectedDetailedRecipe.Ingredients
                    .Select(ingredient => ParseIngredientForConsumption(ingredient))
                    .Where(ing => ing != null)
                    .ToArray();

                var request = new ConsumeIngredientsRequest
                {
                    UserId = _authService.CurrentUserData.Id,
                    RecipeName = SelectedDetailedRecipe.Name,
                    IngredientsToConsume = ingredientsToConsume!
                };

                Log($"Recording cooking session for: {SelectedDetailedRecipe.Name}");

                var success = await _apiService.ConsumeRecipeIngredientsAsync(request);

                if (success)
                {
                    Log($"Successfully recorded cooking session: {SelectedDetailedRecipe.Name}");
                    
                    // TODO: Add cooking history tracking
                    // TODO: Show success message with ingredients consumed
                }
                else
                {
                    SetErrorState(true, "Failed to record cooking session");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to record cooking session");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        /// <summary>
        /// Parse recipe ingredient string to extract consumption information
        /// </summary>
        private IngredientConsumption? ParseIngredientForConsumption(string ingredient)
        {
            try
            {
                // Simple parsing - in a real implementation, this would be more sophisticated
                var parts = ingredient.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length >= 2 && double.TryParse(parts[0], out double quantity))
                {
                    var unit = parts[1];
                    var name = string.Join(" ", parts.Skip(2));

                    return new IngredientConsumption
                    {
                        IngredientName = name,
                        Quantity = quantity,
                        Unit = unit
                    };
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to parse ingredient: {ingredient}, Error: {ex.Message}", "WARN");
            }

            return null;
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Return to recipe list view from details
        /// </summary>
        private void BackToRecipeList()
        {
            ShowRecipeDetails = false;
            SelectedDetailedRecipe = null;
        }

        #endregion

        #region Cuisine-Based Recipes

        /// <summary>
        /// Load recipes by selected cuisine type
        /// </summary>
        private async Task LoadCuisineRecipesAsync()
        {
            if (SelectedCuisine == "Any") return;

            try
            {
                SetBusyState(true, $"Loading {SelectedCuisine} recipes...");
                ClearErrors();

                Log($"Loading recipes for cuisine: {SelectedCuisine}");

                var recipes = await _apiService.GetRecipesByCuisineAsync(SelectedCuisine, MaxRecipes);

                if (recipes != null && recipes.Length > 0)
                {
                    SuggestedRecipes.Clear();
                    foreach (var recipe in recipes)
                    {
                        SuggestedRecipes.Add(recipe);
                    }

                    Log($"Loaded {recipes.Length} {SelectedCuisine} recipes");
                    UpdateRecipeCounts();
                }
                else
                {
                    SetErrorState(true, $"No {SelectedCuisine} recipes found");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, $"Failed to load {SelectedCuisine} recipes");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        #endregion

        #region Helper Methods

        private void OnSelectedRecipeChanged()
        {
            LogDebug($"Selected recipe: {SelectedRecipe?.Name ?? "None"}");
            UpdateCommandStates();
        }

        private void UpdateCommandStates()
        {
            (LoadRecipeDetailsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (GenerateRecipeImageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (AddToFavoritesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveFromFavoritesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CookRecipeCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateRecipeCounts()
        {
            OnPropertyChanged(nameof(TotalSuggestedRecipes));
            OnPropertyChanged(nameof(TotalFavoriteRecipes));
            OnPropertyChanged(nameof(HasSuggestedRecipes));
            OnPropertyChanged(nameof(HasFavoriteRecipes));
        }

        #endregion

        #region Overrides

        public override async void OnActivated()
        {
            base.OnActivated();
            Log("RecipeViewModel activated");
            
            // Auto-generate recipes if user has pantry items
            if (_authService.IsAuthenticated && !HasSuggestedRecipes && !IsBusy)
            {
                Log("Auto-generating initial recipe suggestions");
                await GenerateRecipesAsync();
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            Log("Refreshing recipe data");
            
            if (_authService.IsAuthenticated && !IsBusy)
            {
                _ = GenerateRecipesAsync();
            }
        }

        public override bool Validate()
        {
            ClearErrors();

            if (!_authService.IsAuthenticated)
            {
                SetErrorState(true, "Please sign in to access recipe features");
                return false;
            }

            return !HasErrors;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            SuggestedRecipes.Clear();
            FavoriteRecipes.Clear();
            SelectedRecipe = null;
            SelectedDetailedRecipe = null;
            ShowRecipeDetails = false;
            Log("RecipeViewModel cleaned up");
        }

        #endregion
    }
}
