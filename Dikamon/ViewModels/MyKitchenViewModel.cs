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
    public class FoodCategoryViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameEN { get; set; }
        public string ImageUrl { get; set; }
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
                }
                else
                {
                    LoadFallbackCategories();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a kategóriákat", "OK");
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

            try
            {
                var navigationParameter = new Dictionary<string, object>
                {
                    { "categoryName", category.Name },
                    { "categoryId", category.Id.ToString() } 
                };
                await Shell.Current.GoToAsync(nameof(CategoryItemsPage), navigationParameter);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", $"Navigációs hiba: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SearchItems()
        {
            await Application.Current.MainPage.DisplayAlert("Keresés", $"Keresés a következőre: {SearchText}", "OK");
        }

        [RelayCommand]
        private async Task AddNewItem()
        {
            await Application.Current.MainPage.DisplayAlert("Új termék", "Új termék hozzáadása hamarosan elérhető lesz", "OK");
        }
    }
}