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
            if (SelectedItem == null)
            {
                await Application.Current?.MainPage?.DisplayAlert("Hiba", "Kérjük, válasszon élelmiszert", "OK");
                return;
            }

            if (_userId == 0)
            {
                System.Diagnostics.Debug.WriteLine("User ID is 0 or not set - attempting to reload user ID");
                await ReloadUserIdAsync();

                if (_userId == 0)
                {
                    await Application.Current?.MainPage?.DisplayAlert("Hiba", "A felhasználói azonosító hiányzik. Kérjük, jelentkezzen be újra.", "OK");
                    return;
                }
            }

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"Attempting to save item: {SelectedItem.Name} (ID: {SelectedItem.Id}) with quantity {Quantity} for user {_userId}");

                // First create a new stored item - don't check if it exists, as the API will handle this
                var newStoredItem = new Stores
                {
                    UserId = _userId,
                    ItemId = SelectedItem.Id,
                    Quantity = Quantity,
                    StoredItem = SelectedItem
                };

                System.Diagnostics.Debug.WriteLine($"Created new stored item object: UserId={newStoredItem.UserId}, ItemId={newStoredItem.ItemId}, Quantity={newStoredItem.Quantity}");

                // Add the item directly - the API will handle updating if it already exists
                var addResponse = await _storedItemsApiCommand.AddStoredItem(newStoredItem);

                if (addResponse == null)
                {
                    System.Diagnostics.Debug.WriteLine("API response is null - likely network or service issue");
                    throw new Exception("API response is null");
                }

                if (addResponse.IsSuccessStatusCode && addResponse.Content != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully added/updated item. Response: {System.Text.Json.JsonSerializer.Serialize(addResponse.Content)}");

                    await Application.Current?.MainPage?.DisplayAlert(
                        "Siker",
                        $"Termék hozzáadva/frissítve: {SelectedItem.Name} - {Quantity} {SelectedItem.Unit ?? "db"}",
                        "OK");

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
                    var errorContent = addResponse.Error?.Content ?? "Unknown error";
                    System.Diagnostics.Debug.WriteLine($"API call failed with status {addResponse.StatusCode}. Error: {errorContent}");
                    throw new Exception($"Failed to add item: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving item: {ex.Message}");
                await Application.Current?.MainPage?.DisplayAlert("Hiba", $"Nem sikerült menteni a terméket: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Helper method to reload user ID
        private async Task ReloadUserIdAsync()
        {
            try
            {
                // Try multiple sources to get the user ID
                var userJson = await SecureStorage.GetAsync("user");
                if (!string.IsNullOrEmpty(userJson))
                {
                    try
                    {
                        var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                        if (user != null && user.Id.HasValue)
                        {
                            _userId = user.Id.Value;
                            System.Diagnostics.Debug.WriteLine($"Reloaded User ID from user JSON: {_userId}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializing user JSON: {ex.Message}");
                    }
                }

                // Try to get user ID directly from stored string
                var userIdStr = await SecureStorage.GetAsync("userId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    _userId = userId;
                    System.Diagnostics.Debug.WriteLine($"Reloaded User ID from userId key: {_userId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to reload user ID from any source");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading user ID: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}