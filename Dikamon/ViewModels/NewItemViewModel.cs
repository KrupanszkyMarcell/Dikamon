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
            AvailableItems = new ObservableCollection<Items>();

            LoadUserIdAsync();
        }

        private async void LoadUserIdAsync()
        {
            try
            {
                var userJson = await SecureStorage.GetAsync("user");

                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                    }
                    else
                    {
                    }
                }
                else
                {
                    var userIdStr = await SecureStorage.GetAsync("userId");
                    if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                    {
                        _userId = userId;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        partial void OnCategoryIdChanged(int value)
        {
            if (value > 0)
            {
                LoadItemsByCategoryAsync(value).ConfigureAwait(false);
            }
        }
        public void InitializeWithCategoryInfo(string categoryIdStr)
        {
            if (!string.IsNullOrEmpty(categoryIdStr) && int.TryParse(categoryIdStr, out int categoryId))
            {
                CategoryId = categoryId;
            }
        }

        partial void OnSelectedItemChanged(Items value)
        {
            IsItemSelected = (value != null);

            if (value != null)
            {
                SelectedItemImageSource = !string.IsNullOrEmpty(value.Image)
                    ? value.Image
                    : "placeholder_food.png";
                ItemUnit = !string.IsNullOrEmpty(value.Unit)
                    ? value.Unit
                    : "db";

                Debug.WriteLine($"Set item unit to: {ItemUnit}");
            }
            else
            {
                SelectedItemImageSource = null;
                ItemUnit = "db"; 
            }
        }

        private async Task LoadItemsByCategoryAsync(int categoryId)
        {
            if (categoryId <= 0)
            {
                return;
            }

            try
            {
                IsLoading = true;
                var response = await _itemsApiCommand.GetItemsByTypeId(categoryId);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableItems.Clear();
                    foreach (var item in response.Content)
                    {
                        if (string.IsNullOrEmpty(item.Unit))
                        {
                            item.Unit = "db"; 
                        }
                        AvailableItems.Add(item);
                    }

                    if (AvailableItems.Count == 0)
                    {
                        await Application.Current?.MainPage?.DisplayAlert("Figyelmeztetés", "Ebben a kategóriában nincsenek elérhető élelmiszerek.", "OK");
                    }

                    _isInitialized = true;
                }
                else
                {
                    if (response.Error != null)
                    {
                    }
                    await Application.Current?.MainPage?.DisplayAlert("Hiba", "Nem sikerült betölteni az élelmiszereket", "OK");
                }
            }
            catch (Exception ex)
            {
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
                await Application.Current?.MainPage?.DisplayAlert("Hiba", "A felhasználói adatok nem elérhetők. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            try
            {
                IsLoading = true;
                var storedItemsResponse = await _storedItemsApiCommand.GetStoredItems(_userId);

                if (storedItemsResponse.IsSuccessStatusCode && storedItemsResponse.Content != null)
                {
                    var existingStoredItem = storedItemsResponse.Content
                        .FirstOrDefault(s => s.ItemId == SelectedItem.Id);

                    if (existingStoredItem != null)
                    {
                        existingStoredItem.Quantity += Quantity;
                        if (existingStoredItem.StoredItem == null)
                        {
                            existingStoredItem.StoredItem = SelectedItem;
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
                            throw new Exception("Failed to update item quantity");
                        }
                    }
                    else
                    {
                        var newStoredItem = new Stores
                        {
                            UserId = _userId,
                            ItemId = SelectedItem.Id,
                            Quantity = Quantity,
                            StoredItem = SelectedItem // Set the full StoredItem object to ensure unit information is preserved
                        };

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
                            throw new Exception("Failed to add new item");
                        }
                    }
                    var navigationParameter = new Dictionary<string, object>
            {
                { "refresh", true }
            };
                    await Shell.Current.GoToAsync("..", navigationParameter);
                }
                else
                {
                    throw new Exception("Failed to check existing items");
                }
            }
            catch (Exception ex)
            {
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