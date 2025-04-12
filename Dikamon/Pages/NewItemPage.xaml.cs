using Dikamon.ViewModels;

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
                System.Diagnostics.Debug.WriteLine($"NewItemPage.OnAppearing - CategoryName: {CategoryName}, CategoryId: {CategoryId}");

                if (!string.IsNullOrEmpty(CategoryId))
                {
                    if (int.TryParse(CategoryId, out int categoryIdInt))
                    {
                        System.Diagnostics.Debug.WriteLine($"Initializing NewItemViewModel with CategoryId: {categoryIdInt}");
                        await _viewModel.Initialize(categoryIdInt, CategoryName);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to parse category ID: {CategoryId}");
                        await DisplayAlert("Error", $"Invalid category ID format: {CategoryId}", "OK");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CategoryId is null or empty");
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
}