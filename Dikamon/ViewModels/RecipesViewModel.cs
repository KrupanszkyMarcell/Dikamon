using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Api;
using Dikamon.Models;
using Dikamon.Pages;
using System.Diagnostics;

namespace Dikamon.ViewModels
{
    public partial class RecipesViewModel : ObservableObject
    {
        private readonly IRecipesApiCommand _recipesApiCommand;

        [ObservableProperty]
        private ObservableCollection<Recipes> _recipes;

        [ObservableProperty]
        private ObservableCollection<string> _recipeTypes;

        [ObservableProperty]
        private string _selectedRecipeType;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _itemsPerPage = 3;

        // Add the missing property that's used in RecipesPage.xaml
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        public RecipesViewModel(IRecipesApiCommand recipesApiCommand)
        {
            _recipesApiCommand = recipesApiCommand;
            Recipes = new ObservableCollection<Recipes>();
            RecipeTypes = new ObservableCollection<string>();
        }

        [RelayCommand]
        private async Task LoadRecipeTypesAsync()
        {
            try
            {
                Debug.WriteLine("Loading recipe types...");
                var response = await _recipesApiCommand.GetRecipeTypes();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    RecipeTypes.Clear();
                    RecipeTypes.Add("Mind"); // Add "All" option

                    foreach (var type in response.Content)
                    {
                        if (!string.IsNullOrEmpty(type))
                        {
                            Debug.WriteLine($"Adding recipe type: {type}");
                            RecipeTypes.Add(type);
                        }
                    }

                    // Only set default if it's not already set
                    if (string.IsNullOrEmpty(SelectedRecipeType))
                    {
                        SelectedRecipeType = "Mind"; // Default to "All"
                    }

                    Debug.WriteLine($"Loaded {RecipeTypes.Count} recipe types, selected type: {SelectedRecipeType}");
                }
                else
                {
                    Debug.WriteLine($"Failed to load recipe types: {response.Error?.Content}");
                    // Fallback to hardcoded types if the API fails
                    LoadFallbackRecipeTypes();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recipe types: {ex.Message}");
                // Fallback to hardcoded types
                LoadFallbackRecipeTypes();
            }
        }

        private void LoadFallbackRecipeTypes()
        {
            // Fallback data if the API call fails
            Debug.WriteLine("Loading fallback recipe types");
            RecipeTypes.Clear();

            RecipeTypes.Add("Mind");
            RecipeTypes.Add("REG"); // Using actual type codes
            RecipeTypes.Add("AME");
            RecipeTypes.Add("ASI");
            RecipeTypes.Add("ITA");
            RecipeTypes.Add("HUN");
            RecipeTypes.Add("MEX");
            RecipeTypes.Add("DES");

            // Only set default if it's not already set
            if (string.IsNullOrEmpty(SelectedRecipeType))
            {
                SelectedRecipeType = "Mind"; // Default to "All"
            }
        }

        [RelayCommand]
        public async Task LoadRecipesAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                Debug.WriteLine($"Started loading recipes with type filter: {SelectedRecipeType}");

                // Clear the collection first so the UI updates immediately
                Recipes.Clear();

                // Get all recipes or filter by type
                var typeFilter = SelectedRecipeType == "Mind" ? "" : SelectedRecipeType;
                Debug.WriteLine($"Using type filter for API: '{typeFilter}'");

                var response = string.IsNullOrEmpty(typeFilter) ?
                    await _recipesApiCommand.GetRecipes() :
                    await _recipesApiCommand.GetRecipesByType(typeFilter);

                Debug.WriteLine($"Recipe API response successful: {response.IsSuccessStatusCode}");

                List<Recipes> allRecipes = new List<Recipes>();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    allRecipes = response.Content ?? new List<Recipes>();
                    Debug.WriteLine($"API returned {allRecipes.Count} recipes");
                }
                else
                {
                    Debug.WriteLine($"Failed to load recipes: {response.Error?.Content}");
                    if (response.Error != null)
                    {
                        Debug.WriteLine($"Error details: {response.Error.Content}");
                    }
                    LoadFallbackRecipes();
                    return;
                }

                if (allRecipes.Count == 0)
                {
                    Debug.WriteLine("API returned zero recipes");
                    LoadFallbackRecipes();
                    return;
                }

                // Calculate pagination
                TotalPages = (int)Math.Ceiling((double)allRecipes.Count / Math.Max(1, ItemsPerPage));
                CurrentPage = Math.Min(Math.Max(1, CurrentPage), Math.Max(1, TotalPages));

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    Debug.WriteLine($"Applying search filter: {SearchText}");
                    allRecipes = allRecipes.Where(r =>
                        (r.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Name_EN?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Description_EN?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                    Debug.WriteLine($"After search filter: {allRecipes.Count} recipes");

                    // Recalculate pagination after filter
                    TotalPages = allRecipes.Count > 0 ?
                        (int)Math.Ceiling((double)allRecipes.Count / Math.Max(1, ItemsPerPage)) : 1;
                    CurrentPage = Math.Min(Math.Max(1, CurrentPage), Math.Max(1, TotalPages));
                }

                // Apply pagination safely
                var skipCount = Math.Max(0, (CurrentPage - 1) * ItemsPerPage);
                var takeCount = ItemsPerPage;

                var paginatedRecipes = allRecipes
                    .Skip(skipCount)
                    .Take(takeCount)
                    .ToList();

                Debug.WriteLine($"Applying pagination: page {CurrentPage} of {TotalPages}, showing {paginatedRecipes.Count} recipes");

                // Clear the collection BEFORE adding new items
                Recipes.Clear();

                foreach (var recipe in paginatedRecipes)
                {
                    // Ensure all string properties are non-null
                    recipe.Name = recipe.Name ?? string.Empty;
                    recipe.Name_EN = recipe.Name_EN ?? string.Empty;
                    recipe.Description = recipe.Description ?? string.Empty;
                    recipe.Description_EN = recipe.Description_EN ?? string.Empty;
                    recipe.Type = recipe.Type ?? string.Empty;
                    recipe.Image = recipe.Image ?? string.Empty;

                    Debug.WriteLine($"Adding recipe: {recipe.Name} (Type: {recipe.Type})");
                    Recipes.Add(recipe);
                }

                Debug.WriteLine($"Loaded {Recipes.Count} recipes (Page {CurrentPage} of {TotalPages})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recipes: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                LoadFallbackRecipes();
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                // Make sure to notify for CanGoToNextPage
                OnPropertyChanged(nameof(CanGoToNextPage));
                Debug.WriteLine($"Finished loading recipes, collection count: {Recipes.Count}");
            }
        }

        private void LoadFallbackRecipes()
        {
            // Fallback data if the API call fails
            Debug.WriteLine("Loading fallback recipes");
            Recipes.Clear();

            Recipes.Add(new Recipes
            {
                Id = 1,
                Name = "Bolognai tészta",
                Name_EN = "Spaghetti Bolognese",
                Description = "Klasszikus olasz tésztaétel darált hússal és paradicsomos szósszal",
                Description_EN = "Classic Italian pasta dish with minced meat and tomato sauce",
                Type = "ITA",
                Difficulty = 2,
                Time = 80,
                Image = "https://bgs.jedlik.eu/ml/Images/Recipes/bolognai.jpg"
            });

            Recipes.Add(new Recipes
            {
                Id = 2,
                Name = "Rántott csirke kukoricás rizzsel",
                Name_EN = "Breaded chicken with corn rice",
                Description = "Rántott csirkemell filé kukoricás rizzsel tálalva",
                Description_EN = "Breaded chicken breast fillet served with corn rice",
                Type = "HUN",
                Difficulty = 2,
                Time = 45,
                Image = "https://bgs.jedlik.eu/ml/Images/Recipes/rantott_csirke.jpg"
            });

            Recipes.Add(new Recipes
            {
                Id = 3,
                Name = "Almáspite",
                Name_EN = "Apple pie",
                Description = "Klasszikus magyar almáspite fahéjas almatöltelékkel",
                Description_EN = "Classic Hungarian apple pie with cinnamon apple filling",
                Type = "DES",
                Difficulty = 3,
                Time = 60,
                Image = "https://bgs.jedlik.eu/ml/Images/Recipes/almaspite.jpg"
            });

            TotalPages = 1;
            CurrentPage = 1;

            Debug.WriteLine($"Loaded {Recipes.Count} fallback recipes");

            // Notify that the CanGoToNextPage property may have changed
            OnPropertyChanged(nameof(CanGoToNextPage));
        }

        partial void OnCurrentPageChanged(int value)
        {
            // When current page changes, we need to notify that CanGoToNextPage might have changed
            OnPropertyChanged(nameof(CanGoToNextPage));
        }

        partial void OnTotalPagesChanged(int value)
        {
            // When total pages changes, we need to notify that CanGoToNextPage might have changed
            OnPropertyChanged(nameof(CanGoToNextPage));
        }

        partial void OnSelectedRecipeTypeChanged(string value)
        {
            // When selected recipe type changes, we should reload recipes
            // but we don't want to trigger this on initialization
            if (!IsLoading && !string.IsNullOrEmpty(value))
            {
                Debug.WriteLine($"Selected recipe type changed to: {value}");
                FilterRecipesByTypeCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadRecipeTypesAsync();
            await LoadRecipesAsync();
        }

        [RelayCommand]
        private async Task SearchRecipes()
        {
            CurrentPage = 1; // Reset to first page when searching
            await LoadRecipesAsync();
        }

        [RelayCommand]
        private async Task FilterRecipesByType()
        {
            try
            {
                Debug.WriteLine($"Filtering recipes by type: {SelectedRecipeType}");
                CurrentPage = 1; // Reset to first page when changing filter
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering recipes by type: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadRecipesAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadRecipesAsync();
            }
        }

        [RelayCommand]
        private async Task ViewRecipeDetails(Recipes recipe)
        {
            if (recipe == null)
                return;

            try
            {
                Debug.WriteLine($"Navigating to recipe details for recipe ID: {recipe.Id}");

                // Create navigation parameters with the recipe ID
                var navigationParameter = new Dictionary<string, object>
                {
                    { "recipeId", recipe.Id.ToString() }
                };

                try
                {
                    // First try with the nested route
                    await Shell.Current.GoToAsync($"RecipesPage/{nameof(RecipeDetailsPage)}", navigationParameter);
                }
                catch (Exception routeEx)
                {
                    Debug.WriteLine($"Error with nested route: {routeEx.Message}, trying direct route...");

                    // If that fails, try with the direct route
                    await Shell.Current.GoToAsync(nameof(RecipeDetailsPage), navigationParameter);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error viewing recipe details: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Hiba",
                    $"Nem sikerült megnyitni a recept részleteit: {ex.Message}",
                    "OK");
            }
        }
    }
}