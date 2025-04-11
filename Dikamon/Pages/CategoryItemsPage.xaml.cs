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
            System.Diagnostics.Debug.WriteLine($"CategoryItemsPage.OnAppearing - CategoryName: {CategoryName}, CategoryId: {CategoryId}");

            if (!string.IsNullOrEmpty(CategoryName) && !string.IsNullOrEmpty(CategoryId))
            {
                if (int.TryParse(CategoryId, out int categoryIdInt))
                {
                    System.Diagnostics.Debug.WriteLine($"Calling Initialize with CategoryName: {CategoryName}, CategoryId: {categoryIdInt}");
                    await _viewModel.Initialize(CategoryName, categoryIdInt);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to parse category ID: {CategoryId}");
                    await DisplayAlert("Error", $"Invalid category ID format: {CategoryId}", "OK");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("CategoryName or CategoryId is null or empty");
                await DisplayAlert("Error", "Category information is missing", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in OnAppearing: {ex.Message}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
}