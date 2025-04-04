using Dikamon.ViewModels;

namespace Dikamon.Pages
{
    public partial class MainPage : ContentPage
    {
        

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            this.BindingContext = vm;
        }

    }

}
