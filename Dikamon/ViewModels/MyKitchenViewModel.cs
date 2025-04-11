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
    public partial class MyKitchenViewModel : ObservableObject
    {
        private readonly IItemTypesApiCommand _itemTypesApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;

        [ObservableProperty]
        private ObservableCollection<ItemTypes> _foodCategories;

        [ObservableProperty]
        private ObservableCollection<Stores> _storedItems;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private string _selectedCategory;

        private int _userId;

        public MyKitchenViewModel(IItemTypesApiCommand itemTypesApiCommand, IStoredItemsApiCommand storedItemsApiCommand)
        {
            _itemTypesApiCommand = itemTypesApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            FoodCategories = new ObservableCollection<ItemTypes>();
            StoredItems = new ObservableCollection<Stores>();

            LoadUserIdAsync();
            LoadCategoriesAsync();
        }

        private async void LoadUserIdAsync()
        {
            try
            {
                var userJson = await SecureStorage.GetAsync("user");
                System.Diagnostics.Debug.WriteLine($"LoadUserIdAsync - User JSON: {userJson}");

                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                        System.Diagnostics.Debug.WriteLine($"User ID loaded: {_userId}");
                        await LoadStoredItemsAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User or User.Id is null after deserialization");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("User JSON is null or empty in secure storage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user ID: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                var response = await _itemTypesApiCommand.GetItemTypes();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    FoodCategories.Clear();
                    foreach (var category in response.Content)
                    {
                        FoodCategories.Add(category);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {FoodCategories.Count} categories");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load categories: {response.Error?.Content}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a kategóriákat", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task LoadStoredItemsAsync()
        {
            if (IsLoading || _userId == 0)
                return;

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"Loading stored items for user {_userId}");

                var response = await _storedItemsApiCommand.GetStoredItems(_userId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    StoredItems.Clear();
                    foreach (var item in response.Content)
                    {
                        StoredItems.Add(item);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {StoredItems.Count} stored items");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load stored items: {response.Error?.Content}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stored items: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a tárolt termékeket", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadCategoriesAsync();
            await LoadStoredItemsAsync();
        }

        [RelayCommand]
        private async Task SelectCategory(string categoryName)
        {
            SelectedCategory = categoryName;
            System.Diagnostics.Debug.WriteLine($"Selected category: {categoryName}");

            // Find category ID based on category name
            int categoryId = 0;

            // Map Hungarian category names to their expected IDs
            // This is a temporary solution - ideally, you would get these from the API
            switch (categoryName)
            {
                case "Zöldségek":
                    categoryId = 1;
                    break;
                case "Gyümölcsök":
                    categoryId = 2;
                    break;
                case "Diófélék":
                    categoryId = 3;
                    break;
                case "Tejtermékek":
                    categoryId = 4;
                    break;
                default:
                    // Try to find in FoodCategories if available
                    var category = FoodCategories.FirstOrDefault(c => c.Name == categoryName);
                    if (category != null)
                    {
                        categoryId = category.Id;
                    }
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Category ID for {categoryName}: {categoryId}");

            if (categoryId > 0)
            {
                try
                {
                    // Convert to strings because QueryProperty works with strings
                    var navigationParameter = new Dictionary<string, object>
                    {
                        { "categoryName", categoryName },
                        { "categoryId", categoryId.ToString() } // Must be string for QueryProperty
                    };

                    System.Diagnostics.Debug.WriteLine($"Navigating to CategoryItemsPage with parameters: categoryName={categoryName}, categoryId={categoryId}");
                    await Shell.Current.GoToAsync(nameof(CategoryItemsPage), navigationParameter);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Hiba", $"Navigációs hiba: {ex.Message}", "OK");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No category ID found for {categoryName}");
                await Application.Current.MainPage.DisplayAlert("Hiba", $"Nem található kategória ID a '{categoryName}' kategóriához", "OK");
            }
        }

        [RelayCommand]
        private async Task SearchItems()
        {
            // Implementation for searching items
            await Application.Current.MainPage.DisplayAlert("Keresés", $"Keresés a következőre: {SearchText}", "OK");
        }

        [RelayCommand]
        private async Task AddNewItem()
        {
            // Implementation for adding a new item
            await Application.Current.MainPage.DisplayAlert("Új termék", "Új termék hozzáadása hamarosan elérhető lesz", "OK");
        }
    }
}