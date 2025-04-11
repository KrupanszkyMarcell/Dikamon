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

        protected override async void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);

            // This gets called after navigation, when the MauiContext is available
            if (_tokenService == null && Application.Current?.Handler?.MauiContext != null)
            {
                _tokenService = Application.Current.Handler.MauiContext.Services.GetService<ITokenService>();
                await CheckForStoredCredentials();
            }
        }

        private async Task CheckForStoredCredentials()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Checking for stored credentials...");

                // If token service is available, use it to validate the token
                if (_tokenService != null && await _tokenService.ValidateToken())
                {
                    System.Diagnostics.Debug.WriteLine("Valid token found, navigating to main app page");

                    // Navigate to the main page after login
                    await Shell.Current.GoToAsync("//AfterLoginMainPage", true);
                }
                else
                {
                    // Direct validation without token service (fallback)
                    var token = await SecureStorage.GetAsync("token");
                    var userJson = await SecureStorage.GetAsync("user");

                    if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
                    {
                        System.Diagnostics.Debug.WriteLine("Credentials found, but token service unavailable. Attempting navigation.");
                        await Shell.Current.GoToAsync("//AfterLoginMainPage", true);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid credentials found, staying on welcome page");
                    }
                }
            }
            catch (Exception ex)
            {
                // Just log the error and continue to the default page (MainPage)
                System.Diagnostics.Debug.WriteLine($"Error checking for stored credentials: {ex.Message}");
            }
        }
    }
}