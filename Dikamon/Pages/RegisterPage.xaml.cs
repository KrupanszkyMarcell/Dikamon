using Dikamon.ViewModels;

namespace Dikamon.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel vm)
	{
		InitializeComponent();
		this.BindingContext = vm;
    }
}