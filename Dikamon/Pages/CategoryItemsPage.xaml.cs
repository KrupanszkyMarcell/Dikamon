using Dikamon.ViewModels;

namespace Dikamon.Pages;

public partial class CategoryItemsPage : ContentPage
{
    private readonly CategoryItemsViewModel _viewModel;

    public CategoryItemsPage(CategoryItemsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Make sure we have the route parameters
        var navigationParameter = GetNavigationParameter(Shell.Current.CurrentState);
        if (navigationParameter != null &&
            navigationParameter.ContainsKey("categoryName") &&
            navigationParameter.ContainsKey("categoryId"))
        {
            string categoryName = navigationParameter["categoryName"].ToString();
            int categoryId = Convert.ToInt32(navigationParameter["categoryId"]);

            await _viewModel.Initialize(categoryName, categoryId);
        }
    }

    private IDictionary<string, object> GetNavigationParameter(ShellNavigationState state)
    {
        // Implement the logic to extract navigation parameters from the state
        // This is a placeholder implementation and should be replaced with actual logic
        return new Dictionary<string, object>();
    }
}
