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
            Debug.WriteLine($"RecipeId property set to: {value}");
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
            Debug.WriteLine($"Setting RecipeId on ViewModel to {recipeIdInt}");

            // Use Device.BeginInvokeOnMainThread to ensure this runs on the UI thread
            MainThread.BeginInvokeOnMainThread(() => {
                if (_viewModel.RecipeId != recipeIdInt)
                {
                    _viewModel.RecipeId = recipeIdInt;
                }
            });
        }
        else
        {
            Debug.WriteLine($"Invalid RecipeId: {_recipeId}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Debug.WriteLine("RecipeDetailsPage.OnAppearing");
        UpdateViewModelRecipeId();
    }

    protected override void OnDisappearing()
    {
        Debug.WriteLine("RecipeDetailsPage.OnDisappearing");
        base.OnDisappearing();
    }
}