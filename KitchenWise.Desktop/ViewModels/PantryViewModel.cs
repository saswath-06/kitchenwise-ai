using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using KitchenWise.Desktop.Models;
using KitchenWise.Desktop.Services;
using KitchenWise.Desktop.Utilities;

namespace KitchenWise.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for managing pantry items with API integration
    /// Handles adding, editing, removing, and searching pantry items via backend API
    /// </summary>
    public class PantryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;

        private ObservableCollection<PantryItem> _pantryItems;
        private ObservableCollection<PantryItem> _filteredItems;
        private PantryItem? _selectedItem;
        private string _searchTerm = string.Empty;
        private string _selectedCategory = "All";
        private string _newItemName = string.Empty;
        private double _newItemQuantity = 1.0;
        private string _newItemUnit = "piece";
        private string _newItemCategory = "Other";

        public PantryViewModel(ApiService apiService, AuthService authService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _pantryItems = new ObservableCollection<PantryItem>();
            _filteredItems = new ObservableCollection<PantryItem>();

            InitializeCommands();

            Console.WriteLine("PantryViewModel initialized with API integration");
        }

        #region Properties

        /// <summary>
        /// All pantry items (backing collection)
        /// </summary>
        public ObservableCollection<PantryItem> PantryItems
        {
            get => _pantryItems;
            set => SetProperty(ref _pantryItems, value);
        }

        /// <summary>
        /// Filtered pantry items for display (based on search and category filter)
        /// </summary>
        public ObservableCollection<PantryItem> FilteredItems
        {
            get => _filteredItems;
            set => SetProperty(ref _filteredItems, value);
        }

        /// <summary>
        /// Currently selected pantry item
        /// </summary>
        public PantryItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnSelectedItemChanged();
                }
            }
        }

        /// <summary>
        /// Search term for filtering items
        /// </summary>
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    _ = RefreshFilteredItemsAsync();
                }
            }
        }

        /// <summary>
        /// Selected category for filtering
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = RefreshFilteredItemsAsync();
                }
            }
        }

        // New Item Properties
        public string NewItemName
        {
            get => _newItemName;
            set => SetProperty(ref _newItemName, value);
        }

        public double NewItemQuantity
        {
            get => _newItemQuantity;
            set => SetProperty(ref _newItemQuantity, value);
        }

        public string NewItemUnit
        {
            get => _newItemUnit;
            set => SetProperty(ref _newItemUnit, value);
        }

        public string NewItemCategory
        {
            get => _newItemCategory;
            set => SetProperty(ref _newItemCategory, value);
        }

        /// <summary>
        /// Available categories for filtering and new items
        /// </summary>
        public string[] Categories => new[] { "All" }.Concat(PantryCategories.AllCategories).ToArray();

        /// <summary>
        /// Available measurement units
        /// </summary>
        public string[] AvailableUnits => Models.MeasurementUnits.AllUnits;

        /// <summary>
        /// Total number of items in pantry
        /// </summary>
        public int TotalItems => PantryItems.Count;

        /// <summary>
        /// Number of items displayed after filtering
        /// </summary>
        public int FilteredItemCount => FilteredItems.Count;

        /// <summary>
        /// Items that are expired or expiring soon
        /// </summary>
        public int ExpiringItemsCount => PantryItems.Count(item => item.IsExpired || item.IsExpiringSoon);

        #endregion

        #region Commands

        public ICommand AddItemCommand { get; private set; } = null!;
        public ICommand RemoveItemCommand { get; private set; } = null!;
        public ICommand EditItemCommand { get; private set; } = null!;
        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ConsumeItemCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            AddItemCommand = new AsyncRelayCommand(AddItemAsync, CanAddItem);
            RemoveItemCommand = new AsyncRelayCommand(RemoveSelectedItemAsync, () => SelectedItem != null);
            EditItemCommand = new RelayCommand(EditSelectedItem, () => SelectedItem != null);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrEmpty(SearchTerm));
            RefreshCommand = new AsyncRelayCommand(LoadPantryItemsAsync);
            ConsumeItemCommand = new AsyncRelayCommand(ConsumeSelectedItemAsync, () => SelectedItem != null && SelectedItem.Quantity > 0);
        }

        #endregion

        #region API Integration

        /// <summary>
        /// Load pantry items from API
        /// </summary>
        public async Task LoadPantryItemsAsync()
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUserData == null)
            {
                Log("Cannot load pantry items - user not authenticated", "WARN");
                return;
            }

            try
            {
                SetBusyState(true, "Loading pantry items...");

                var userId = _authService.CurrentUserData.Id;
                Log($"Loading pantry items for user: {userId}");

                var apiItems = await _apiService.GetUserPantryItemsAsync(userId);

                if (apiItems != null)
                {
                    // Convert API DTOs to local models
                    PantryItems.Clear();

                    foreach (var apiItem in apiItems)
                    {
                        var localItem = ConvertFromApiDto(apiItem);
                        PantryItems.Add(localItem);
                    }

                    Log($"Loaded {apiItems.Length} items from API");
                }
                else
                {
                    Log("Failed to load pantry items from API", "ERROR");
                    SetErrorState(true, "Failed to load pantry items. Please check your connection.");
                }

                await RefreshFilteredItemsAsync();
                UpdateCounts();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to load pantry items");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        /// <summary>
        /// Convert API DTO to local model
        /// </summary>
        private PantryItem ConvertFromApiDto(PantryItemDto dto)
        {
            return new PantryItem
            {
                Id = (int)dto.Id.GetHashCode(), // Simple conversion for local ID
                Name = dto.Name,
                Quantity = dto.Quantity,
                Unit = dto.Unit,
                Category = dto.Category,
                AddedDate = dto.AddedDate,
                ExpiryDate = dto.ExpiryDate
            };
        }

        /// <summary>
        /// Convert local model to API DTO for creation
        /// </summary>
        private CreatePantryItemRequest ConvertToCreateRequest(string name, double quantity, string unit, string category)
        {
            return new CreatePantryItemRequest
            {
                UserId = _authService.CurrentUserData?.Id ?? Guid.Empty,
                Name = name,
                Quantity = quantity,
                Unit = unit,
                Category = category,
                ExpiryDate = null // Could be extended to support expiry dates
            };
        }

        #endregion

        #region Command Implementations

        private bool CanAddItem()
        {
            return !string.IsNullOrWhiteSpace(NewItemName) &&
                   NewItemQuantity > 0 &&
                   !string.IsNullOrWhiteSpace(NewItemUnit) &&
                   _authService.IsAuthenticated;
        }

        private async Task AddItemAsync()
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUserData == null)
            {
                SetErrorState(true, "Please sign in to add items to your pantry");
                return;
            }

            try
            {
                SetBusyState(true, "Adding item to pantry...");
                ClearErrors();

                var request = ConvertToCreateRequest(
                    NewItemName.Trim(),
                    NewItemQuantity,
                    NewItemUnit,
                    NewItemCategory
                );

                var apiItem = await _apiService.AddPantryItemAsync(request);

                if (apiItem != null)
                {
                    // Add to local collection
                    var localItem = ConvertFromApiDto(apiItem);
                    PantryItems.Add(localItem);

                    await RefreshFilteredItemsAsync();

                    // Clear the form
                    NewItemName = string.Empty;
                    NewItemQuantity = 1.0;
                    NewItemUnit = "piece";
                    NewItemCategory = "Other";

                    Log($"Added new item: {apiItem.Name}");
                    UpdateCounts();
                }
                else
                {
                    SetErrorState(true, "Failed to add item to pantry");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to add item to pantry");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task RemoveSelectedItemAsync()
        {
            if (SelectedItem == null || !_authService.IsAuthenticated)
                return;

            try
            {
                SetBusyState(true, "Removing item from pantry...");

                // Find corresponding API item ID (for now, we'll need to search by name)
                // In a real implementation, we'd store the API ID in our local model
                var apiItems = await _apiService.GetUserPantryItemsAsync(_authService.CurrentUserData!.Id);
                var apiItem = apiItems?.FirstOrDefault(item => item.Name == SelectedItem.Name);

                if (apiItem != null)
                {
                    var success = await _apiService.DeletePantryItemAsync(apiItem.Id);

                    if (success)
                    {
                        var itemName = SelectedItem.Name;
                        PantryItems.Remove(SelectedItem);
                        await RefreshFilteredItemsAsync();
                        SelectedItem = null;

                        Log($"Removed item: {itemName}");
                        UpdateCounts();
                    }
                    else
                    {
                        SetErrorState(true, "Failed to remove item from pantry");
                    }
                }
                else
                {
                    SetErrorState(true, "Item not found in server database");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to remove item from pantry");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void EditSelectedItem()
        {
            if (SelectedItem == null) return;

            // For now, just log that edit was requested
            Log($"Edit requested for item: {SelectedItem.Name}");

            // TODO: Implement edit functionality with API integration
            // This could open a dialog or switch to edit mode
        }

        private async Task ConsumeSelectedItemAsync()
        {
            if (SelectedItem == null || SelectedItem.Quantity <= 0 || !_authService.IsAuthenticated)
                return;

            try
            {
                SetBusyState(true, "Consuming item...");

                // Find corresponding API item
                var apiItems = await _apiService.GetUserPantryItemsAsync(_authService.CurrentUserData!.Id);
                var apiItem = apiItems?.FirstOrDefault(item => item.Name == SelectedItem.Name);

                if (apiItem != null)
                {
                    var consumeAmount = Math.Min(1.0, SelectedItem.Quantity);
                    var updatedItem = await _apiService.ConsumePantryItemAsync(apiItem.Id, consumeAmount);

                    if (updatedItem != null)
                    {
                        // Update local item
                        SelectedItem.ConsumeQuantity(consumeAmount);
                        Log($"Consumed {consumeAmount} of {SelectedItem.Name}, {SelectedItem.Quantity} remaining");
                    }
                    else
                    {
                        // Item was completely consumed and removed
                        Log($"Item consumed completely: {SelectedItem.Name}");
                        PantryItems.Remove(SelectedItem);
                        await RefreshFilteredItemsAsync();
                        SelectedItem = null;
                    }

                    UpdateCounts();
                }
                else
                {
                    SetErrorState(true, "Item not found in server database");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to consume item");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void ClearSearch()
        {
            SearchTerm = string.Empty;
            SelectedCategory = "All";
        }

        #endregion

        #region Helper Methods

        private async Task RefreshFilteredItemsAsync()
        {
            try
            {
                FilteredItems.Clear();

                if (!_authService.IsAuthenticated || _authService.CurrentUserData == null)
                {
                    return;
                }

                // Use API search if we have search criteria
                if (!string.IsNullOrWhiteSpace(SearchTerm) || (SelectedCategory != "All"))
                {
                    var searchCategory = SelectedCategory == "All" ? null : SelectedCategory;
                    var searchResults = await _apiService.SearchUserPantryItemsAsync(
                        _authService.CurrentUserData.Id,
                        SearchTerm,
                        searchCategory
                    );

                    if (searchResults != null)
                    {
                        foreach (var apiItem in searchResults)
                        {
                            var localItem = ConvertFromApiDto(apiItem);
                            FilteredItems.Add(localItem);
                        }
                    }
                }
                else
                {
                    // No filters, show all local items
                    var sortedItems = PantryItems.OrderBy(item => item.Name);
                    foreach (var item in sortedItems)
                    {
                        FilteredItems.Add(item);
                    }
                }

                LogDebug($"Filtered items: {FilteredItems.Count} of {PantryItems.Count}");
            }
            catch (Exception ex)
            {
                Log($"Error refreshing filtered items: {ex.Message}", "ERROR");
            }
        }

        private void OnSelectedItemChanged()
        {
            LogDebug($"Selected item: {SelectedItem?.Name ?? "None"}");

            // Trigger command re-evaluation
            (RemoveItemCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (EditItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ConsumeItemCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(FilteredItemCount));
            OnPropertyChanged(nameof(ExpiringItemsCount));
        }

        #endregion

        #region Overrides

        public override async void OnActivated()
        {
            base.OnActivated();
            Log("PantryViewModel activated - loading items from API");
            await LoadPantryItemsAsync();
        }

        public override async void Refresh()
        {
            base.Refresh();
            Log("Refreshing pantry data from API");
            await LoadPantryItemsAsync();
        }

        public override bool Validate()
        {
            ClearErrors();

            if (!_authService.IsAuthenticated)
            {
                SetErrorState(true, "Please sign in to access your pantry");
                return false;
            }

            return !HasErrors;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            PantryItems.Clear();
            FilteredItems.Clear();
            Log("PantryViewModel cleaned up");
        }

        #endregion
    }
}