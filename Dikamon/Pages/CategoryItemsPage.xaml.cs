using Dikamon.ViewModels;

namespace Dikamon.Pages;

[QueryProperty(nameof(CategoryName), "categoryName")]
[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class CategoryItemsPage : ContentPage
{
    private readonly CategoryItemsViewModel _viewModel;

    public string CategoryName { get; set; }
    public string CategoryId { get; set; }

    public CategoryItemsPage(CategoryItemsViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        this.BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (!string.IsNullOrEmpty(CategoryName) && !string.IsNullOrEmpty(CategoryId))
            {
                if (int.TryParse(CategoryId, out int categoryIdInt))
                {
                    await _viewModel.Initialize(CategoryName, categoryIdInt);
                }
                else
                {
                    await DisplayAlert("Error", $"Invalid category ID format: {CategoryId}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Category information is missing", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
}