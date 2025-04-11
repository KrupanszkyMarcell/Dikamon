using Dikamon.ViewModels;

namespace Dikamon.Pages;

[QueryProperty(nameof(CategoryName), "categoryName")]
[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class CategoryItemsPage : ContentPage
{
    private readonly CategoryItemsViewModel _viewModel;

    public string CategoryName { get; set; }
    public string CategoryId { get; set; }

    public CategoryItemsPage(CategoryItemsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
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
                    await DisplayAlert("Hiba", "Érvénytelen kategória azonosító", "OK");
                }
            }
            else
            {
                await DisplayAlert("Hiba", "Hiányzó kategória információk", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            await DisplayAlert("Hiba", $"Hiba történt az oldal betöltésekor: {ex.Message}", "OK");
        }
    }
}