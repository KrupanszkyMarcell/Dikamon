using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages;

[QueryProperty(nameof(RecipeId), "recipeId")]
public partial class RecipeDetailsPage : ContentPage
{
    private readonly RecipeDetailsViewModel _viewModel;
    public string RecipeId { get; set; }

    public RecipeDetailsPage(RecipeDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        this.BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Pass the recipeId to the ViewModel
        if (!string.IsNullOrEmpty(RecipeId) && int.TryParse(RecipeId, out int recipeId))
        {
            Debug.WriteLine($"RecipeDetailsPage: Setting RecipeId in ViewModel to: {recipeId}");
            _viewModel.RecipeId = recipeId;
        }
        else
        {
            Debug.WriteLine($"RecipeDetailsPage: Invalid RecipeId: {RecipeId}");
            DisplayAlert("Hiba", "Érvénytelen recept azonosító", "OK");
        }
    }
}