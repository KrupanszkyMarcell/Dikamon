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
            if (_viewModel != null)
            {
                await _viewModel.LoadRecipeTypesCommand.ExecuteAsync(null);
                if (!_isInitialized)
                {
                    await _viewModel.LoadRecipesCommand.ExecuteAsync(null);
                    _isInitialized = true;
                }
            }
        }

        private void RecipeType_Changed(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                if (sender is Picker picker && picker.SelectedItem is string selectedType)
                {
                    _viewModel.SelectedRecipeType = selectedType;
                    _viewModel.CurrentPage = 1;
                    _viewModel.FilterRecipesByTypeCommand.Execute(null);
                }
            }
        }
    }
}