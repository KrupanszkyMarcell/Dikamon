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

        System.Diagnostics.Debug.WriteLine($"[TRACE] NewItemPage.OnAppearing - CategoryId: {CategoryId}, CategoryName: {CategoryName}");

        // Explicitly initialize the ViewModel with the CategoryId
        if (!string.IsNullOrEmpty(CategoryId))
        {
            System.Diagnostics.Debug.WriteLine($"[TRACE] Calling InitializeWithCategoryInfo with CategoryId: {CategoryId}");
            _viewModel.InitializeWithCategoryInfo(CategoryId);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ERROR] CategoryId is null or empty in NewItemPage.OnAppearing");
        }
    }
}