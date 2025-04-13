using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Api;
using Dikamon.Models;

namespace Dikamon.ViewModels
{
    [QueryProperty(nameof(CategoryId), "categoryId")]
    [QueryProperty(nameof(CategoryName), "categoryName")]
    [QueryProperty(nameof(RefreshRequired), "refresh")]
    public partial class NewItemViewModel : ObservableObject
    {
        private readonly IItemsApiCommand _itemsApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;

        [ObservableProperty]
        private ObservableCollection<Items> _availableItems = new();

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
            IStoredItemsApiCommand storedItemsApiCommand)
        {
            _itemsApiCommand = itemsApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            // Make sure collections are initialized
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

        partial void OnCategoryIdChanged(int value)
        {
            Debug.WriteLine($"[TRACE] CategoryId property changed to: {value}");
            if (value > 0)
            {
                // Important: Load items for this category immediately when categoryId changes
                LoadItemsByCategoryAsync(value).ConfigureAwait(false);
            }
        }

        // This handles the case when categoryId comes in as a string from navigation
        public void InitializeWithCategoryInfo(string categoryIdStr)
        {
            Debug.WriteLine($"[TRACE] InitializeWithCategoryInfo called with: {categoryIdStr}");
            if (!string.IsNullOrEmpty(categoryIdStr) && int.TryParse(categoryIdStr, out int categoryId))
            {
                Debug.WriteLine($"[TRACE] Parsed categoryId: {categoryId}, setting CategoryId property");
                CategoryId = categoryId;
            }
            else
            {
                Debug.WriteLine($"[ERROR] Failed to parse category ID from string: {categoryIdStr}");
            }
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

        private async Task LoadItemsByCategoryAsync(int categoryId)
        {
            if (categoryId <= 0)
            {
                Debug.WriteLine($"[ERROR] Invalid category ID: {categoryId}, not loading items");
                return;
            }

            try
            {
                IsLoading = true;
                Debug.WriteLine($"[TRACE] Loading items for category ID: {categoryId}");
                var response = await _itemsApiCommand.GetItemsByTypeId(categoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableItems.Clear();
                    foreach (var item in response.Content)
                    {
                        // Ensure unit is properly set
                        if (string.IsNullOrEmpty(item.Unit))
                        {
                            Debug.WriteLine($"[WARNING] Item {item.Name} (ID: {item.Id}) has no unit, setting default 'db'");
                            item.Unit = "db"; // Set a default unit if none is provided
                        }
                        else
                        {
                            Debug.WriteLine($"[INFO] Item {item.Name} (ID: {item.Id}) has unit: {item.Unit}");
                        }

                        AvailableItems.Add(item);
                        Debug.WriteLine($"Added item: {item.Name}, ID: {item.Id}, Unit: {item.Unit}");
                    }
                    Debug.WriteLine($"[SUCCESS] Loaded {AvailableItems.Count} items for category {categoryId}");

                    if (AvailableItems.Count == 0)
                    {
                        Debug.WriteLine($"[WARNING] No items found for category {categoryId}");
                        await Application.Current?.MainPage?.DisplayAlert("Figyelmeztetés", "Ebben a kategóriában nincsenek elérhető élelmiszerek.", "OK");
                    }

                    _isInitialized = true;
                }
                else
                {
                    Debug.WriteLine($"[ERROR] Failed to load items: {response.Error?.Content}");
                    if (response.Error != null)
                    {
                        Debug.WriteLine($"Error details: {response.Error.Content}");
                    }
                    await Application.Current?.MainPage?.DisplayAlert("Hiba", "Nem sikerült betölteni az élelmiszereket", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EXCEPTION] Error loading items: {ex.Message}");
                await Application.Current?.MainPage?.DisplayAlert("Hiba", $"Hiba történt az élelmiszerek betöltése közben: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
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
                await Application.Current?.MainPage?.DisplayAlert("Hiba", "Kérjük, válasszon élelmiszert", "OK");
                return;
            }

            if (_userId == 0)
            {
                await Application.Current?.MainPage?.DisplayAlert("Hiba", "A felhasználói adatok nem elérhetők. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            try
            {
                IsLoading = true;
                Debug.WriteLine($"Saving item: {SelectedItem.Name}, Quantity: {Quantity}, User ID: {_userId}, Item ID: {SelectedItem.Id}, Unit: {SelectedItem.Unit}");

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

                        // Ensure StoredItem is set correctly
                        if (existingStoredItem.StoredItem == null)
                        {
                            existingStoredItem.StoredItem = SelectedItem;
                            Debug.WriteLine($"Set StoredItem for existing item with unit: {existingStoredItem.StoredItem.Unit}");
                        }

                        var updateResponse = await _storedItemsApiCommand.AddStoredItem(existingStoredItem);

                        if (updateResponse.IsSuccessStatusCode)
                        {
                            await Application.Current?.MainPage?.DisplayAlert(
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
                        Debug.WriteLine($"Adding new item to storage: {SelectedItem.Name} with unit: {SelectedItem.Unit}");
                        var newStoredItem = new Stores
                        {
                            UserId = _userId,
                            ItemId = SelectedItem.Id,
                            Quantity = Quantity,
                            StoredItem = SelectedItem // Set the full StoredItem object to ensure unit information is preserved
                        };

                        Debug.WriteLine($"Created new stored item with StoredItem unit: {newStoredItem.StoredItem?.Unit}");

                        var addResponse = await _storedItemsApiCommand.AddStoredItem(newStoredItem);

                        if (addResponse.IsSuccessStatusCode)
                        {
                            await Application.Current?.MainPage?.DisplayAlert(
                                "Siker",
                                $"Termék hozzáadva: {SelectedItem.Name} - {Quantity} {SelectedItem.Unit ?? "db"}",
                                "OK");
                        }
                        else
                        {
                            Debug.WriteLine($"API error when adding item: {addResponse.Error?.Content}");
                            if (addResponse.Error != null)
                            {
                                Debug.WriteLine($"Error content: {addResponse.Error.Content}");
                            }
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
                await Application.Current?.MainPage?.DisplayAlert("Hiba", $"Nem sikerült menteni a terméket: {ex.Message}", "OK");
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