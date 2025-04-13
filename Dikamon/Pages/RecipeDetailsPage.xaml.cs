using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages
{
    [QueryProperty(nameof(RecipeId), "recipeId")]
    public partial class RecipeDetailsPage : ContentPage
    {
        private readonly RecipeDetailsViewModel _viewModel;
        public string RecipeId { get; set; }

        public RecipeDetailsPage(RecipeDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Apply the RecipeId property to the ViewModel
            if (!string.IsNullOrEmpty(RecipeId) && int.TryParse(RecipeId, out int recipeIdInt))
            {
                Debug.WriteLine($"RecipeDetailsPage setting RecipeId from QueryProperty: {recipeIdInt}");
                _viewModel.RecipeId = recipeIdInt;
            }
            else
            {
                Debug.WriteLine($"RecipeDetailsPage invalid RecipeId: {RecipeId}");
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "Érvénytelen recept azonosító";
            }
        }
    }
}