using Dikamon.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dikamon
{
    public partial class AppShell : Shell
    {
        private ITokenService _tokenService;

        public AppShell()
        {
            InitializeComponent();
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);

            // This gets called after navigation, when the MauiContext is available
            if (_tokenService == null && Application.Current?.Handler?.MauiContext != null)
            {
                _tokenService = Application.Current.Handler.MauiContext.Services.GetService<ITokenService>();

                // Note: We're no longer checking credentials here
                // The MainViewModel now handles this with a loading screen
                Debug.WriteLine("AppShell initialized token service");
            }
        }
    }
}