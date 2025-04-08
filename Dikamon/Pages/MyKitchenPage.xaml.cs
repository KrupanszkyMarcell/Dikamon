using Dikamon.ViewModels;

namespace Dikamon.Pages;

public partial class MyKitchenPage : ContentPage
{
    private readonly MyKitchenViewModel _viewModel;

    public MyKitchenPage(MyKitchenViewModel vm)
    {
        InitializeComponent();
        this.BindingContext = vm;
    }


}