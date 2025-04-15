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
            Task.Run(async () => await CheckForStoredCredentialsAsync());
        }

        private async Task CheckForStoredCredentialsAsync()
        {
            try
            {
                IsLoading = true;
                await Task.Delay(1000);

                if (_tokenService != null)
                {
                    bool isValid = await _tokenService.ValidateToken();

                    if (isValid)
                    {
                        await Shell.Current.GoToAsync("//AfterLoginMainPage", true);
                        return; 
                    }

                }
                else
                {
                    // Fallback check without token service
                    var token = await SecureStorage.GetAsync("token");
                    var userJson = await SecureStorage.GetAsync("user");

                    if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
                    {
                        await Shell.Current.GoToAsync("//AfterLoginMainPage", true);
                        return; 
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                IsLoading = false;
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