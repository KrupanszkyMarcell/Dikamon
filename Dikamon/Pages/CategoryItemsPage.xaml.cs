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
                    await DisplayAlert("Hiba", "�rv�nytelen kateg�ria azonos�t�", "OK");
                }
            }
            else
            {
                await DisplayAlert("Hiba", "Hi�nyz� kateg�ria inform�ci�k", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            await DisplayAlert("Hiba", $"Hiba t�rt�nt az oldal bet�lt�sekor: {ex.Message}", "OK");
        }
    }
}