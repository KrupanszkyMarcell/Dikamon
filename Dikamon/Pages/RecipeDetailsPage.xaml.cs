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

        Debug.WriteLine("RecipeDetailsPage.OnAppearing");

        if (!string.IsNullOrEmpty(RecipeId) && int.TryParse(RecipeId, out int recipeIdInt))
        {
            Debug.WriteLine($"Setting RecipeId to {recipeIdInt}");
            if (_viewModel.RecipeId != recipeIdInt)
            {
                _viewModel.RecipeId = recipeIdInt;
            }
        }
        else
        {
            Debug.WriteLine($"Invalid RecipeId: {RecipeId}");
        }
    }

    protected override void OnDisappearing()
    {
        Debug.WriteLine("RecipeDetailsPage.OnDisappearing");
        base.OnDisappearing();
    }
}