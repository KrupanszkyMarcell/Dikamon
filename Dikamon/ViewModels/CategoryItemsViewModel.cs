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
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadCategoryItemsAsync();
                return;
            }

            try
            {
                IsLoading = true;

                // Search for items of this category
                var response = await _storedItemsApiCommand.GetStoredItemsByTypeIdAndSearch(_userId, CategoryId, SearchText);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    CategoryItems.Clear();
                    foreach (var item in response.Content)
                    {
                        if (item.StoredItem != null)
                        {
                            CategoryItems.Add(item);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Search API call failed: {response.Error?.Content}");
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

        [RelayCommand]
        private async Task AddItem()
        {
            if (_userId == 0)
                return;

            // Show a selection dialog for available items
            string[] itemNames = AvailableItems.Select(i => i.Name).ToArray();

            if (itemNames.Length == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Információ", "Nincsenek elérhető termékek ebben a kategóriában", "OK");
                return;
            }

            string selectedItem = await Application.Current.MainPage.DisplayActionSheet("Válassz terméket", "Mégse", null, itemNames);

            if (selectedItem == "Mégse" || string.IsNullOrEmpty(selectedItem))
                return;

            // Get the selected item
            var item = AvailableItems.FirstOrDefault(i => i.Name == selectedItem);
            if (item == null)
                return;

            // Ask for quantity
            string result = await Application.Current.MainPage.DisplayPromptAsync("Mennyiség",
                $"Add meg a {selectedItem} mennyiségét:",
                initialValue: "1",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(result) || !int.TryParse(result, out int quantity) || quantity <= 0)
                return;

            try
            {
                // Check if the item is already in the storage
                var existingItem = CategoryItems.FirstOrDefault(s => s.ItemId == item.Id);

                if (existingItem != null)
                {
                    // Update existing item quantity
                    existingItem.Quantity += quantity;
                    await _storedItemsApiCommand.AddStoredItem(existingItem);
                }
                else
                {
                    // Add new item to storage
                    var newStoredItem = new Stores
                    {
                        UserId = _userId,
                        ItemId = item.Id,
                        Quantity = quantity,
                        StoredItem = item
                    };

                    var response = await _storedItemsApiCommand.AddStoredItem(newStoredItem);

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        newStoredItem.Id = response.Content.Id;
                        CategoryItems.Add(newStoredItem);
                    }
                }

                await Application.Current.MainPage.DisplayAlert("Siker", $"{quantity} {selectedItem} hozzáadva a konyhádhoz", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült hozzáadni a terméket", "OK");
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