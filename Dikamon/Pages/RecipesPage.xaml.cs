using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages
{
    public partial class RecipesPage : ContentPage
    {
        private readonly RecipesViewModel _viewModel;

        public RecipesPage(RecipesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Refresh data when page appears
            if (_viewModel != null)
            {
                _viewModel.RefreshCommand.Execute(null);
            }
        }

        private void RecipeType_Changed(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                // Make sure SelectedRecipeType is updated before filtering
                if (sender is Picker picker && picker.SelectedItem is string selectedType)
                {
                    _viewModel.SelectedRecipeType = selectedType;
                }

                // Reset to page 1 when filter changes
                _viewModel.CurrentPage = 1;
                _viewModel.FilterRecipesByTypeCommand.Execute(null);
            }
        }
    }
}