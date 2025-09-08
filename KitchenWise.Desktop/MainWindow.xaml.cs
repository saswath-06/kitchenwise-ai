using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using KitchenWise.Desktop.Models;
using KitchenWise.Desktop.ViewModels;
using KitchenWise.Desktop.Services;

namespace KitchenWise.Desktop
{
    /// <summary>
    /// Main application window for KitchenWise with API-connected pantry management
    /// </summary>
    public partial class MainWindow : Window
    {
        private PantryViewModel? _pantryViewModel;
        private RecipeViewModel? _recipeViewModel;
        private AuthService? _authService;
        private ApiService? _apiService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            InitializeServices();
        }

        /// <summary>
        /// Initialize window properties and event handlers
        /// </summary>
        private void InitializeWindow()
        {
            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;

            // Enable window dragging by clicking on header
            this.MouseLeftButtonDown += (sender, e) => this.DragMove();
        }

        /// <summary>
        /// Initialize authentication and API services
        /// </summary>
        private async void InitializeServices()
        {
            try
            {
                Console.WriteLine("Initializing services...");

                // Initialize API service first
                _apiService = new ApiService("http://localhost:5196");

                // Test API connectivity
                var apiStatus = await _apiService.GetApiStatusAsync();
                if (apiStatus != null)
                {
                    Console.WriteLine($"API connected: {apiStatus.Service} v{apiStatus.Version}");
                }

                // Initialize AuthService with API integration
                _authService = new AuthService(
                    domain: "dev-au2yf8c1n0hrml0i.us.auth0.com",
                    clientId: "Cc5LBWI8tn20z2AHZ2h13tmvKwF6PrP8",
                    redirectUri: "http://localhost:8080/callback",
                    apiService: _apiService
                );

                _authService.AuthStateChanged += OnAuthStateChanged;
                await _authService.InitializeAsync();

                Console.WriteLine("Services initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing services: {ex.Message}");
                MessageBox.Show($"Failed to initialize application services: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Initialize the pantry ViewModel with API integration
        /// </summary>
        private void InitializePantryViewModel()
        {
            try
            {
                if (_apiService == null || _authService == null)
                {
                    Console.WriteLine("Cannot initialize PantryViewModel - services not available");
                    return;
                }

                Console.WriteLine("Creating API-connected PantryViewModel...");
                _pantryViewModel = new PantryViewModel(_apiService, _authService);
                Console.WriteLine("PantryViewModel created successfully.");

                // Bind the items list to the ViewModel
                PantryItemsList.ItemsSource = _pantryViewModel.FilteredItems;
                Console.WriteLine("ItemsSource bound successfully.");

                // Update stats when ViewModel changes
                _pantryViewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(_pantryViewModel.TotalItems) ||
                        e.PropertyName == nameof(_pantryViewModel.FilteredItemCount) ||
                        e.PropertyName == nameof(_pantryViewModel.ExpiringItemsCount))
                    {
                        UpdateStats();
                    }

                    if (e.PropertyName == nameof(_pantryViewModel.IsBusy))
                    {
                        StatusText.Text = _pantryViewModel.IsBusy ? _pantryViewModel.BusyMessage : GetStatusText();
                    }

                    if (e.PropertyName == nameof(_pantryViewModel.HasErrors))
                    {
                        if (_pantryViewModel.HasErrors)
                        {
                            MessageBox.Show(_pantryViewModel.ErrorMessage, "Pantry Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                };

                Console.WriteLine("PantryViewModel initialized with API integration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing PantryViewModel: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error initializing pantry: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Initialize the recipe ViewModel with API integration
        /// </summary>
        private void InitializeRecipeViewModel()
        {
            try
            {
                if (_apiService == null || _authService == null)
                {
                    Console.WriteLine("Cannot initialize RecipeViewModel - services not available");
                    return;
                }

                Console.WriteLine("Creating API-connected RecipeViewModel...");
                _recipeViewModel = new RecipeViewModel(_apiService, _authService);
                Console.WriteLine("RecipeViewModel created successfully.");

                // Update status when RecipeViewModel changes
                _recipeViewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(_recipeViewModel.IsBusy))
                    {
                        StatusText.Text = _recipeViewModel.IsBusy ? _recipeViewModel.BusyMessage : GetStatusText();
                    }

                    if (e.PropertyName == nameof(_recipeViewModel.HasErrors))
                    {
                        if (_recipeViewModel.HasErrors)
                        {
                            MessageBox.Show(_recipeViewModel.ErrorMessage, "Recipe Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    if (e.PropertyName == nameof(_recipeViewModel.TotalSuggestedRecipes))
                    {
                        UpdateRecipeStats();
                    }
                };

                Console.WriteLine("RecipeViewModel initialized with API integration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing RecipeViewModel: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error initializing recipes: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle authentication state changes
        /// </summary>
        private void OnAuthStateChanged(object? sender, AuthStateChangedEventArgs e)
        {
            Console.WriteLine($"Auth state changed: {(e.IsAuthenticated ? "Logged in" : "Logged out")}");

            // Update UI based on authentication state
            Dispatcher.Invoke(() => UpdateUIForAuthState(e.IsAuthenticated, e.User, e.UserData));
        }

        /// <summary>
        /// Update UI elements based on authentication state
        /// </summary>
        private void UpdateUIForAuthState(bool isAuthenticated, UserProfile? user, UserDto? userData)
        {
            if (isAuthenticated && user != null && userData != null)
            {
                // User logged in
                BtnLogin.Content = $"Welcome, {user.Name}!";
                BtnLogin.IsEnabled = true;
                BtnGetStarted.Content = "My Pantry";
                
                // Show logout button, hide login button
                BtnLogin.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;
                
                // Update header authentication UI
                TxtUserStatus.Text = $"Logged in as {user.Name}";
                BtnHeaderLogout.Visibility = Visibility.Visible;

                // Update window title to include user
                this.Title = $"KitchenWise - {user.Name}'s Kitchen";

                // Initialize ViewModels for the authenticated user
                if (_pantryViewModel == null)
                {
                    InitializePantryViewModel();
                }
                if (_recipeViewModel == null)
                {
                    InitializeRecipeViewModel();
                }

                // Activate ViewModels and load data from API
                if (_pantryViewModel != null)
                {
                    _pantryViewModel.OnActivated();
                }
                if (_recipeViewModel != null)
                {
                    _recipeViewModel.OnActivated();
                }

                // Show pantry view for logged in users
                ShowPantryView();

                StatusText.Text = $"Logged in as {user.Email} | API Connected";
            }
            else
            {
                // User logged out
                BtnLogin.Content = "Sign In";
                BtnLogin.IsEnabled = true;
                BtnGetStarted.Content = "Get Started";
                
                // Show login button, hide logout button
                BtnLogin.Visibility = Visibility.Visible;
                BtnLogout.Visibility = Visibility.Collapsed;
                
                // Update header authentication UI
                TxtUserStatus.Text = "Not logged in";
                BtnHeaderLogout.Visibility = Visibility.Collapsed;

                // Reset window title
                this.Title = "KitchenWise - Kitchen Organization App";

                // Clear ViewModels data when user logs out
                if (_pantryViewModel != null)
                {
                    _pantryViewModel.Cleanup();
                    _pantryViewModel = null;
                    PantryItemsList.ItemsSource = null;
                }
                if (_recipeViewModel != null)
                {
                    _recipeViewModel.Cleanup();
                    _recipeViewModel = null;
                }

                // Show welcome view for non-authenticated users
                ShowWelcomeView();

                StatusText.Text = "Not logged in | API Connected";
            }
        }

        /// <summary>
        /// Get appropriate status text based on current state
        /// </summary>
        private string GetStatusText()
        {
            var apiStatus = _apiService != null ? "API Connected" : "API Disconnected";

            if (_authService?.IsAuthenticated == true)
            {
                return $"Logged in as {_authService.CurrentUser?.Email} | {apiStatus}";
            }
            return $"Not logged in | {apiStatus}";
        }

        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine($"KitchenWise started at {DateTime.Now}");
                BtnLogin.Focus();
                ShowWelcomeMessage();
                TestPantryItems();
                TestMVVMFoundation();

                // Update initial UI state
                UpdateUIForAuthState(false, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MainWindow_Loaded: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error during window load: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle keyboard shortcuts
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                this.Close();
            }

            if (e.Key == Key.Escape)
            {
                this.Close();
            }

            if (e.Key == Key.F1)
            {
                ShowWelcomeView();
            }

            if (e.Key == Key.F2 && _authService?.IsAuthenticated == true)
            {
                ShowPantryView();
            }

            if (e.Key == Key.F5)
            {
                // F5 to refresh pantry data
                _pantryViewModel?.Refresh();
            }
        }

        #region View Navigation

        private void ShowWelcomeView()
        {
            WelcomeView.Visibility = Visibility.Visible;
            PantryView.Visibility = Visibility.Collapsed;
            Console.WriteLine("Switched to Welcome view");
        }

        private void ShowPantryView()
        {
            // Only show pantry if user is authenticated
            if (_authService?.IsAuthenticated != true)
            {
                MessageBox.Show("Please sign in to access your pantry.", "Authentication Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            WelcomeView.Visibility = Visibility.Collapsed;
            PantryView.Visibility = Visibility.Visible;

            // Refresh pantry data when view is shown
            _pantryViewModel?.Refresh();

            Console.WriteLine("Switched to Pantry view");
        }

        private void BtnShowWelcome_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeView();
        }

        private void BtnShowPantry_Click(object sender, RoutedEventArgs e)
        {
            ShowPantryView();
        }

        #endregion

        #region Authentication Events

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_authService == null)
            {
                MessageBox.Show("Authentication service not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_authService.IsAuthenticated)
                {
                    // User is logged in, show logout option
                    var result = MessageBox.Show(
                        $"You are logged in as {_authService.CurrentUser?.Name}.\n\nWould you like to log out?",
                        "Logout Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        BtnLogin.IsEnabled = false;
                        BtnLogin.Content = "Logging out...";

                        await _authService.LogoutAsync();
                        Console.WriteLine("User logged out successfully");
                    }
                }
                else
                {
                    // User not logged in, show login
                    BtnLogin.IsEnabled = false;
                    BtnLogin.Content = "Signing In...";

                    var loginResult = await _authService.LoginAsync();

                    if (loginResult.IsSuccess && loginResult.User != null && loginResult.UserData != null)
                    {
                        Console.WriteLine($"Login successful for: {loginResult.User.Email}");
                        MessageBox.Show(
                            $"Welcome to KitchenWise, {loginResult.User.Name}!\n\n" +
                            $"Your personal pantry is now ready for use.\n" +
                            $"You have {loginResult.UserData.PantryItemCount} items in your pantry.",
                            "Login Successful",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        Console.WriteLine($"Login failed: {loginResult.ErrorMessage}");
                        MessageBox.Show(
                            $"Login failed: {loginResult.ErrorMessage ?? "Unknown error"}\n\n" +
                            "Please check your connection and try again.",
                            "Login Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in login/logout: {ex.Message}");
                MessageBox.Show($"Authentication error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ensure button state is restored
                if (_authService?.IsAuthenticated == true)
                {
                    BtnLogin.Content = $"Welcome, {_authService.CurrentUser?.Name}!";
                }
                else
                {
                    BtnLogin.Content = "Sign In";
                }
                BtnLogin.IsEnabled = true;
            }
        }

        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            await PerformLogout();
        }

        private async void BtnHeaderLogout_Click(object sender, RoutedEventArgs e)
        {
            await PerformLogout();
        }

        /// <summary>
        /// Perform logout with improved error handling and state management
        /// </summary>
        private async Task PerformLogout()
        {
            if (_authService == null)
            {
                MessageBox.Show("Authentication service not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (!_authService.IsAuthenticated)
                {
                    MessageBox.Show("You are not logged in.", "Not Logged In", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Confirm logout
                var result = MessageBox.Show(
                    $"Are you sure you want to sign out?\n\nUser: {_authService.CurrentUser?.Name ?? "Unknown"}",
                    "Confirm Sign Out",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    // Update UI to show logout in progress
                    if (BtnHeaderLogout != null)
                    {
                        BtnHeaderLogout.IsEnabled = false;
                        BtnHeaderLogout.Content = "🔄 Signing out...";
                    }
                    if (BtnLogout != null)
                    {
                        BtnLogout.IsEnabled = false;
                        BtnLogout.Content = "Signing out...";
                    }

                    Console.WriteLine("=== STARTING LOGOUT PROCESS ===");

                    // Perform logout
                    await _authService.LogoutAsync();

                    Console.WriteLine("=== LOGOUT COMPLETED ===");

                    // Force immediate UI update
                    Dispatcher.Invoke(() => {
                        UpdateAuthenticationUI(false, null);
                        UpdateAuthButtons();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                MessageBox.Show($"An error occurred during logout: {ex.Message}\n\nLocal session has been cleared.", "Logout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Force local logout even if Auth0 logout failed
                Dispatcher.Invoke(() => ForceLocalLogout());
            }
            finally
            {
                // Reset button states
                if (BtnHeaderLogout != null)
                {
                    BtnHeaderLogout.IsEnabled = true;
                    BtnHeaderLogout.Content = "🚪 Sign Out";
                }
                if (BtnLogout != null)
                {
                    BtnLogout.IsEnabled = true;
                    BtnLogout.Content = "Sign Out";
                }
            }
        }

        /// <summary>
        /// Force local logout by clearing all authentication state
        /// </summary>
        private void ForceLocalLogout()
        {
            try
            {
                // Clear API authorization
                _apiService?.SetAuthorizationHeader(null);

                // Update UI immediately
                UpdateAuthenticationUI(false, null);

                Console.WriteLine("Forced local logout completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in force logout: {ex.Message}");
            }
        }

        /// <summary>
        /// Update authentication UI elements
        /// </summary>
        private void UpdateAuthenticationUI(bool isAuthenticated, UserProfile? user)
        {
            if (isAuthenticated && user != null)
            {
                // User logged in
                TxtUserStatus.Text = $"Logged in as {user.Name}";
                BtnHeaderLogout.Visibility = Visibility.Visible;
                BtnLogin.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;
                this.Title = $"KitchenWise - {user.Name}'s Kitchen";
            }
            else
            {
                // User logged out
                TxtUserStatus.Text = "Not logged in";
                BtnHeaderLogout.Visibility = Visibility.Collapsed;
                BtnLogin.Visibility = Visibility.Visible;
                BtnLogout.Visibility = Visibility.Collapsed;
                this.Title = "KitchenWise - Kitchen Organization App";
                
                // Show welcome view for logged out users
                ShowWelcomeView();
            }
        }

        /// <summary>
        /// Update authentication button visibility and state
        /// </summary>
        private void UpdateAuthButtons()
        {
            if (_authService?.IsAuthenticated == true)
            {
                BtnLogin.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;
            }
            else
            {
                BtnLogin.Visibility = Visibility.Visible;
                BtnLogout.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnGetStarted_Click(object sender, RoutedEventArgs e)
        {
            if (_authService?.IsAuthenticated == true)
            {
                ShowPantryView();
            }
            else
            {
                // Prompt user to sign in first
                var result = MessageBox.Show(
                    "To get started with KitchenWise, you need to sign in first.\n\nWould you like to sign in now?",
                    "Sign In Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    BtnLogin_Click(sender, e);
                }
            }
        }

        #endregion

        #region Pantry Management Events (now using API-connected ViewModel)

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && _pantryViewModel != null)
            {
                _pantryViewModel.SearchTerm = textBox.Text;
            }
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem && _pantryViewModel != null)
            {
                _pantryViewModel.SelectedCategory = selectedItem.Content.ToString() ?? "All";
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            CategoryFilter.SelectedIndex = 0;
            _pantryViewModel?.ClearSearchCommand.Execute(null);
        }

        private void PantryItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && _pantryViewModel != null)
            {
                _pantryViewModel.SelectedItem = listBox.SelectedItem as PantryItem;

                var hasSelection = _pantryViewModel.SelectedItem != null;
                BtnRemoveItem.IsEnabled = hasSelection;
                BtnConsumeItem.IsEnabled = hasSelection && _pantryViewModel.SelectedItem?.Quantity > 0;
            }
        }

        private async void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (_pantryViewModel == null)
            {
                MessageBox.Show("Please sign in to add items to your pantry.", "Authentication Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var name = NewItemName.Text?.Trim();
                var quantityText = NewItemQuantity.Text?.Trim();
                var unit = (NewItemUnit.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "piece";
                var category = (NewItemCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Other";

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Please enter an item name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewItemName.Focus();
                    return;
                }

                if (!double.TryParse(quantityText, out double quantity) || quantity <= 0)
                {
                    MessageBox.Show("Please enter a valid quantity greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewItemQuantity.Focus();
                    return;
                }

                _pantryViewModel.NewItemName = name;
                _pantryViewModel.NewItemQuantity = quantity;
                _pantryViewModel.NewItemUnit = unit;
                _pantryViewModel.NewItemCategory = category;

                // Use Execute instead of ExecuteAsync for ICommand
                _pantryViewModel.AddItemCommand.Execute(null);

                // Clear form if successful (ViewModel handles this)
                NewItemName.Text = string.Empty;
                NewItemQuantity.Text = "1";
                NewItemUnit.SelectedIndex = 0;
                NewItemCategory.SelectedIndex = 6;

                NewItemName.Focus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding item: {ex.Message}");
                MessageBox.Show($"Failed to add item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (_pantryViewModel?.SelectedItem == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to remove '{_pantryViewModel.SelectedItem.Name}' from your pantry?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                // Use Execute instead of ExecuteAsync for ICommand
                _pantryViewModel.RemoveItemCommand.Execute(null);
            }
        }

        private async void BtnConsumeItem_Click(object sender, RoutedEventArgs e)
        {
            if (_pantryViewModel?.SelectedItem == null) return;

            var result = MessageBox.Show(
                $"Consume 1 unit of '{_pantryViewModel.SelectedItem.Name}'?\n\n" +
                $"Current quantity: {_pantryViewModel.SelectedItem.DisplayQuantity}",
                "Confirm Consumption",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                // Use Execute instead of ExecuteAsync for ICommand
                _pantryViewModel.ConsumeItemCommand.Execute(null);
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateStats()
        {
            if (_pantryViewModel != null)
            {
                var total = _pantryViewModel.TotalItems;
                var filtered = _pantryViewModel.FilteredItemCount;
                var expiring = _pantryViewModel.ExpiringItemsCount;

                StatsText.Text = $"Total Items: {total} | Showing: {filtered} | Expiring: {expiring}";
            }
        }

        private void UpdateRecipeStats()
        {
            if (_recipeViewModel != null)
            {
                var suggested = _recipeViewModel.TotalSuggestedRecipes;
                var favorites = _recipeViewModel.TotalFavoriteRecipes;

                // Update status with recipe info
                var recipeInfo = $"Recipes: {suggested} suggested, {favorites} favorites";
                Console.WriteLine($"Recipe stats updated: {recipeInfo}");
            }
        }

        private void ShowWelcomeMessage()
        {
            Console.WriteLine("====================================");
            Console.WriteLine("    Welcome to KitchenWise!");
            Console.WriteLine("====================================");
            Console.WriteLine("Your smart kitchen organization app");
            Console.WriteLine("Features available:");
            Console.WriteLine("• User authentication with API");
            Console.WriteLine("• Cloud-synchronized pantry management");
            Console.WriteLine("• Add/remove items via API");
            Console.WriteLine("• Real-time search and filter");
            Console.WriteLine("• Expiry tracking");
            Console.WriteLine("====================================");
        }

        private void TestPantryItems()
        {
            var items = new[]
            {
                new Models.PantryItem("Tomatoes", 3, "piece", "Vegetables"),
                new Models.PantryItem("Chicken Breast", 1.5, "lb", "Meat"),
                new Models.PantryItem("Rice", 2, "cup", "Grains")
            };

            Console.WriteLine("=== Sample Pantry Items (Local Models) ===");
            foreach (var item in items)
            {
                Console.WriteLine($"• {item.Name}: {item.DisplayQuantity} ({item.Category})");
                Console.WriteLine($"  Added: {item.DisplayAddedDate}, Status: {item.ExpiryStatus}");
            }
            Console.WriteLine("==========================================");
        }

        private void TestMVVMFoundation()
        {
            Console.WriteLine("=== Testing MVVM Foundation ===");

            var testViewModel = new TestViewModel();
            testViewModel.TestProperty = "Hello MVVM!";
            Console.WriteLine($"ViewModel Property: {testViewModel.TestProperty}");

            var testCommand = new Utilities.RelayCommand(() =>
            {
                Console.WriteLine("RelayCommand executed successfully!");
            });

            if (testCommand.CanExecute(null))
            {
                testCommand.Execute(null);
            }

            Console.WriteLine("MVVM Foundation test completed!");
            Console.WriteLine("===============================");
        }

        #endregion

        #region Recipe Management Events

        private async void BtnGenerateRecipes_Click(object sender, RoutedEventArgs e)
        {
            if (_recipeViewModel == null)
            {
                MessageBox.Show("Please sign in to generate recipes.", "Authentication Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                BtnGenerateRecipes.IsEnabled = false;
                BtnGenerateRecipes.Content = "🔄 Generating...";

                // Execute the generate recipes command and wait for completion
                if (_recipeViewModel.GenerateRecipesCommand.CanExecute(null))
                {
                    // Cast to AsyncRelayCommand to properly await
                    var asyncCommand = _recipeViewModel.GenerateRecipesCommand as AsyncRelayCommand;
                    if (asyncCommand != null)
                    {
                        await asyncCommand.ExecuteAsync(null);
                    }
                    else
                    {
                        _recipeViewModel.GenerateRecipesCommand.Execute(null);
                        // Wait for async operation to complete
                        await Task.Delay(3000);
                    }
                }

                // Show success message with recipe count
                if (_recipeViewModel.HasSuggestedRecipes)
                {
                    MessageBox.Show(
                        $"🎉 Generated {_recipeViewModel.TotalSuggestedRecipes} AI-powered recipe suggestions!\n\n" +
                        $"Based on your pantry items, here are some delicious recipes you can make.\n" +
                        $"Click 'Open Recipe Center' to see full instructions and cook!",
                        "Recipes Generated",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Enable recipe details button if we have recipes
                    BtnShowRecipeDetails.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show(
                        "No recipes were generated. Please make sure you have pantry items added and try again.",
                        "No Recipes Generated",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating recipes: {ex.Message}");
                MessageBox.Show($"Failed to generate recipes: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnGenerateRecipes.IsEnabled = true;
                BtnGenerateRecipes.Content = "🍳 Generate Recipes";
            }
        }

        private void BtnShowRecipeDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_recipeViewModel == null)
            {
                MessageBox.Show("Please sign in to access recipes.", "Authentication Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Open the dedicated Recipe Window
                var recipeWindow = new RecipeWindow(_recipeViewModel, _apiService!, _authService!);
                recipeWindow.Owner = this;
                recipeWindow.ShowDialog();

                Console.WriteLine("Recipe window closed, returning to main window");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening recipe window: {ex.Message}");
                MessageBox.Show($"Failed to open recipe window: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Cleanup

        protected override void OnClosed(EventArgs e)
        {
            Console.WriteLine($"KitchenWise closed at {DateTime.Now}");
            _pantryViewModel?.Cleanup();
            _recipeViewModel?.Cleanup();
            _authService?.LogoutAsync();
            _apiService?.Dispose();
            base.OnClosed(e);
        }

        #endregion

        private class TestViewModel : ViewModels.BaseViewModel
        {
            private string _testProperty = string.Empty;

            public string TestProperty
            {
                get => _testProperty;
                set => SetProperty(ref _testProperty, value);
            }
        }
    }
}