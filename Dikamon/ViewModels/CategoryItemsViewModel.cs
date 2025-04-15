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
    [QueryProperty(nameof(RefreshRequired), "refresh")]
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

        [ObservableProperty]
        private bool _refreshRequired = false;

        private int _userId;
        private bool _isInitialized = false;

        public CategoryItemsViewModel(IItemsApiCommand itemsApiCommand,
                                    IStoredItemsApiCommand storedItemsApiCommand,
                                    IItemTypesApiCommand itemTypesApiCommand)
        {
            _itemsApiCommand = itemsApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;
            _itemTypesApiCommand = itemTypesApiCommand;

            CategoryItems = new ObservableCollection<Stores>();
            AvailableItems = new ObservableCollection<Items>();
        }

        partial void OnRefreshRequiredChanged(bool value)
        {
            if (value && _isInitialized)
            {
                LoadCategoryItemsAsync().ConfigureAwait(false);
            }
        }

        public async Task Initialize(string categoryName, int categoryId)
        {
            if (_isInitialized && categoryName == CategoryName && categoryId == CategoryId)
            {
                return;
            }
            CategoryName = categoryName;
            CategoryId = categoryId;
            _isInitialized = false;
            await LoadUserIdAsync();
            if (_userId == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Figyelmeztetés", "A felhasználói adatok betöltése sikertelen. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            await Task.WhenAll(
                LoadCategoryItemsAsync(),
                LoadAvailableItemsAsync()
            );

            _isInitialized = true;
        }

        private async Task LoadUserIdAsync()
        {
            try
            {

                var userJson = await SecureStorage.GetAsync("user");
                if (!string.IsNullOrEmpty(userJson))
                {
                    try
                    {
                        var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                        if (user != null && user.Id.HasValue)
                        {
                            _userId = user.Id.Value;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }


                var userIdStr = await SecureStorage.GetAsync("userId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    _userId = userId;
                }
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        private async Task LoadCategoryItemsAsync()
        {
            if (IsLoading)
                return;

            if (_userId == 0)
            {
                return;
            }

            if (CategoryId == 0)
            {
                return;
            }

            try
            {
                IsLoading = true;
                var response = await _storedItemsApiCommand.GetStoredItemsByTypeId(_userId, CategoryId);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    CategoryItems.Clear();
                    if (AvailableItems.Count == 0)
                    {
                        await LoadAvailableItemsAsync();
                    }

                    foreach (var item in response.Content)
                    {

                        if (item.StoredItem == null && item.ItemId > 0)
                        {
                            try
                            {
                                var matchingAvailableItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                                if (matchingAvailableItem != null)
                                {
                                    item.StoredItem = matchingAvailableItem;
                                }
                                else
                                {

                                    var itemsResponse = await _itemsApiCommand.GetItemsByTypeId(CategoryId);
                                    if (itemsResponse.IsSuccessStatusCode && itemsResponse.Content != null)
                                    {
                                        var matchingItem = itemsResponse.Content.FirstOrDefault(i => i.Id == item.ItemId);
                                        if (matchingItem != null)
                                        {
                                            item.StoredItem = matchingItem;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                        }


                        if (item.StoredItem != null)
                        {

                            if (string.IsNullOrEmpty(item.StoredItem.Unit))
                            {
                                item.StoredItem.Unit = "db";
                            }
                            CategoryItems.Add(item);
                        }
                    }
                }
                else
                {
                    if (response.Error != null)
                    {
                        var errorContent = response.Error.Content;
                    }
                }
            }
            catch (Exception ex)
            {
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
                var response = await _itemsApiCommand.GetItemsByTypeId(CategoryId);
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
                }
                else
                {
                    if (response.Error != null)
                    {
                        var errorContent = response.Error.Content;
                    }
                }
            }
            catch (Exception ex)
            {
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
            await LoadUserIdAsync();
            await Task.WhenAll(
                LoadCategoryItemsAsync(),
                LoadAvailableItemsAsync()
            );
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

                if (_userId == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Felhasználói azonosító nem található", "OK");
                    return;
                }

                if (CategoryItems.Count == 0)
                {
                    await LoadCategoryItemsAsync();
                }
                var typedResponse = await _storedItemsApiCommand.GetStoredItemsByTypeIdAndSearch(_userId, CategoryId, SearchText);
                var apiSearchResults = new List<Stores>();
                if (typedResponse.IsSuccessStatusCode && typedResponse.Content != null && typedResponse.Content.Count > 0)
                {
                    foreach (var item in typedResponse.Content)
                    {
                        PopulateStoredItem(item);
                        if (item.StoredItem != null)
                        {
                            if (string.IsNullOrEmpty(item.StoredItem.Unit))
                            {
                                item.StoredItem.Unit = "db";
                            }
                            apiSearchResults.Add(item);
                        }
                    }
                }
                else if (typedResponse.IsSuccessStatusCode) 
                {
                    var generalResponse = await _storedItemsApiCommand.GetStoredItemsBySearch(_userId, SearchText);

                    if (generalResponse.IsSuccessStatusCode && generalResponse.Content != null)
                    {
                        foreach (var item in generalResponse.Content)
                        {
                            PopulateStoredItem(item);
                            if (item.StoredItem != null)
                            {
                                if (string.IsNullOrEmpty(item.StoredItem.Unit))
                                {
                                    item.StoredItem.Unit = "db";
                                }

                                bool isInCategory = item.StoredItem.TypeId == CategoryId;

                                if (!isInCategory)
                                {
                                    var availableItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                                    isInCategory = availableItem != null && availableItem.TypeId == CategoryId;
                                }

                                if (isInCategory)
                                {
                                    apiSearchResults.Add(item);
                                }
                            }
                        }
                    }
                }
                void PopulateStoredItem(Stores item)
                {
                    if (item.StoredItem == null && item.ItemId > 0)
                    {
                        var matchingItem = AvailableItems.FirstOrDefault(i => i.Id == item.ItemId);
                        if (matchingItem != null)
                        {
                            item.StoredItem = matchingItem;
                        }
                    }
                }

                List<Stores> searchResults;

                if (apiSearchResults.Count > 0)
                {
                    searchResults = apiSearchResults;
                }
                else
                {
                    searchResults = CategoryItems
                        .Where(item =>
                            item.StoredItem != null &&
                            (item.StoredItem.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                             (item.StoredItem.Name_EN != null && item.StoredItem.Name_EN.Contains(SearchText, StringComparison.OrdinalIgnoreCase))))
                        .ToList();
                }
                CategoryItems.Clear();
                foreach (var item in searchResults)
                {
                    CategoryItems.Add(item);
                }

                if (CategoryItems.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Információ", $"Nincs találat a keresésre: '{SearchText}'", "OK");
                }
            }
            catch (Exception ex)
            {
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
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "A felhasználói adatok nem elérhetők. Kérjük, jelentkezzen be újra.", "OK");
                return;
            }

            try
            {
                if (CategoryId <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Érvénytelen kategória azonosító", "OK");
                    return;
                }
                string categoryIdStr = CategoryId.ToString();
                var navigationParameter = new Dictionary<string, object>
                {
                    { "categoryId", categoryIdStr },
                    { "categoryName", CategoryName }
                };

                await Shell.Current.GoToAsync(nameof(NewItemPage), navigationParameter);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült megnyitni az új termék oldalt", "OK");
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
                var response = await _storedItemsApiCommand.AddStoredItem(item);

                if (!response.IsSuccessStatusCode)
                {
                    item.Quantity -= 1;
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült növelni a mennyiséget", "OK");
                }
            }
            catch (Exception ex)
            {
                item.Quantity -= 1;
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült növelni a mennyiséget", "OK");
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
                    bool remove = await Application.Current.MainPage.DisplayAlert(
                        "Megerősítés",
                        $"Biztosan eltávolítod a {item.StoredItem?.Name} terméket a konyhádból?",
                        "Igen", "Nem");

                    if (remove)
                    {
                        var response = await _storedItemsApiCommand.DeleteStoredItem(item);
                        if (response.IsSuccessStatusCode)
                        {
                            CategoryItems.Remove(item);
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült törölni a terméket", "OK");
                        }
                    }
                }
                else
                {
                    item.Quantity -= 1;
                    var response = await _storedItemsApiCommand.AddStoredItem(item);
                    if (!response.IsSuccessStatusCode)
                    {
                        item.Quantity += 1; 
                        await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült csökkenteni a mennyiséget", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                if (item.Quantity > 0) item.Quantity += 1; 
                await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült csökkenteni a mennyiséget", "OK");
            }
        }
    }
}