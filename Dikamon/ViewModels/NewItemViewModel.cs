using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private ObservableCollection<ItemTypes> _itemTypes = new();

        [ObservableProperty]
        private ObservableCollection<Items> _availableItems = new();

        [ObservableProperty]
        private ItemTypes _selectedItemType;

        [ObservableProperty]
        private Items _selectedItem;

        [ObservableProperty]
        private string _selectedItemImageSource;

        [ObservableProperty]
        private string _itemUnit = "db";

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
        private bool _isInitialized = false;

        public bool CanDecrement => Quantity > 1;

        public NewItemViewModel(
            IItemsApiCommand itemsApiCommand,
            IItemTypesApiCommand itemTypesApiCommand,
            IStoredItemsApiCommand storedItemsApiCommand)
        {
            _itemsApiCommand = itemsApiCommand;
            _itemTypesApiCommand = itemTypesApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            // Make sure collections are initialized
            ItemTypes = new ObservableCollection<ItemTypes>();
            AvailableItems = new ObservableCollection<Items>();

            LoadUserIdAsync();
        }

        private async void LoadUserIdAsync()
        {
            try
            {
                var userJson = await SecureStorage.GetAsync("user");
                Debug.WriteLine($"User JSON from secure storage: {(userJson != null ? "Retrieved" : "Not found")}");

                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                        Debug.WriteLine($"User ID loaded: {_userId}");
                    }
                    else
                    {
                        Debug.WriteLine("User or User.Id is null after deserialization");
                    }
                }
                else
                {
                    // Fallback: Try to get user ID from a different source
                    var userIdStr = await SecureStorage.GetAsync("userId");
                    if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                    {
                        _userId = userId;
                        Debug.WriteLine($"User ID loaded from userId key: {_userId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user ID: {ex.Message}");
            }
        }

        public async Task Initialize(int categoryId, string categoryName)
        {
            if (_isInitialized && categoryId == CategoryId)
            {
                Debug.WriteLine($"NewItemViewModel already initialized with category ID: {categoryId}");
                return;
            }

            CategoryId = categoryId;
            CategoryName = categoryName;
            _isInitialized = false;

            IsLoading = true;
            Debug.WriteLine($"Initializing NewItemViewModel with category ID: {categoryId}, name: {categoryName}");

            try
            {
                // Load all item types first
                await LoadItemTypesAsync();

                // Then load items for the specific category
                if (categoryId > 0)
                {
                    await LoadItemsByCategoryAsync(categoryId);

                    // Pre-select the category if it was provided
                    var matchingType = ItemTypes.FirstOrDefault(t => t.Id == categoryId);
                    if (matchingType != null)
                    {
                        Debug.WriteLine($"Pre-selecting category: {matchingType.Name}");
                        SelectedItemType = matchingType;
                    }
                    else
                    {
                        Debug.WriteLine($"No matching category found for ID: {categoryId}");
                    }
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Initialize: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a kategóriákat és termékeket", "OK");
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
                Debug.WriteLine($"Selected item type changed to: {value.Name}, ID: {value.Id}");
                LoadItemsByCategoryAsync(value.Id);
            }
            else
            {
                Debug.WriteLine("Selected item type changed to null");
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
                Debug.WriteLine($"Selected item changed to: {value.Name}, ID: {value.Id}");

                // Set the image source
                SelectedItemImageSource = !string.IsNullOrEmpty(value.Image)
                    ? value.Image
                    : "placeholder_food.png";

                // Set the unit
                ItemUnit = !string.IsNullOrEmpty(value.Unit)
                    ? value.Unit
                    : "db";

                Debug.WriteLine($"Set item unit to: {ItemUnit}");
            }
            else
            {
                Debug.WriteLine("Selected item changed to null");
                SelectedItemImageSource = null;
                ItemUnit = "db"; // Default unit
            }
        }

        private async Task LoadItemTypesAsync()
        {
            try
            {
                Debug.WriteLine("Loading item types...");
                var response = await _itemTypesApiCommand.GetItemTypes();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    ItemTypes.Clear();
                    foreach (var type in response.Content)
                    {
                        ItemTypes.Add(type);
                        Debug.WriteLine($"Added item type: {type.Name}, ID: {type.Id}");
                    }
                    Debug.WriteLine($"Loaded {ItemTypes.Count} item types");
                }
                else
                {
                    Debug.WriteLine($"Failed to load item types: {response.Error?.Content}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading item types: {ex.Message}");
                // Don't rethrow - we want to continue even if this fails
            }
        }

        private async Task LoadItemsByCategoryAsync(int categoryId)
        {
            try
            {
                Debug.WriteLine($"Loading items for category ID: {categoryId}");
                var response = await _itemsApiCommand.GetItemsByTypeId(categoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableItems.Clear();
                    foreach (var item in response.Content)
                    {
                        AvailableItems.Add(item);
                        Debug.WriteLine($"Added item: {item.Name}, ID: {item.Id}, Unit: {item.Unit}");
                    }
                    IsItemSelectionEnabled = true;
                    Debug.WriteLine($"Loaded {AvailableItems.Count} items for category {categoryId}");
                }
                else
                {
                    Debug.WriteLine($"Failed to load items: {response.Error?.Content}");
                    IsItemSelectionEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading items: {ex.Message}");
                IsItemSelectionEnabled = false;
            }
        }

        [RelayCommand]
        private void IncrementQuantity()
        {
            Quantity++;
            Debug.WriteLine($"Incremented quantity to: {Quantity}");
        }

        [RelayCommand]
        private void DecrementQuantity()
        {
            if (Quantity > 1)
            {
                Quantity--;
                Debug.WriteLine($"Decremented quantity to: {Quantity}");
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (SelectedItem == null)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "Kérjük, válasszon élelmiszert", "OK");
                return;
            }

            if (_userId == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "A felhasználói adatok nem elérhetők. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            try
            {
                IsLoading = true;
                Debug.WriteLine($"Saving item: {SelectedItem.Name}, Quantity: {Quantity}, User ID: {_userId}");

                // Check if the item already exists in the user's storage
                var storedItemsResponse = await _storedItemsApiCommand.GetStoredItems(_userId);

                if (storedItemsResponse.IsSuccessStatusCode && storedItemsResponse.Content != null)
                {
                    var existingStoredItem = storedItemsResponse.Content
                        .FirstOrDefault(s => s.ItemId == SelectedItem.Id);

                    if (existingStoredItem != null)
                    {
                        // Update the existing item's quantity
                        Debug.WriteLine($"Item already exists in storage, updating quantity from {existingStoredItem.Quantity} to {existingStoredItem.Quantity + Quantity}");
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
                            Debug.WriteLine($"API error when updating item: {updateResponse.Error?.Content}");
                            throw new Exception("Failed to update item quantity");
                        }
                    }
                    else
                    {
                        // Add new stored item
                        Debug.WriteLine($"Adding new item to storage: {SelectedItem.Name}");
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
                            Debug.WriteLine($"API error when adding item: {addResponse.Error?.Content}");
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
                    Debug.WriteLine($"API error when getting stored items: {storedItemsResponse.Error?.Content}");
                    throw new Exception("Failed to check existing items");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving item: {ex.Message}");
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