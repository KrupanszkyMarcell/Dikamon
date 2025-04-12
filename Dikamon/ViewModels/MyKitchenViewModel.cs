using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    // Helper class to represent a food category in the UI
    public class FoodCategoryViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameEN { get; set; }
        public string ImageUrl { get; set; }

        // Convert from ItemTypes model
        public static FoodCategoryViewModel FromItemTypes(ItemTypes itemType)
        {
            return new FoodCategoryViewModel
            {
                Id = itemType.Id,
                Name = itemType.Name,
                NameEN = itemType.Name_EN,
                ImageUrl = itemType.Image
            };
        }
    }

    public partial class MyKitchenViewModel : ObservableObject
    {
        private readonly IItemTypesApiCommand _itemTypesApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;

        [ObservableProperty]
        private ObservableCollection<FoodCategoryViewModel> _foodCategories;

        [ObservableProperty]
        private ObservableCollection<Stores> _storedItems;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private FoodCategoryViewModel _selectedCategory;

        private int _userId;

        public MyKitchenViewModel(IItemTypesApiCommand itemTypesApiCommand, IStoredItemsApiCommand storedItemsApiCommand)
        {
            _itemTypesApiCommand = itemTypesApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            FoodCategories = new ObservableCollection<FoodCategoryViewModel>();
            StoredItems = new ObservableCollection<Stores>();

            LoadUserIdAsync();
            LoadCategoriesAsync();
        }

        private async void LoadUserIdAsync()
        {
            try
            {
                var userJson = await SecureStorage.GetAsync("user");
                Debug.WriteLine($"LoadUserIdAsync - User JSON: {userJson}");

                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                        Debug.WriteLine($"User ID loaded: {_userId}");
                        await LoadStoredItemsAsync();
                    }
                    else
                    {
                        Debug.WriteLine("User or User.Id is null after deserialization");
                    }
                }
                else
                {
                    Debug.WriteLine("User JSON is null or empty in secure storage");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user ID: {ex.Message}");
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
                        FoodCategories.Add(FoodCategoryViewModel.FromItemTypes(category));
                    }
                    Debug.WriteLine($"Loaded {FoodCategories.Count} categories");
                }
                else
                {
                    Debug.WriteLine($"Failed to load categories: {response.Error?.Content}");

                    // Fallback to hardcoded categories if API fails
                    LoadFallbackCategories();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a kategóriákat", "OK");

                // Fallback to hardcoded categories
                LoadFallbackCategories();
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        private void LoadFallbackCategories()
        {
            // Fallback data if the API call fails
            Debug.WriteLine("Loading fallback categories");
            FoodCategories.Clear();

            FoodCategories.Add(new FoodCategoryViewModel
            {
                Id = 1,
                Name = "Zöldségek",
                NameEN = "Vegetables",
                ImageUrl = "https://bgs.jedlik.eu/ml/Images/Types/vegetables.jpg"
            });

            FoodCategories.Add(new FoodCategoryViewModel
            {
                Id = 2,
                Name = "Gyümölcsök",
                NameEN = "Fruits",
                ImageUrl = "https://bgs.jedlik.eu/ml/Images/Types/fruits.jpg"
            });

            FoodCategories.Add(new FoodCategoryViewModel
            {
                Id = 3,
                Name = "Diófélék",
                NameEN = "Nuts",
                ImageUrl = "https://bgs.jedlik.eu/ml/Images/Types/nuts.jpg"
            });

            FoodCategories.Add(new FoodCategoryViewModel
            {
                Id = 4,
                Name = "Tejtermékek",
                NameEN = "Dairy",
                ImageUrl = "https://bgs.jedlik.eu/ml/Images/Types/dairy.jpg"
            });

            FoodCategories.Add(new FoodCategoryViewModel
            {
                Id = 5,
                Name = "Állati eredetű",
                NameEN = "Animal based",
                ImageUrl = "https://bgs.jedlik.eu/ml/Images/Types/meats.jpg"
            });

            FoodCategories.Add(new FoodCategoryViewModel
            {
                Id = 6,
                Name = "Egyéb",
                NameEN = "Other",
                ImageUrl = "https://bgs.jedlik.eu/ml/Images/Types/other.jpg"
            });
        }

        [RelayCommand]
        private async Task LoadStoredItemsAsync()
        {
            if (IsLoading || _userId == 0)
                return;

            try
            {
                IsLoading = true;
                Debug.WriteLine($"Loading stored items for user {_userId}");

                var response = await _storedItemsApiCommand.GetStoredItems(_userId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    StoredItems.Clear();
                    foreach (var item in response.Content)
                    {
                        StoredItems.Add(item);
                    }
                    Debug.WriteLine($"Loaded {StoredItems.Count} stored items");
                }
                else
                {
                    Debug.WriteLine($"Failed to load stored items: {response.Error?.Content}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading stored items: {ex.Message}");
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
        private async Task SelectCategory(FoodCategoryViewModel category)
        {
            if (category == null)
                return;

            SelectedCategory = category;
            Debug.WriteLine($"Selected category: {category.Name}, ID: {category.Id}");

            try
            {
                // Convert to strings because QueryProperty works with strings
                var navigationParameter = new Dictionary<string, object>
                {
                    { "categoryName", category.Name },
                    { "categoryId", category.Id.ToString() } // Must be string for QueryProperty
                };

                Debug.WriteLine($"Navigating to CategoryItemsPage with parameters: categoryName={category.Name}, categoryId={category.Id}");
                await Shell.Current.GoToAsync(nameof(CategoryItemsPage), navigationParameter);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", $"Navigációs hiba: {ex.Message}", "OK");
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