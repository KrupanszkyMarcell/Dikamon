using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages;

public partial class MyKitchenPage : ContentPage
{
    private readonly MyKitchenViewModel _viewModel;

    public MyKitchenPage(MyKitchenViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        this.BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            _viewModel.RefreshCommand.Execute(null);
        }
    }
}