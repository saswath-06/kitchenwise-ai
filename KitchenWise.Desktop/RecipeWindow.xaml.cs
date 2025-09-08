using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using KitchenWise.Desktop.ViewModels;
using KitchenWise.Desktop.Services;

namespace KitchenWise.Desktop
{
    /// <summary>
    /// Recipe Center window for displaying AI-generated recipes and detailed recipe information
    /// </summary>
    public partial class RecipeWindow : Window
    {
        private readonly RecipeViewModel _recipeViewModel;
        private readonly ApiService _apiService;
        private readonly AuthService _authService;

        public RecipeWindow(RecipeViewModel recipeViewModel, ApiService apiService, AuthService authService)
        {
            InitializeComponent();
            
            _recipeViewModel = recipeViewModel ?? throw new ArgumentNullException(nameof(recipeViewModel));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            InitializeWindow();
            SetupDataBinding();
            
            Console.WriteLine("RecipeWindow initialized successfully");
        }

        /// <summary>
        /// Initialize window properties and event handlers
        /// </summary>
        private void InitializeWindow()
        {
            this.Loaded += RecipeWindow_Loaded;
            this.Closing += RecipeWindow_Closing;
            
            // Set initial UI state
            UpdateRecipeCount();
            ShowEmptyState();
        }

        /// <summary>
        /// Setup data binding between UI and ViewModel
        /// </summary>
        private void SetupDataBinding()
        {
            try
            {
                // Clear any existing items before setting ItemsSource
                RecipeListBox.Items.Clear();
                CuisineFilterCombo.Items.Clear();
                
                // Bind recipe list to ViewModel
                RecipeListBox.ItemsSource = _recipeViewModel.SuggestedRecipes;
                
                // Bind cuisine filter to ViewModel available cuisines
                CuisineFilterCombo.ItemsSource = _recipeViewModel.AvailableCuisines;
                
                // Set initial values after binding
                MaxRecipesText.Text = _recipeViewModel.MaxRecipes.ToString();
                
                // Set selected cuisine after ItemsSource is bound
                if (_recipeViewModel.AvailableCuisines.Contains(_recipeViewModel.SelectedCuisine))
                {
                    CuisineFilterCombo.SelectedItem = _recipeViewModel.SelectedCuisine;
                }
                else
                {
                    CuisineFilterCombo.SelectedIndex = 0; // Default to first item
                }

                // Subscribe to ViewModel property changes
                _recipeViewModel.PropertyChanged += RecipeViewModel_PropertyChanged;
                
                Console.WriteLine("Data binding setup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up data binding: {ex.Message}");
                MessageBox.Show($"Failed to setup data binding: {ex.Message}", "Initialization Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle ViewModel property changes
        /// </summary>
        private void RecipeViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(_recipeViewModel.TotalSuggestedRecipes):
                        UpdateRecipeCount();
                        break;
                    case nameof(_recipeViewModel.SelectedRecipe):
                        UpdateSelectedRecipe();
                        break;
                    case nameof(_recipeViewModel.SelectedDetailedRecipe):
                        UpdateDetailedRecipe();
                        break;
                    case nameof(_recipeViewModel.IsBusy):
                        UpdateBusyState();
                        break;
                    case nameof(_recipeViewModel.HasErrors):
                        if (_recipeViewModel.HasErrors)
                        {
                            MessageBox.Show(_recipeViewModel.ErrorMessage, "Recipe Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        break;
                }
            });
        }

        #region Window Events

        private void RecipeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("RecipeWindow loaded, activating ViewModel");
                _recipeViewModel.OnActivated();
                
                // Update initial state
                UpdateRecipeCount();
                UpdateBusyState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RecipeWindow_Loaded: {ex.Message}");
                MessageBox.Show($"Error loading recipe window: {ex.Message}", "Load Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecipeWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Console.WriteLine("RecipeWindow closing, cleaning up ViewModel");
                _recipeViewModel.PropertyChanged -= RecipeViewModel_PropertyChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RecipeWindow_Closing: {ex.Message}");
            }
        }

        #endregion

        #region Button Event Handlers

        private void BtnRefreshRecipes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Refreshing recipes");
                _recipeViewModel.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing recipes: {ex.Message}");
                MessageBox.Show($"Failed to refresh recipes: {ex.Message}", "Refresh Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Closing recipe window");
                this.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing window: {ex.Message}");
            }
        }

        private async void BtnGenerateRecipes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Generating recipes from UI");
                
                // Update ViewModel with UI values
                if (int.TryParse(MaxRecipesText.Text, out int maxRecipes))
                {
                    _recipeViewModel.MaxRecipes = maxRecipes;
                }

                if (_recipeViewModel.GenerateRecipesCommand.CanExecute(null))
                {
                    _recipeViewModel.GenerateRecipesCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating recipes: {ex.Message}");
                MessageBox.Show($"Failed to generate recipes: {ex.Message}", "Generation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_recipeViewModel.AddToFavoritesCommand.CanExecute(null))
                {
                    _recipeViewModel.AddToFavoritesCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to favorites: {ex.Message}");
                MessageBox.Show($"Failed to add to favorites: {ex.Message}", "Favorites Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGenerateImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRecipe = _recipeViewModel.SelectedRecipe;
                if (selectedRecipe == null)
                {
                    MessageBox.Show("Please select a recipe first.", "No Recipe Selected", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show loading state
                BtnGenerateImage.IsEnabled = false;
                BtnGenerateImage.Content = "🔄 Generating...";
                
                Console.WriteLine($"Generating image for recipe: {selectedRecipe.Name}");

                // Generate image using API
                var imageUrl = await _apiService.GenerateRecipeImageAsync(
                    selectedRecipe.Name,
                    selectedRecipe.Description,
                    selectedRecipe.Cuisine
                );

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Load and display the image
                    await LoadRecipeImage(imageUrl);
                    
                    // Update the recipe with the image URL
                    selectedRecipe.ImageUrl = imageUrl;
                    
                    MessageBox.Show("Recipe image generated successfully!", "Image Generated", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to generate recipe image. Please try again.", "Generation Failed", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating image: {ex.Message}");
                MessageBox.Show($"Failed to generate image: {ex.Message}", "Image Generation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Reset button state
                BtnGenerateImage.IsEnabled = true;
                BtnGenerateImage.Content = "📷 Generate Image";
            }
        }

        /// <summary>
        /// Load and display recipe image from URL with proper sizing
        /// </summary>
        private async Task LoadRecipeImage(string imageUrl)
        {
            try
            {
                Console.WriteLine($"Loading recipe image from URL: {imageUrl}");

                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                // Set maximum dimensions to prevent oversized images while maintaining quality
                bitmap.DecodePixelWidth = 500; // Max width for better performance and sizing
                bitmap.EndInit();
                bitmap.Freeze();

                // Show the image and hide the placeholder
                RecipeImage.Source = bitmap;
                RecipeImageBorder.Visibility = Visibility.Visible;

                // Adjust border height based on image aspect ratio for better fit
                var aspectRatio = (double)bitmap.PixelHeight / bitmap.PixelWidth;
                var targetHeight = Math.Min(300, Math.Max(150, 500 * aspectRatio));
                RecipeImageBorder.Height = targetHeight;

                Console.WriteLine($"Recipe image loaded successfully - Size: {bitmap.PixelWidth}x{bitmap.PixelHeight}, Target Height: {targetHeight}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading recipe image: {ex.Message}");
                // Keep the placeholder visible if image loading fails
                RecipeImageBorder.Visibility = Visibility.Visible;
                RecipeImageBorder.Height = 200; // Default height for placeholder
            }
        }

        private async void BtnStartCooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRecipe = _recipeViewModel.SelectedRecipe;
                if (selectedRecipe == null)
                {
                    MessageBox.Show("Please select a recipe first.", "No Recipe Selected", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_authService.IsAuthenticated || _authService.CurrentUserData == null)
                {
                    MessageBox.Show("Please sign in to start cooking.", "Authentication Required", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show detailed confirmation with ingredients list
                var ingredientsList = string.Join("\n• ", selectedRecipe.Ingredients);
                var result = MessageBox.Show(
                    $"Start cooking '{selectedRecipe.Name}'?\n\n" +
                    $"This will attempt to consume the following ingredients from your pantry:\n\n• {ingredientsList}\n\n" +
                    $"Note: Only ingredients found in your pantry will be consumed.",
                    "Start Cooking & Consume Ingredients",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await StartCookingAndConsumeIngredientsAsync(selectedRecipe);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting cooking: {ex.Message}");
                MessageBox.Show($"Failed to start cooking: {ex.Message}", "Cooking Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Start cooking process and consume ingredients from pantry
        /// </summary>
        private async Task StartCookingAndConsumeIngredientsAsync(RecipeDto recipe)
        {
            try
            {
                BtnStartCooking.IsEnabled = false;
                BtnStartCooking.Content = "🔄 Consuming Ingredients...";
                StatusText.Text = "Consuming ingredients from pantry...";

                var userId = _authService.CurrentUserData!.Id;
                var consumedItems = new List<string>();
                var notFoundItems = new List<string>();

                // Get current pantry items
                var pantryItems = await _apiService.GetUserPantryItemsAsync(userId);
                if (pantryItems == null)
                {
                    MessageBox.Show("Could not access pantry items.", "Pantry Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Process each ingredient in the recipe
                foreach (var ingredient in recipe.Ingredients)
                {
                    var consumeResult = await TryConsumeIngredientAsync(ingredient, pantryItems);
                    if (consumeResult.wasConsumed)
                    {
                        consumedItems.Add(consumeResult.itemName);
                    }
                    else
                    {
                        notFoundItems.Add(consumeResult.itemName);
                    }
                }

                // Show results
                var resultMessage = $"Started cooking '{recipe.Name}'!\n\n";
                
                if (consumedItems.Any())
                {
                    resultMessage += $"✅ Consumed from pantry:\n• {string.Join("\n• ", consumedItems)}\n\n";
                }

                if (notFoundItems.Any())
                {
                    resultMessage += $"⚠️ Not found in pantry (you may need to add these):\n• {string.Join("\n• ", notFoundItems)}\n\n";
                }

                resultMessage += "Enjoy cooking! 🍳";

                MessageBox.Show(resultMessage, "Cooking Started", MessageBoxButton.OK, MessageBoxImage.Information);

                Console.WriteLine($"Cooking started for {recipe.Name}. Consumed: {consumedItems.Count}, Not found: {notFoundItems.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in cooking process: {ex.Message}");
                MessageBox.Show($"Error during cooking process: {ex.Message}", "Cooking Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnStartCooking.IsEnabled = true;
                BtnStartCooking.Content = "🍳 Start Cooking & Use Ingredients";
                StatusText.Text = "Ready to generate recipes";
            }
        }

        /// <summary>
        /// Try to consume a recipe ingredient from pantry
        /// </summary>
        private async Task<(bool wasConsumed, string itemName)> TryConsumeIngredientAsync(string ingredient, PantryItemDto[] pantryItems)
        {
            try
            {
                // Parse ingredient to extract name and quantity
                var parsedIngredient = ParseRecipeIngredient(ingredient);
                if (string.IsNullOrEmpty(parsedIngredient.name))
                {
                    return (false, ingredient);
                }

                // Find matching pantry item (case-insensitive partial match)
                var matchingItem = pantryItems.FirstOrDefault(item => 
                    item.Name.Contains(parsedIngredient.name, StringComparison.OrdinalIgnoreCase) ||
                    parsedIngredient.name.Contains(item.Name, StringComparison.OrdinalIgnoreCase));

                if (matchingItem != null)
                {
                    // Try to consume the ingredient
                    var consumeAmount = Math.Min(parsedIngredient.quantity, matchingItem.Quantity);
                    var updatedItem = await _apiService.ConsumePantryItemAsync(matchingItem.Id, consumeAmount);
                    
                    Console.WriteLine($"Consumed {consumeAmount} {matchingItem.Unit} of {matchingItem.Name}");
                    return (true, $"{consumeAmount} {matchingItem.Unit} {matchingItem.Name}");
                }

                return (false, parsedIngredient.name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consuming ingredient '{ingredient}': {ex.Message}");
                return (false, ingredient);
            }
        }

        /// <summary>
        /// Parse recipe ingredient string to extract name and quantity
        /// </summary>
        private (string name, double quantity, string unit) ParseRecipeIngredient(string ingredient)
        {
            try
            {
                // Simple parsing for common formats like "2 cups flour", "1 lb ground beef", etc.
                var parts = ingredient.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length >= 2)
                {
                    // Try to parse quantity from first part
                    if (double.TryParse(parts[0], out double quantity))
                    {
                        var unit = parts.Length > 2 ? parts[1] : "piece";
                        var name = string.Join(" ", parts.Skip(2));
                        
                        // If no name after unit, assume unit is part of name
                        if (string.IsNullOrEmpty(name))
                        {
                            name = string.Join(" ", parts.Skip(1));
                            unit = "piece";
                        }
                        
                        return (name, quantity, unit);
                    }
                }

                // If parsing fails, assume 1 unit of the whole ingredient name
                return (ingredient, 1.0, "piece");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing ingredient '{ingredient}': {ex.Message}");
                return (ingredient, 1.0, "piece");
            }
        }

        #endregion

        #region Control Event Handlers

        private void CuisineFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox comboBox && comboBox.SelectedItem is string selectedCuisine)
                {
                    _recipeViewModel.SelectedCuisine = selectedCuisine;
                    Console.WriteLine($"Cuisine filter changed to: {selectedCuisine}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing cuisine filter: {ex.Message}");
            }
        }

        private void RecipeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ListBox listBox && listBox.SelectedItem is RecipeDto selectedRecipe)
                {
                    _recipeViewModel.SelectedRecipe = selectedRecipe;
                    Console.WriteLine($"Recipe selected: {selectedRecipe.Name}");
                }
                else
                {
                    _recipeViewModel.SelectedRecipe = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error selecting recipe: {ex.Message}");
            }
        }

        #endregion

        #region UI Update Methods

        private void UpdateRecipeCount()
        {
            try
            {
                var count = _recipeViewModel.TotalSuggestedRecipes;
                RecipeCountText.Text = count > 0 ? $"{count} recipes generated" : "No recipes generated";
                RecipeStatsText.Text = count > 0 ? $"Total: {count} recipes" : "Generate recipes to get started";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating recipe count: {ex.Message}");
            }
        }

        private void UpdateSelectedRecipe()
        {
            try
            {
                var selectedRecipe = _recipeViewModel.SelectedRecipe;
                
                if (selectedRecipe != null)
                {
                    // Update recipe header
                    RecipeNameText.Text = selectedRecipe.Name;
                    
                    // Show recipe details panel
                    RecipeDetailsPanel.Visibility = Visibility.Visible;
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                    
                    // Update basic info
                    PrepTimeText.Text = $"{selectedRecipe.PrepTimeMinutes} min";
                    CookTimeText.Text = $"{selectedRecipe.CookTimeMinutes} min";
                    ServingsText.Text = selectedRecipe.Servings.ToString();
                    DifficultyText.Text = selectedRecipe.DifficultyLevel;
                    RecipeDescriptionText.Text = selectedRecipe.Description;
                    
                    // Update ingredients
                    IngredientsList.ItemsSource = selectedRecipe.Ingredients;
                    
                    // Update instructions
                    InstructionsList.ItemsSource = selectedRecipe.Instructions;
                    
                    // Show/hide nutrition info
                    if (selectedRecipe.Nutrition != null)
                    {
                        NutritionSection.Visibility = Visibility.Visible;
                        CaloriesText.Text = $"{selectedRecipe.Nutrition.Calories:F0}";
                        ProteinText.Text = $"{selectedRecipe.Nutrition.Protein:F1}g";
                        CarbsText.Text = $"{selectedRecipe.Nutrition.Carbs:F1}g";
                        FatText.Text = $"{selectedRecipe.Nutrition.Fat:F1}g";
                        FiberText.Text = $"{selectedRecipe.Nutrition.Fiber:F1}g";
                    }
                    else
                    {
                        NutritionSection.Visibility = Visibility.Collapsed;
                    }
                    
                    // Show/hide recipe image
                    if (!string.IsNullOrEmpty(selectedRecipe.ImageUrl))
                    {
                        _ = LoadRecipeImage(selectedRecipe.ImageUrl); // Load image asynchronously
                    }
                    else
                    {
                        RecipeImageBorder.Visibility = Visibility.Collapsed;
                        RecipeImage.Source = null;
                        RecipeImageBorder.Height = double.NaN; // Reset to auto height
                    }
                    
                    Console.WriteLine($"Updated UI for selected recipe: {selectedRecipe.Name}");
                }
                else
                {
                    ShowEmptyState();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating selected recipe: {ex.Message}");
            }
        }

        private void UpdateDetailedRecipe()
        {
            try
            {
                var detailedRecipe = _recipeViewModel.SelectedDetailedRecipe;
                
                if (detailedRecipe != null)
                {
                    // Update with detailed information
                    Console.WriteLine($"Updated UI with detailed recipe: {detailedRecipe.Name}");
                    
                    // Show additional nutrition info if available
                    if (detailedRecipe.Nutrition != null)
                    {
                        NutritionSection.Visibility = Visibility.Visible;
                        CaloriesText.Text = $"{detailedRecipe.Nutrition.Calories:F0}";
                        ProteinText.Text = $"{detailedRecipe.Nutrition.Protein:F1}g";
                        CarbsText.Text = $"{detailedRecipe.Nutrition.Carbs:F1}g";
                        FatText.Text = $"{detailedRecipe.Nutrition.Fat:F1}g";
                        FiberText.Text = $"{detailedRecipe.Nutrition.Fiber:F1}g";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating detailed recipe: {ex.Message}");
            }
        }

        private void UpdateBusyState()
        {
            try
            {
                var isBusy = _recipeViewModel.IsBusy;
                var busyMessage = _recipeViewModel.BusyMessage;
                
                // Update status text
                StatusText.Text = isBusy ? busyMessage : "Ready to generate recipes";
                
                // Disable/enable buttons during busy operations
                BtnGenerateRecipes.IsEnabled = !isBusy;
                BtnRefreshRecipes.IsEnabled = !isBusy;
                BtnStartCooking.IsEnabled = !isBusy && _recipeViewModel.SelectedRecipe != null;
                
                Console.WriteLine($"Updated busy state: {(isBusy ? busyMessage : "Ready")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating busy state: {ex.Message}");
            }
        }

        private void ShowEmptyState()
        {
            try
            {
                RecipeNameText.Text = "Select a recipe to view details";
                RecipeDetailsPanel.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
                
                Console.WriteLine("Showing empty state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing empty state: {ex.Message}");
            }
        }

        #endregion
    }
}
