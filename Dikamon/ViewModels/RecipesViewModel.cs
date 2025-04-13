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

            LoadRecipeTypesAsync();
            LoadRecipesAsync();
        }

        private async Task LoadRecipeTypesAsync()
        {
            try
            {
                IsLoading = true;
                var response = await _recipesApiCommand.GetRecipeTypes();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    RecipeTypes.Clear();
                    RecipeTypes.Add("Mind"); // Add "All" option

                    foreach (var type in response.Content)
                    {
                        RecipeTypes.Add(type);
                    }

                    SelectedRecipeType = "Mind"; // Default to "All"
                    Debug.WriteLine($"Loaded {RecipeTypes.Count} recipe types");
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
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadFallbackRecipeTypes()
        {
            // Fallback data if the API call fails
            Debug.WriteLine("Loading fallback recipe types");
            RecipeTypes.Clear();

            RecipeTypes.Add("Mind");
            RecipeTypes.Add("Reggeli");
            RecipeTypes.Add("Amerikai");
            RecipeTypes.Add("Ázsiai");
            RecipeTypes.Add("Olasz");
            RecipeTypes.Add("Magyaros");
            RecipeTypes.Add("Mexikói");
            RecipeTypes.Add("Desszert");

            SelectedRecipeType = "Mind"; // Default to "All"
        }

        [RelayCommand]
        private async Task LoadRecipesAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;

                // Get all recipes or filter by type
                var response = SelectedRecipeType == "Mind" ?
                    await _recipesApiCommand.GetRecipes() :
                    await _recipesApiCommand.GetRecipesByType(GetRecipeTypeCode(SelectedRecipeType));

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var allRecipes = response.Content;

                    // Calculate pagination
                    TotalPages = (int)Math.Ceiling((double)allRecipes.Count / ItemsPerPage);
                    CurrentPage = Math.Min(CurrentPage, TotalPages);
                    if (CurrentPage < 1) CurrentPage = 1;

                    // Apply search filter if provided
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        allRecipes = allRecipes.Where(r =>
                            r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            r.Name_EN.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            r.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            r.Description_EN.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        ).ToList();

                        // Recalculate pagination after filter
                        TotalPages = (int)Math.Ceiling((double)allRecipes.Count / ItemsPerPage);
                        CurrentPage = Math.Min(CurrentPage, TotalPages);
                        if (CurrentPage < 1) CurrentPage = 1;
                    }

                    // Apply pagination
                    var paginatedRecipes = allRecipes
                        .Skip((CurrentPage - 1) * ItemsPerPage)
                        .Take(ItemsPerPage)
                        .ToList();

                    Recipes.Clear();
                    foreach (var recipe in paginatedRecipes)
                    {
                        Recipes.Add(recipe);
                    }

                    Debug.WriteLine($"Loaded {Recipes.Count} recipes (Page {CurrentPage} of {TotalPages})");

                    // Notify that the CanGoToNextPage property may have changed
                    OnPropertyChanged(nameof(CanGoToNextPage));
                }
                else
                {
                    Debug.WriteLine($"Failed to load recipes: {response.Error?.Content}");

                    // Fallback to hardcoded recipes if the API fails
                    LoadFallbackRecipes();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recipes: {ex.Message}");

                // Fallback to hardcoded recipes
                LoadFallbackRecipes();
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
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

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
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
            CurrentPage = 1; // Reset to first page when changing filter
            await LoadRecipesAsync();
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

            // Navigate to recipe details page
            var navigationParameter = new Dictionary<string, object>
            {
                { "recipeId", recipe.Id.ToString() }
            };

            // await Shell.Current.GoToAsync(nameof(RecipeDetailsPage), navigationParameter);
        }

        // Helper method to convert recipe type name to code
        private string GetRecipeTypeCode(string typeName)
        {
            return typeName switch
            {
                "Reggeli" => "REG",
                "Amerikai" => "AME",
                "Ázsiai" => "ASI",
                "Olasz" => "ITA",
                "Magyaros" => "HUN",
                "Mexikói" => "MEX",
                "Desszert" => "DES",
                _ => ""
            };
        }
    }
}