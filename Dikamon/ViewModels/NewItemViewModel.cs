using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Api;
using Dikamon.Models;

namespace Dikamon.ViewModels
{
    [QueryProperty(nameof(RefreshRequired), "refresh")]
    public partial class NewItemViewModel : ObservableObject
    {
        private readonly IItemsApiCommand _itemsApiCommand;
        private readonly IItemTypesApiCommand _itemTypesApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;

        [ObservableProperty]
        private ObservableCollection<ItemTypes> _itemTypes;

        [ObservableProperty]
        private ObservableCollection<Items> _availableItems;

        [ObservableProperty]
        private ItemTypes _selectedItemType;

        [ObservableProperty]
        private Items _selectedItem;

        [ObservableProperty]
        private string _selectedItemImageSource;

        [ObservableProperty]
        private string _itemUnit;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDecrement))]
        private int _quantity = 1;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isItemSelectionEnabled;

        [ObservableProperty]
        private bool _isItemSelected;

        [ObservableProperty]
        private int _categoryId;

        [ObservableProperty]
        private string _categoryName;

        [ObservableProperty]
        private bool _refreshRequired;

        private int _userId;

        public bool CanDecrement => Quantity > 1;

        public NewItemViewModel(
            IItemsApiCommand itemsApiCommand,
            IItemTypesApiCommand itemTypesApiCommand,
            IStoredItemsApiCommand storedItemsApiCommand)
        {
            _itemsApiCommand = itemsApiCommand;
            _itemTypesApiCommand = itemTypesApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            ItemTypes = new ObservableCollection<ItemTypes>();
            AvailableItems = new ObservableCollection<Items>();

            LoadUserIdAsync();
        }

        private async void LoadUserIdAsync()
        {
            try
            {
                var userJson = await SecureStorage.GetAsync("user");
                System.Diagnostics.Debug.WriteLine($"User JSON from secure storage: {(userJson != null ? "Retrieved" : "Not found")}");

                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                        System.Diagnostics.Debug.WriteLine($"User ID loaded: {_userId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User or User.Id is null after deserialization");
                    }
                }
                else
                {
                    // Fallback: Try to get user ID from a different source
                    var userIdStr = await SecureStorage.GetAsync("userId");
                    if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                    {
                        _userId = userId;
                        System.Diagnostics.Debug.WriteLine($"User ID loaded from userId key: {_userId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user ID: {ex.Message}");
            }
        }

        public async Task Initialize(int categoryId, string categoryName)
        {
            CategoryId = categoryId;
            CategoryName = categoryName;

            IsLoading = true;

            try
            {
                await Task.WhenAll(
                    LoadItemTypesAsync(),
                    LoadItemsByCategoryAsync(categoryId)
                );

                // Pre-select the category if it was provided
                if (categoryId > 0)
                {
                    var matchingType = ItemTypes.FirstOrDefault(t => t.Id == categoryId);
                    if (matchingType != null)
                    {
                        SelectedItemType = matchingType;
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedItemTypeChanged(ItemTypes value)
        {
            if (value != null)
            {
                LoadItemsByCategoryAsync(value.Id);
            }
            else
            {
                AvailableItems.Clear();
            }

            // Reset the selected item when type changes
            SelectedItem = null;
            IsItemSelectionEnabled = (value != null);
        }

        partial void OnSelectedItemChanged(Items value)
        {
            IsItemSelected = (value != null);

            if (value != null)
            {
                // Set the image source
                SelectedItemImageSource = !string.IsNullOrEmpty(value.Image)
                    ? value.Image
                    : "placeholder_food.png";

                // Set the unit
                ItemUnit = !string.IsNullOrEmpty(value.Unit)
                    ? value.Unit
                    : "db";
            }
            else
            {
                SelectedItemImageSource = null;
                ItemUnit = null;
            }
        }

        private async Task LoadItemTypesAsync()
        {
            try
            {
                var response = await _itemTypesApiCommand.GetItemTypes();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    ItemTypes.Clear();
                    foreach (var type in response.Content)
                    {
                        ItemTypes.Add(type);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {ItemTypes.Count} item types");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load item types: {response.Error?.Content}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading item types: {ex.Message}");
            }
        }

        private async Task LoadItemsByCategoryAsync(int categoryId)
        {
            try
            {
                var response = await _itemsApiCommand.GetItemsByTypeId(categoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableItems.Clear();
                    foreach (var item in response.Content)
                    {
                        AvailableItems.Add(item);
                    }
                    IsItemSelectionEnabled = true;
                    System.Diagnostics.Debug.WriteLine($"Loaded {AvailableItems.Count} items for category {categoryId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load items: {response.Error?.Content}");
                    IsItemSelectionEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
                IsItemSelectionEnabled = false;
            }
        }

        [RelayCommand]
        private void IncrementQuantity()
        {
            Quantity++;
        }

        [RelayCommand]
        private void DecrementQuantity()
        {
            if (Quantity > 1)
            {
                Quantity--;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (SelectedItem == null || _userId == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "Kérjük, válasszon élelmiszert", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                // Check if the item already exists in the user's storage
                var storedItemsResponse = await _storedItemsApiCommand.GetStoredItems(_userId);

                if (storedItemsResponse.IsSuccessStatusCode && storedItemsResponse.Content != null)
                {
                    var existingStoredItem = storedItemsResponse.Content
                        .FirstOrDefault(s => s.ItemId == SelectedItem.Id);

                    if (existingStoredItem != null)
                    {
                        // Update the existing item's quantity
                        existingStoredItem.Quantity += Quantity;
                        var updateResponse = await _storedItemsApiCommand.AddStoredItem(existingStoredItem);

                        if (updateResponse.IsSuccessStatusCode)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                            "Siker",
                            $"A mennyiség frissítve: {SelectedItem.Name} - {existingStoredItem.Quantity} {SelectedItem.Unit ?? "db"}",
                                "OK");
                        }
                        else
                        {
                            throw new Exception("Failed to update item quantity");
                        }
                    }
                    else
                    {
                        // Add new stored item
                        var newStoredItem = new Stores
                        {
                            UserId = _userId,
                            ItemId = SelectedItem.Id,
                            Quantity = Quantity,
                            StoredItem = SelectedItem
                        };

                        var addResponse = await _storedItemsApiCommand.AddStoredItem(newStoredItem);

                        if (addResponse.IsSuccessStatusCode)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                            "Siker",
                            $"Termék hozzáadva: {SelectedItem.Name} - {Quantity} {SelectedItem.Unit ?? "db"}",
                                "OK");
                        }
                        else
                        {
                            throw new Exception("Failed to add new item");
                        }
                    }

                    // Refresh the category page after adding an item
                    var navigationParameter = new Dictionary<string, object>
                    {
                        { "refresh", true }
                    };

                    // Navigate back
                    await Shell.Current.GoToAsync("..", navigationParameter);
                }
                else
                {
                    throw new Exception("Failed to check existing items");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült menteni a terméket", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}