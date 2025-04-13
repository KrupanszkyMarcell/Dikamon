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

namespace Dikamon.ViewModels
{
    // Helper class for ingredients with comparison between required and available
    public partial class IngredientViewModel : ObservableObject
    {
        [ObservableProperty]
        private Items _item;

        [ObservableProperty]
        private int _quantity;

        [ObservableProperty]
        private int _availableQuantity;

        [ObservableProperty]
        private bool _hasEnough;
    }

    // Helper class for instruction steps
    public partial class InstructionStepViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _stepNumber;

        [ObservableProperty]
        private string _description;
    }

    [QueryProperty(nameof(RecipeId), "recipeId")]
    public partial class RecipeDetailsViewModel : ObservableObject
    {
        private readonly IRecipesApiCommand _recipesApiCommand;
        private readonly IIngredientsApiCommand _ingredientsApiCommand;
        private readonly IStoredItemsApiCommand _storedItemsApiCommand;

        [ObservableProperty]
        private int _recipeId;

        [ObservableProperty]
        private Recipes _recipe;

        [ObservableProperty]
        private ObservableCollection<IngredientViewModel> _ingredients;

        [ObservableProperty]
        private ObservableCollection<InstructionStepViewModel> _instructionSteps;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _canMakeRecipe;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        private int _userId;
        private bool _isUserIdLoaded = false;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        public RecipeDetailsViewModel(
            IRecipesApiCommand recipesApiCommand,
            IIngredientsApiCommand ingredientsApiCommand,
            IStoredItemsApiCommand storedItemsApiCommand)
        {
            _recipesApiCommand = recipesApiCommand;
            _ingredientsApiCommand = ingredientsApiCommand;
            _storedItemsApiCommand = storedItemsApiCommand;

            Ingredients = new ObservableCollection<IngredientViewModel>();
            InstructionSteps = new ObservableCollection<InstructionStepViewModel>();

            // Start loading the user ID right away
            Task.Run(async () => await EnsureUserIdLoadedAsync());
        }

        partial void OnRecipeIdChanged(int value)
        {
            if (value > 0)
            {
                Debug.WriteLine($"Recipe ID changed to {value}, will load recipe details");
                // Use a fire-and-forget Task to handle async initialization
                Task.Run(async () => await InitializeAsync(value));
            }
        }

        private async Task InitializeAsync(int recipeId)
        {
            try
            {
                await _initializationLock.WaitAsync();

                // First ensure user ID is loaded
                if (!await EnsureUserIdLoadedAsync())
                {
                    Debug.WriteLine("Failed to load user ID during initialization");
                    HasError = true;
                    ErrorMessage = "Nem sikerült betölteni a felhasználói adatokat. Kérjük, jelentkezzen be újra.";
                    return;
                }

                // Now load recipe details
                await LoadRecipeDetailsAsync(recipeId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during initialization: {ex.Message}");
                HasError = true;
                ErrorMessage = "Hiba történt az adatok betöltése közben.";
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private async Task<bool> EnsureUserIdLoadedAsync()
        {
            // If user ID is already loaded, return immediately
            if (_isUserIdLoaded && _userId > 0)
            {
                return true;
            }

            try
            {
                // Try to get from user object first
                var userJson = await SecureStorage.GetAsync("user");
                Debug.WriteLine($"Looking for user ID, found userJson: {!string.IsNullOrEmpty(userJson)}");

                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && user.Id.HasValue)
                    {
                        _userId = user.Id.Value;
                        _isUserIdLoaded = true;
                        Debug.WriteLine($"User ID loaded from user object: {_userId}");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("User object or User.Id is null after deserialization");
                    }
                }

                // Fallback: Try to get user ID directly
                var userIdStr = await SecureStorage.GetAsync("userId");
                Debug.WriteLine($"Looking for userId key, found: {!string.IsNullOrEmpty(userIdStr)}");

                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    _userId = userId;
                    _isUserIdLoaded = true;
                    Debug.WriteLine($"User ID loaded from userId key: {_userId}");
                    return true;
                }

                Debug.WriteLine("Failed to load user ID from any source");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user ID: {ex.Message}");
                return false;
            }
        }

        private async Task LoadRecipeDetailsAsync(int recipeId)
        {
            try
            {
                if (_userId <= 0)
                {
                    Debug.WriteLine("Cannot load recipe details: User ID is not available");
                    HasError = true;
                    ErrorMessage = "A felhasználói adatok nem elérhetők. Kérjük, jelentkezzen be újra.";
                    return;
                }

                IsLoading = true;
                HasError = false;
                Debug.WriteLine($"Loading recipe details for ID: {recipeId}, User ID: {_userId}");

                // Load the recipe
                var response = await _recipesApiCommand.GetRecipesById(recipeId);
                if (response.IsSuccessStatusCode && response.Content != null && response.Content.Count > 0)
                {
                    Recipe = response.Content[0];
                    Debug.WriteLine($"Recipe loaded: {Recipe.Name}");

                    // Load ingredients
                    await LoadIngredientsAsync(recipeId);

                    // Create instruction steps from description
                    CreateInstructionSteps();

                    // Check if we can make the recipe
                    UpdateCanMakeRecipe();
                }
                else
                {
                    Debug.WriteLine($"Failed to load recipe details: {response.Error?.Content}");
                    HasError = true;
                    ErrorMessage = "Nem sikerült betölteni a recept részleteit.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recipe details: {ex.Message}");
                HasError = true;
                ErrorMessage = "Hiba történt a recept betöltése közben.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadIngredientsAsync(int recipeId)
        {
            try
            {
                // Clear existing ingredients
                Ingredients.Clear();

                // Load the recipe ingredients
                var ingredientsResponse = await _ingredientsApiCommand.GetIngredientsById(recipeId);
                if (ingredientsResponse.IsSuccessStatusCode && ingredientsResponse.Content != null)
                {
                    var recipeIngredients = ingredientsResponse.Content;
                    Debug.WriteLine($"Loaded {recipeIngredients.Count} ingredients for recipe {recipeId}");

                    // Load the user's stored items to compare quantities
                    var storedItemsResponse = await _storedItemsApiCommand.GetStoredItems(_userId);
                    if (storedItemsResponse.IsSuccessStatusCode && storedItemsResponse.Content != null)
                    {
                        var storedItems = storedItemsResponse.Content;
                        Debug.WriteLine($"Loaded {storedItems.Count} stored items for user {_userId}");

                        // Create view models for each ingredient with the required and available quantities
                        foreach (var ingredient in recipeIngredients)
                        {
                            // Skip ingredients with null Item
                            if (ingredient.Item == null)
                            {
                                Debug.WriteLine($"Skipping ingredient with ID {ingredient.ItemId} because Item is null");
                                continue;
                            }

                            var storedItem = storedItems.FirstOrDefault(si => si.ItemId == ingredient.ItemId);
                            var availableQuantity = storedItem?.Quantity ?? 0;

                            var ingredientViewModel = new IngredientViewModel
                            {
                                Item = ingredient.Item,
                                Quantity = ingredient.Quantity,
                                AvailableQuantity = availableQuantity,
                                HasEnough = availableQuantity >= ingredient.Quantity
                            };

                            Ingredients.Add(ingredientViewModel);
                            Debug.WriteLine($"Added ingredient: {ingredient.Item?.Name}, Needed: {ingredient.Quantity}, Available: {availableQuantity}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to load stored items: {storedItemsResponse.Error?.Content}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Failed to load ingredients: {ingredientsResponse.Error?.Content}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading ingredients: {ex.Message}");
            }
        }

        private void CreateInstructionSteps()
        {
            InstructionSteps.Clear();

            if (Recipe == null || string.IsNullOrEmpty(Recipe.Description))
                return;

            // For simplicity, we'll create instruction steps by splitting the description by periods
            // In a real app, you might want to store these steps in a more structured way
            var sentences = Recipe.Description.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            int stepNumber = 1;
            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedSentence))
                {
                    InstructionSteps.Add(new InstructionStepViewModel
                    {
                        StepNumber = stepNumber,
                        Description = trimmedSentence + "."
                    });
                    stepNumber++;
                }
            }

            Debug.WriteLine($"Created {InstructionSteps.Count} instruction steps");
        }

        private void UpdateCanMakeRecipe()
        {
            // We can make the recipe if we have enough of all ingredients
            CanMakeRecipe = Ingredients.Count > 0 && Ingredients.All(i => i.HasEnough);
            Debug.WriteLine($"Can make recipe: {CanMakeRecipe}");
        }

        [RelayCommand]
        private async Task RetryLoading()
        {
            if (RecipeId > 0)
            {
                Debug.WriteLine($"Retrying loading recipe details for ID: {RecipeId}");
                await InitializeAsync(RecipeId);
            }
        }

        [RelayCommand]
        private async Task MakeRecipe()
        {
            if (!CanMakeRecipe)
            {
                await Application.Current.MainPage.DisplayAlert("Figyelmeztetés", "Nincs elég hozzávalód a recepthez", "OK");
                return;
            }

            // Confirmation dialog
            bool confirm = await Application.Current.MainPage.DisplayAlert("Megerősítés",
                "Biztosan elkészíted a receptet? A szükséges hozzávalók levonásra kerülnek a készletedből.",
                "Igen", "Nem");

            if (!confirm)
                return;

            try
            {
                IsLoading = true;
                Debug.WriteLine("Making recipe and removing ingredients from storage");

                bool success = true;
                List<string> failedItems = new List<string>();

                // Load fresh stored items data
                var storedItemsResponse = await _storedItemsApiCommand.GetStoredItems(_userId);
                if (!storedItemsResponse.IsSuccessStatusCode || storedItemsResponse.Content == null)
                {
                    Debug.WriteLine($"Failed to load stored items: {storedItemsResponse.Error?.Content}");
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a tárolt termékeket", "OK");
                    return;
                }

                var storedItems = storedItemsResponse.Content;

                // Process each ingredient
                foreach (var ingredient in Ingredients)
                {
                    var storedItem = storedItems.FirstOrDefault(si => si.ItemId == ingredient.Item.Id);

                    if (storedItem == null)
                    {
                        success = false;
                        failedItems.Add(ingredient.Item.Name);
                        continue;
                    }

                    // Calculate new quantity
                    int newQuantity = storedItem.Quantity - ingredient.Quantity;

                    if (newQuantity < 0)
                    {
                        success = false;
                        failedItems.Add(ingredient.Item.Name);
                        continue;
                    }

                    if (newQuantity == 0)
                    {
                        // Delete the item if quantity is zero
                        var deleteResponse = await _storedItemsApiCommand.DeleteStoredItem(storedItem);
                        if (!deleteResponse.IsSuccessStatusCode)
                        {
                            success = false;
                            failedItems.Add(ingredient.Item.Name);
                            Debug.WriteLine($"Failed to delete item {ingredient.Item.Name}: {deleteResponse.Error?.Content}");
                        }
                        else
                        {
                            Debug.WriteLine($"Deleted item {ingredient.Item.Name} from storage");
                        }
                    }
                    else
                    {
                        // Update the item with new quantity
                        storedItem.Quantity = newQuantity;
                        var updateResponse = await _storedItemsApiCommand.AddStoredItem(storedItem);
                        if (!updateResponse.IsSuccessStatusCode)
                        {
                            success = false;
                            failedItems.Add(ingredient.Item.Name);
                            Debug.WriteLine($"Failed to update item {ingredient.Item.Name}: {updateResponse.Error?.Content}");
                        }
                        else
                        {
                            Debug.WriteLine($"Updated item {ingredient.Item.Name} to quantity {newQuantity}");
                        }
                    }
                }

                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Siker", "Recept elkészítve! A hozzávalók levonásra kerültek a készletedből.", "OK");

                    // Refresh ingredients to show new quantities
                    await LoadIngredientsAsync(RecipeId);
                    UpdateCanMakeRecipe();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Hiba",
                        $"Nem sikerült minden hozzávalót levonni a készletedből. Problémás termékek: {string.Join(", ", failedItems)}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error making recipe: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Hiba történt a recept elkészítése közben", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}