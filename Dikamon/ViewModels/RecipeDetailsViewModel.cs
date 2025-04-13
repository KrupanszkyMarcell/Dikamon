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

        private int _userId;

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

            LoadUserIdAsync();
        }

        partial void OnRecipeIdChanged(int value)
        {
            if (value > 0)
            {
                Debug.WriteLine($"Recipe ID changed to {value}, loading recipe details");
                LoadRecipeDetailsAsync(value).ConfigureAwait(false);
            }
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
                        Debug.WriteLine($"User ID loaded: {_userId}");
                    }
                    else
                    {
                        Debug.WriteLine("User or User.Id is null after deserialization");
                    }
                }
                else
                {
                    // Fallback: Try to get user ID from a different source
                    var userIdStr = await SecureStorage.GetAsync("userId");
                    if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                    {
                        _userId = userId;
                        Debug.WriteLine($"User ID loaded from userId key: {_userId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user ID: {ex.Message}");
            }
        }

        private async Task LoadRecipeDetailsAsync(int recipeId)
        {
            try
            {
                if (_userId <= 0)
                {
                    Debug.WriteLine("Cannot load recipe details: User ID is not available");
                    await Application.Current.MainPage.DisplayAlert("Hiba", "A felhasználói adatok nem elérhetők", "OK");
                    return;
                }

                IsLoading = true;
                Debug.WriteLine($"Loading recipe details for ID: {recipeId}");

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
                    await Application.Current.MainPage.DisplayAlert("Hiba", "Nem sikerült betölteni a recept részleteit", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recipe details: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Hiba", "Hiba történt a recept betöltése közben", "OK");
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
            CanMakeRecipe = Ingredients.All(i => i.HasEnough);
            Debug.WriteLine($"Can make recipe: {CanMakeRecipe}");
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

    // Helper class for ingredients with comparison between required and available
    public class IngredientViewModel : ObservableObject
    {
        public Items Item { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
        public bool HasEnough { get; set; }
    }

    // Helper class for instruction steps
    public class InstructionStepViewModel : ObservableObject
    {
        public int StepNumber { get; set; }
        public string Description { get; set; }
    }
}