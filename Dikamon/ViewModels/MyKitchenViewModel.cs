using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        private readonly IItemsApiCommand _itemsApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;

        // Observable Collections
        public ObservableCollection<CategoryViewModel> Categories { get; set; } = new();
        public ObservableCollection<ItemViewModel> Items { get; set; } = new();
        public ObservableCollection<ItemViewModel> CurrentPageItems { get; set; } = new();

        // Selected category
        private CategoryViewModel _selectedCategory;
        public CategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        // View state
        private bool _isCategoryViewVisible = true;
        public bool IsCategoryViewVisible
        {
            get => _isCategoryViewVisible;
            set => SetProperty(ref _isCategoryViewVisible, value);
        }

        private bool _isItemViewVisible = false;
        public bool IsItemViewVisible
        {
            get => _isItemViewVisible;
            set => SetProperty(ref _isItemViewVisible, value);
        }

        // Pagination properties
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _itemsPerPage = 6; // 2x3 grid
        public int ItemsPerPage
        {
            get => _itemsPerPage;
            set => SetProperty(ref _itemsPerPage, value);
        }

        private string _paginationText = "Oldal: 1/1";
        public string PaginationText
        {
            get => _paginationText;
            set => SetProperty(ref _paginationText, value);
        }

        private bool _canGoToPreviousPage = false;
        public bool CanGoToPreviousPage
        {
            get => _canGoToPreviousPage;
            set => SetProperty(ref _canGoToPreviousPage, value);
        }

        private bool _canGoToNextPage = false;
        public bool CanGoToNextPage
        {
            get => _canGoToNextPage;
            set => SetProperty(ref _canGoToNextPage, value);
        }

        // Search functionality
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        // Current user ID
        private int userId = 0;

        public MyKitchenViewModel(
            IItemTypesApiCommand itemTypesApiCommand,
            IItemsApiCommand itemsApiCommand,
            IStoredItemsApiCommand storedItemsApiCommand)
        {
            _itemTypesApiCommand = itemTypesApiCommand;
            _itemsApiCommand = itemsApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            // Load categories when the view model is created
            Task.Run(async () => await LoadCategories());
            Task.Run(async () => await LoadUserId());
        }

        private async Task LoadUserId()
        {
            try
            {
                var userJson = await SecureStorage.Default.GetAsync("user");
                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = JsonSerializer.Deserialize<Users>(userJson);
                    if (user?.Id != null)
                    {
                        userId = user.Id.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                System.Diagnostics.Debug.WriteLine($"Error loading user ID: {ex.Message}");
            }
        }

        private async Task LoadCategories()
        {
            try
            {
                var response = await _itemTypesApiCommand.GetItemTypes();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var categoryList = new ObservableCollection<CategoryViewModel>();
                    foreach (var category in response.Content)
                    {
                        categoryList.Add(new CategoryViewModel
                        {
                            Id = category.Id,
                            Name = category.Name,
                            NameEn = category.Name_EN,
                            Image = !string.IsNullOrEmpty(category.Image)
                                ? category.Image
                                : "default_category.png"
                        });
                    }

                    // Update on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Categories = categoryList;
                        OnPropertyChanged(nameof(Categories));
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                await Application.Current.MainPage.DisplayAlert("Hiba", $"Nem sikerült betölteni a kategóriákat: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SelectCategory(CategoryViewModel category)
        {
            if (category != null)
            {
                SelectedCategory = category;
                IsCategoryViewVisible = false;
                IsItemViewVisible = true;

                await LoadItemsForCategory(category.Id);
            }
        }

        private async Task LoadItemsForCategory(int categoryId)
        {
            try
            {
                if (userId == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Hiba", "A felhasználó azonosítása sikertelen. Kérjük, jelentkezzen be újra.", "OK");
                    return;
                }

                var response = await _storedItemsApiCommand.GetStoredItemsByTypeId(userId, categoryId);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var itemsList = new ObservableCollection<ItemViewModel>();
                    foreach (var storedItem in response.Content)
                    {
                        var item = storedItem.StoredItem;
                        if (item != null)
                        {
                            itemsList.Add(new ItemViewModel
                            {
                                Id = item.Id,
                                Name = item.Name,
                                NameEn = item.Name_EN,
                                TypeId = item.TypeId,
                                Unit = item.Unit ?? "db",
                                Image = !string.IsNullOrEmpty(item.Image)
                                    ? item.Image
                                    : "default_item.png",
                                Quantity = storedItem.Quantity
                            });
                        }
                    }

                    // Update on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Items = itemsList;
                        OnPropertyChanged(nameof(Items));
                        CurrentPage = 1;
                        UpdatePagination();
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                await Application.Current.MainPage.DisplayAlert("Hiba", $"Nem sikerült betölteni az elemeket: {ex.Message}", "OK");
            }
        }

        private void UpdatePagination()
        {
            // Calculate total pages
            TotalPages = (Items.Count + ItemsPerPage - 1) / ItemsPerPage;
            if (TotalPages < 1) TotalPages = 1;

            // Ensure current page is valid
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
            if (CurrentPage < 1)
                CurrentPage = 1;

            // Update page navigation buttons
            CanGoToPreviousPage = CurrentPage > 1;
            CanGoToNextPage = CurrentPage < TotalPages;

            // Update pagination text
            PaginationText = $"Oldal: {CurrentPage}/{TotalPages}";

            // Update the items for the current page
            int startIndex = (CurrentPage - 1) * ItemsPerPage;
            var pageItems = Items
                .Skip(startIndex)
                .Take(ItemsPerPage)
                .ToList();

            CurrentPageItems = new ObservableCollection<ItemViewModel>(pageItems);
            OnPropertyChanged(nameof(CurrentPageItems));
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePagination();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePagination();
            }
        }

        [RelayCommand]
        private void BackToCategories()
        {
            IsCategoryViewVisible = true;
            IsItemViewVisible = false;
            SelectedCategory = null;
        }

        [RelayCommand]
        private async Task Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || SelectedCategory == null)
                return;

            try
            {
                var response = await _storedItemsApiCommand.GetStoredItemsByTypeIdAndSearch(
                    userId, SelectedCategory.Id, SearchText);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var itemsList = new ObservableCollection<ItemViewModel>();
                    foreach (var storedItem in response.Content)
                    {
                        var item = storedItem.StoredItem;
                        if (item != null)
                        {
                            itemsList.Add(new ItemViewModel
                            {
                                Id = item.Id,
                                Name = item.Name,
                                NameEn = item.Name_EN,
                                TypeId = item.TypeId,
                                Unit = item.Unit ?? "db",
                                Image = !string.IsNullOrEmpty(item.Image)
                                    ? item.Image
                                    : "default_item.png",
                                Quantity = storedItem.Quantity
                            });
                        }
                    }

                    // Update on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Items = itemsList;
                        OnPropertyChanged(nameof(Items));
                        CurrentPage = 1;
                        UpdatePagination();
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", $"Keresési hiba: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AddNewItem()
        {
            await Application.Current.MainPage.DisplayAlert("Új tétel", "Új tétel hozzáadása funkció hamarosan elérhető lesz!", "OK");
        }

        [RelayCommand]
        private async Task Info(ItemViewModel item)
        {
            if (item != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    item.Name,
                    $"Név: {item.Name}\nMennyiség: {item.Quantity} {item.Unit}\nTípus: {SelectedCategory?.Name ?? "Ismeretlen kategória"}",
                    "Bezárás");
            }
        }

        [RelayCommand]
        private async Task DecreaseQuantity(ItemViewModel item)
        {
            if (item != null && item.Quantity > 0)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Mennyiség csökkentése",
                    $"Biztosan csökkenteni szeretnéd a(z) {item.Name} mennyiségét?",
                    "Igen", "Nem");

                if (confirm)
                {
                    try
                    {
                        var storedItem = new Stores
                        {
                            UserId = userId,
                            ItemId = item.Id,
                            Quantity = item.Quantity - 1
                        };

                        // If quantity is 0, delete the item, otherwise update it
                        if (storedItem.Quantity == 0)
                        {
                            var response = await _storedItemsApiCommand.DeleteStoredItem(storedItem);
                            if (response.IsSuccessStatusCode)
                            {
                                // Remove the item from the list
                                Items.Remove(item);
                                OnPropertyChanged(nameof(Items));
                                UpdatePagination();
                                await Application.Current.MainPage.DisplayAlert("Sikeres", "A tétel eltávolítva.", "OK");
                            }
                        }
                        else
                        {
                            // Update the item quantity
                            var response = await _storedItemsApiCommand.AddStoredItem(storedItem);
                            if (response.IsSuccessStatusCode)
                            {
                                // Update the quantity in the viewmodel
                                item.Quantity = storedItem.Quantity;
                                OnPropertyChanged(nameof(Items));

                                UpdatePagination();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("Hiba", $"Nem sikerült frissíteni a tételt: {ex.Message}", "OK");
                    }
                }
            }
        }
    }

    // ViewModels for the UI
    public class CategoryViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    public class ItemViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }
    }
}