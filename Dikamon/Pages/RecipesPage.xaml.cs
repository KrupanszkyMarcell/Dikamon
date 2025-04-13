using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages
{
    public partial class RecipesPage : ContentPage
    {
        private readonly RecipesViewModel _viewModel;
        private bool _isInitialized = false;

        public RecipesPage(RecipesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh data when page appears for the first time
            if (_viewModel != null)
            {
                Debug.WriteLine("RecipesPage appeared, triggering refresh");
                await _viewModel.LoadRecipeTypesCommand.ExecuteAsync(null);

                // Only load recipes on first appear to avoid resetting filters
                if (!_isInitialized)
                {
                    await _viewModel.LoadRecipesCommand.ExecuteAsync(null);
                    _isInitialized = true;
                }

                Debug.WriteLine("RecipesPage refresh completed");
            }
        }

        private void RecipeType_Changed(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                // Make sure SelectedRecipeType is updated before filtering
                if (sender is Picker picker && picker.SelectedItem is string selectedType)
                {
                    Debug.WriteLine($"Recipe type picker changed to: {selectedType}");
                    _viewModel.SelectedRecipeType = selectedType;

                    // Reset to page 1 when filter changes
                    _viewModel.CurrentPage = 1;

                    // Force a reload with the new filter
                    _viewModel.FilterRecipesByTypeCommand.Execute(null);
                }
            }
        }
    }
}