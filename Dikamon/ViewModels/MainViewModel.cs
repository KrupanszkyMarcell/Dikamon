using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Pages;
using Dikamon.Services;

namespace Dikamon.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading = true;

        private readonly ITokenService _tokenService;

        public MainViewModel(ITokenService tokenService = null)
        {
            _tokenService = tokenService;

            // Start checking for previous login
            Task.Run(async () => await CheckForStoredCredentialsAsync());
        }

        private async Task CheckForStoredCredentialsAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("Checking for stored credentials...");

                // Add a small delay to ensure the loading indicator is visible
                await Task.Delay(1000);

                if (_tokenService != null)
                {
                    bool isValid = await _tokenService.ValidateToken();

                    if (isValid)
                    {
                        Debug.WriteLine("Valid token found, navigating to main app page");
                        await Shell.Current.GoToAsync("//AfterLoginMainPage", true);
                        return; // Exit early as we're navigating away
                    }
                    else
                    {
                        Debug.WriteLine("No valid token found with token service");
                    }
                }
                else
                {
                    // Fallback check without token service
                    var token = await SecureStorage.GetAsync("token");
                    var userJson = await SecureStorage.GetAsync("user");

                    if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
                    {
                        Debug.WriteLine("Credentials found in secure storage, attempting navigation");
                        await Shell.Current.GoToAsync("//AfterLoginMainPage", true);
                        return; // Exit early as we're navigating away
                    }
                    else
                    {
                        Debug.WriteLine("No valid credentials found in secure storage");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for stored credentials: {ex.Message}");
            }
            finally
            {
                // Only hide the loading indicator if we're still on this page
                IsLoading = false;
                Debug.WriteLine("Finished checking credentials, loading state set to false");
            }
        }

        [RelayCommand]
        async Task GoToLoginPage()
        {
            await Shell.Current.GoToAsync($"{nameof(LoginPage)}", true);
        }

        [RelayCommand]
        async Task GoToRegisterPage()
        {
            await Shell.Current.GoToAsync($"{nameof(RegisterPage)}", true);
        }
    }
}