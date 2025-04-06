using Dikamon.ViewModels;

namespace Dikamon.Pages;

public partial class AfterLoginMainPage : ContentPage
{
	public AfterLoginMainPage(AfterLoginMainViewModel vm)
	{
		InitializeComponent();
		this.BindingContext = vm;
    }
}