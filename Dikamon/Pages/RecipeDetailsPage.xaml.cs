using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages;

[QueryProperty(nameof(RecipeId), "recipeId")]
public partial class RecipeDetailsPage : ContentPage
{
    private readonly RecipeDetailsViewModel _viewModel;
    private string _recipeId;

    public string RecipeId
    {
        get => _recipeId;
        set
        {
            _recipeId = value;
            UpdateViewModelRecipeId();
        }
    }

    public RecipeDetailsPage(RecipeDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void UpdateViewModelRecipeId()
    {
        if (!string.IsNullOrEmpty(_recipeId) && int.TryParse(_recipeId, out int recipeIdInt))
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (_viewModel.RecipeId != recipeIdInt)
                {
                    _viewModel.RecipeId = recipeIdInt;
                }
            });
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateViewModelRecipeId();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}