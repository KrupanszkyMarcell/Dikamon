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
                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                        await LoadStoredItemsAsync();
                    }
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
                var response = await _storedItemsApiCommand.GetStoredItems(_userId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    StoredItems.Clear();
                    foreach (var item in response.Content)
                    {
                        StoredItems.Add(item);
                    }
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
            await Application.Current.MainPage.DisplayAlert("Kategória", $"A '{categoryName}' kategória részletek hamarosan elérhetőek lesznek", "OK");
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