using Dikamon.ViewModels;
using System.Diagnostics;

namespace Dikamon.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            _viewModel = vm;
            this.BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("MainPage appeared");
        }
    }
}