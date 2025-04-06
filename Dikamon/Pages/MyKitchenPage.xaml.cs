using Dikamon.ViewModels;

namespace Dikamon.Pages;

public partial class MyKitchenPage : ContentPage
{
	public MyKitchenPage(MyKitchenViewModel vm)
	{
		InitializeComponent();
		this.BindingContext = vm;
    }
}