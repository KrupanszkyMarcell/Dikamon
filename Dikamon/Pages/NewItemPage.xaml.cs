using Dikamon.ViewModels;

namespace Dikamon.Pages;

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrEmpty(CategoryId))
        {
            _viewModel.InitializeWithCategoryInfo(CategoryId);
        }
    }
}