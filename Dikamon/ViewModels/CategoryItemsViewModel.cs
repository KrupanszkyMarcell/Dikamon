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

        public CategoryItemsViewModel(IItemsApiCommand itemsApiCommand,
                                    IStoredItemsApiCommand storedItemsApiCommand,
                                    IItemTypesApiCommand itemTypesApiCommand)
        {
            _itemsApiCommand = itemsApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;
            _itemTypesApiCommand = itemTypesApiCommand;

            CategoryItems = new ObservableCollection<Stores>();
            AvailableItems = new ObservableCollection<Items>();

            LoadUserIdAsync();
        }

        public async Task Initialize(string categoryName, int categoryId)
        {
            CategoryName = categoryName;
            CategoryId = categoryId;

            await LoadCategoryItemsAsync();
            await LoadAvailableItemsAsync();
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
            if (IsLoading || _userId == 0 || CategoryId == 0)
                return;

            try
            {
                IsLoading = true;

                // Get stored items for this user and filter by category
                var response = await _storedItemsApiCommand.GetStoredItemsByTypeId(_userId, CategoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    CategoryItems.Clear();
                    foreach (var item in response.Content)
                    {
                        CategoryItems.Add(item);
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

                // Get all items for this category
                var response = await _itemsApiCommand.GetItemsByTypeId(CategoryId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableItems.Clear();
                    foreach (var item in response.Content)
                    {
                        AvailableItems.Add(item);
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
            await LoadCategoryItemsAsync();
            await LoadAvailableItemsAsync();
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
                        CategoryItems.Add(item);
                    }
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
                await _storedItemsApiCommand.AddStoredItem(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error incrementing item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült növelni a mennyiséget", "OK");
                item.Quantity -= 1; // Revert the change
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
                        await _storedItemsApiCommand.DeleteStoredItem(item);
                        CategoryItems.Remove(item);
                    }
                }
                else
                {
                    item.Quantity -= 1;
                    await _storedItemsApiCommand.AddStoredItem(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrementing item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült csökkenteni a mennyiséget", "OK");
                item.Quantity += 1; // Revert the change
            }
        }
    }
}