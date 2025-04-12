using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Api;
using Dikamon.Models;
using Dikamon.Pages;

namespace Dikamon.ViewModels
{
    [QueryProperty(nameof(RefreshRequired), "refresh")]
    public partial class CategoryItemsViewModel : ObservableObject
    {
        private readonly IItemsApiCommand _itemsApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;
        private readonly IItemTypesApiCommand _itemTypesApiCommand;

        [ObservableProperty]
        private string _categoryName;

        [ObservableProperty]
        private int _categoryId;

        [ObservableProperty]
        private ObservableCollection<Stores> _categoryItems;

        [ObservableProperty]
        private ObservableCollection<Items> _availableItems;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _refreshRequired = false;

        private int _userId;
        private bool _isInitialized = false;

        public CategoryItemsViewModel(IItemsApiCommand itemsApiCommand,
                                    IStoredItemsApiCommand storedItemsApiCommand,
                                    IItemTypesApiCommand itemTypesApiCommand)
        {
            _itemsApiCommand = itemsApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;
            _itemTypesApiCommand = itemTypesApiCommand;

            CategoryItems = new ObservableCollection<Stores>();
            AvailableItems = new ObservableCollection<Items>();
        }

        partial void OnRefreshRequiredChanged(bool value)
        {
            if (value && _isInitialized)
            {
                // Refresh data when coming back from another page
                System.Diagnostics.Debug.WriteLine("Refresh required detected, reloading data...");
                LoadCategoryItemsAsync().ConfigureAwait(false);
            }
        }

        public async Task Initialize(string categoryName, int categoryId)
        {
            if (_isInitialized && categoryName == CategoryName && categoryId == CategoryId)
            {
                System.Diagnostics.Debug.WriteLine($"Already initialized with the same category: {categoryName}, {categoryId}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Initializing CategoryItemsViewModel with category: {categoryName}, id: {categoryId}");

            CategoryName = categoryName;
            CategoryId = categoryId;

            _isInitialized = false;

            await LoadUserIdAsync();

            if (_userId == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load user ID, cannot proceed with initialization");
                await Application.Current.MainPage.DisplayAlert("Figyelmeztetés", "A felhasználói adatok betöltése sikertelen. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            await Task.WhenAll(
                LoadCategoryItemsAsync(),
                LoadAvailableItemsAsync()
            );

            _isInitialized = true;
        }

        private async Task LoadUserIdAsync()
        {
            try
            {
                // Try to get user directly from SecureStorage
                var userJson = await SecureStorage.GetAsync("user");
                System.Diagnostics.Debug.WriteLine($"User JSON from secure storage: {(userJson != null ? "Retrieved" : "Not found")}");

                if (!string.IsNullOrEmpty(userJson))
                {
                    try
                    {
                        var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                        if (user != null && user.Id.HasValue)
                        {
                            _userId = user.Id.Value;
                            System.Diagnostics.Debug.WriteLine($"User ID loaded: {_userId}");
                            return;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("User or User.Id is null after deserialization");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializing user JSON: {ex.Message}");
                    }
                }

                // Fallback: Try to get user ID from a different source if available
                var userIdStr = await SecureStorage.GetAsync("userId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    _userId = userId;
                    System.Diagnostics.Debug.WriteLine($"User ID loaded from userId key: {_userId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to load user ID from any source");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user ID: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadCategoryItemsAsync()
        {
            if (IsLoading)
                return;

            if (_userId == 0)
            {
                System.Diagnostics.Debug.WriteLine("Cannot load category items: User ID is 0");
                return;
            }

            if (CategoryId == 0)
            {
                System.Diagnostics.Debug.WriteLine("Cannot load category items: Category ID is 0");
                return;
            }

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"Loading stored items for user {_userId} and category {CategoryId}");

                // First try to get stored items filtered by category type
                var response = await _storedItemsApiCommand.GetStoredItemsByTypeId(_userId, CategoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    CategoryItems.Clear();
                    System.Diagnostics.Debug.WriteLine($"Received {response.Content.Count} items");
                    foreach (var item in response.Content)
                    {
                        // Make sure the StoredItem property is populated
                        if (item.StoredItem == null && item.ItemId > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"StoredItem is null for item ID {item.ItemId}, trying to load item details");
                            try
                            {
                                var itemsResponse = await _itemsApiCommand.GetItemsByTypeId(CategoryId);
                                if (itemsResponse.IsSuccessStatusCode && itemsResponse.Content != null)
                                {
                                    var matchingItem = itemsResponse.Content.FirstOrDefault(i => i.Id == item.ItemId);
                                    if (matchingItem != null)
                                    {
                                        item.StoredItem = matchingItem;
                                        System.Diagnostics.Debug.WriteLine($"Populated item details for ID {item.ItemId}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error loading item details: {ex.Message}");
                            }
                        }

                        if (item.StoredItem != null)
                        {
                            CategoryItems.Add(item);
                            System.Diagnostics.Debug.WriteLine($"Added item to CategoryItems: {item.StoredItem.Name}, Quantity: {item.Quantity}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping item with ID {item.ItemId} because StoredItem is null");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed: {response.Error?.Content}");
                    if (response.Error != null)
                    {
                        var errorContent = response.Error.Content;
                        System.Diagnostics.Debug.WriteLine($"Error content: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading category items: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a kategória elemeit", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task LoadAvailableItemsAsync()
        {
            if (IsLoading || CategoryId == 0)
                return;

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"Loading available items for category {CategoryId}");

                // Get all items for this category
                var response = await _itemsApiCommand.GetItemsByTypeId(CategoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableItems.Clear();
                    System.Diagnostics.Debug.WriteLine($"Received {response.Content.Count} available items");
                    foreach (var item in response.Content)
                    {
                        AvailableItems.Add(item);
                        System.Diagnostics.Debug.WriteLine($"Added available item: {item.Name}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"API call failed: {response.Error?.Content}");
                    if (response.Error != null)
                    {
                        var errorContent = response.Error.Content;
                        System.Diagnostics.Debug.WriteLine($"Error content: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available items: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;
            await LoadUserIdAsync();
            await Task.WhenAll(
                LoadCategoryItemsAsync(),
                LoadAvailableItemsAsync()
            );
        }

        [RelayCommand]
        private async Task Search()
        {
            System.Diagnostics.Debug.WriteLine($"Search initiated with text: '{SearchText}'");

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // If search text is empty, just reload all items
                await LoadCategoryItemsAsync();
                return;
            }

            try
            {
                IsLoading = true;

                if (_userId == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot search items: User ID is 0");
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Felhasználói azonosító nem található", "OK");
                    return;
                }

                // First, try to load all items for this category if we don't have them yet
                if (CategoryItems.Count == 0)
                {
                    await LoadCategoryItemsAsync();
                }

                // Try category-specific API search first
                System.Diagnostics.Debug.WriteLine($"Calling typed API search for '{SearchText}' in category {CategoryId} for user {_userId}");
                var typedResponse = await _storedItemsApiCommand.GetStoredItemsByTypeIdAndSearch(_userId, CategoryId, SearchText);

                var apiSearchResults = new List<Stores>();

                // Process typed search results
                if (typedResponse.IsSuccessStatusCode && typedResponse.Content != null && typedResponse.Content.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Typed API search returned {typedResponse.Content.Count} results");

                    foreach (var item in typedResponse.Content)
                    {
                        PopulateStoredItem(item);
                        if (item.StoredItem != null)
                        {
                            apiSearchResults.Add(item);
                        }
                    }
                }
                // If typed search returned no results, try general search
                else if (typedResponse.IsSuccessStatusCode) // API call succeeded but returned no items
                {
                    System.Diagnostics.Debug.WriteLine($"Typed API search returned no results, trying general search");

                    // Try the general search API
                    var generalResponse = await _storedItemsApiCommand.GetStoredItemsBySearch(_userId, SearchText);

                    if (generalResponse.IsSuccessStatusCode && generalResponse.Content != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"General API search returned {generalResponse.Content.Count} results, filtering for current category");

                        // Filter the general results to only include items from our category
                        foreach (var item in generalResponse.Content)
                        {
                            PopulateStoredItem(item);

                            // Check if the item belongs to our category
                            if (item.StoredItem != null)
                            {
                                // First, check if we can determine the category from the stored item
                                bool isInCategory = item.StoredItem.TypeId == CategoryId;

                                // If we can't determine from the stored item, check available items
                                if (!isInCategory)
                                {
                                    var availableItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                                    isInCategory = availableItem != null && availableItem.TypeId == CategoryId;
                                }

                                if (isInCategory)
                                {
                                    apiSearchResults.Add(item);
                                    System.Diagnostics.Debug.WriteLine($"Added item from general search: {item.StoredItem.Name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"General API search failed: {generalResponse.Error?.Content}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Typed API search failed: {typedResponse.Error?.Content}");
                }

                // Helper method to populate StoredItem if null
                void PopulateStoredItem(Stores item)
                {
                    if (item.StoredItem == null && item.ItemId > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"StoredItem is null for item ID {item.ItemId}, trying to find it in available items");
                        var matchingItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                        if (matchingItem != null)
                        {
                            item.StoredItem = matchingItem;
                        }
                    }
                }

                // If API search found items, use those results
                // Otherwise, filter locally to handle partial matches the API might have missed
                List<Stores> searchResults;

                if (apiSearchResults.Count > 0)
                {
                    searchResults = apiSearchResults;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("API searches returned no results, filtering locally");
                    // Perform a local search on the already loaded items
                    searchResults = CategoryItems
                        .Where(item =>
                            item.StoredItem != null &&
                            (item.StoredItem.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                             (item.StoredItem.Name_EN != null && item.StoredItem.Name_EN.Contains(SearchText, StringComparison.OrdinalIgnoreCase))))
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"Local search found {searchResults.Count} results");
                }

                // Clear and repopulate the collection
                CategoryItems.Clear();

                foreach (var item in searchResults)
                {
                    CategoryItems.Add(item);
                    System.Diagnostics.Debug.WriteLine($"Added search result: {item.StoredItem?.Name}");
                }

                if (CategoryItems.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No items found matching the search criteria");
                    await Application.Current.MainPage.DisplayAlert("Információ", $"Nincs találat a keresésre: '{SearchText}'", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching items: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "A keresés nem sikerült", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Modified AddItem method to navigate to the new item page
        [RelayCommand]
        private async Task AddItem()
        {
            if (_userId == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "A felhasználói adatok nem elérhetők. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            try
            {
                // Add debug information
                System.Diagnostics.Debug.WriteLine($"[TRACE] Navigating to NewItemPage with categoryId: {CategoryId}, categoryName: {CategoryName}");

                // Double-check that CategoryId is valid
                if (CategoryId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Invalid CategoryId: {CategoryId}");
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Érvénytelen kategória azonosító", "OK");
                    return;
                }

                // Ensure the categoryId is passed as a string
                string categoryIdStr = CategoryId.ToString();
                System.Diagnostics.Debug.WriteLine($"[TRACE] Converted CategoryId to string: {categoryIdStr}");

                // Navigate to the NewItemPage with the category information
                var navigationParameter = new Dictionary<string, object>
                {
                    { "categoryId", categoryIdStr },
                    { "categoryName", CategoryName }
                };

                await Shell.Current.GoToAsync(nameof(NewItemPage), navigationParameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error navigating to NewItemPage: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült megnyitni az új termék oldalt", "OK");
            }
        }

        [RelayCommand]
        private async Task Increment(Stores item)
        {
            if (item == null || _userId == 0)
                return;

            try
            {
                item.Quantity += 1;
                var response = await _storedItemsApiCommand.AddStoredItem(item);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API error when incrementing: {response.Error?.Content}");
                    item.Quantity -= 1; // Revert the change on API error
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült növelni a mennyiséget", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error incrementing item: {ex.Message}");
                item.Quantity -= 1; // Revert the change
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült növelni a mennyiséget", "OK");
            }
        }

        [RelayCommand]
        private async Task Decrement(Stores item)
        {
            if (item == null || _userId == 0 || item.Quantity <= 0)
                return;

            try
            {
                if (item.Quantity == 1)
                {
                    // Ask for confirmation before removing
                    bool remove = await Application.Current.MainPage.DisplayAlert(
                        "Megerősítés",
                        $"Biztosan eltávolítod a {item.StoredItem?.Name} terméket a konyhádból?",
                        "Igen", "Nem");

                    if (remove)
                    {
                        var response = await _storedItemsApiCommand.DeleteStoredItem(item);
                        if (response.IsSuccessStatusCode)
                        {
                            CategoryItems.Remove(item);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"API error when deleting: {response.Error?.Content}");
                            await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült törölni a terméket", "OK");
                        }
                    }
                }
                else
                {
                    item.Quantity -= 1;
                    var response = await _storedItemsApiCommand.AddStoredItem(item);

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"API error when decrementing: {response.Error?.Content}");
                        item.Quantity += 1; // Revert the change on API error
                        await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült csökkenteni a mennyiséget", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrementing item: {ex.Message}");
                if (item.Quantity > 0) item.Quantity += 1; // Revert the change if we decremented
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült csökkenteni a mennyiséget", "OK");
            }
        }
    }
}