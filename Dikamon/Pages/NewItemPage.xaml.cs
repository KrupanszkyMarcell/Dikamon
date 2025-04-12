using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages
{
    [QueryProperty(nameof(CategoryId), "categoryId")]
    [QueryProperty(nameof(CategoryName), "categoryName")]
    public partial class NewItemPage : ContentPage
    {
        private readonly NewItemViewModel _viewModel;

        public string CategoryId { get; set; }
        public string CategoryName { get; set; }

        public NewItemPage(NewItemViewModel vm)
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
                Debug.WriteLine($"NewItemPage.OnAppearing - CategoryName: {CategoryName}, CategoryId: {CategoryId}");

                if (!string.IsNullOrEmpty(CategoryId))
                {
                    if (int.TryParse(CategoryId, out int categoryIdInt))
                    {
                        Debug.WriteLine($"Initializing NewItemViewModel with CategoryId: {categoryIdInt}");
                        await _viewModel.Initialize(categoryIdInt, CategoryName ?? "Unknown Category");
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to parse category ID: {CategoryId}");
                        await DisplayAlert("Hiba", $"Érvénytelen kategória azonosító: {CategoryId}", "OK");
                    }
                }
                else
                {
                    Debug.WriteLine("CategoryId is null or empty");

                    // If no category ID is provided, we can either:
                    // 1. Show all categories in the picker (which we already do)
                    // 2. Or return to the previous page
                    await _viewModel.Initialize(0, "Összes kategória");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnAppearing: {ex.Message}");
                await DisplayAlert("Hiba", $"Hiba történt: {ex.Message}", "OK");
            }
        }
    }
}