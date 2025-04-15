using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Api;
using Dikamon.DelegatingHandlers;
using Dikamon.Models;
using Dikamon.Pages;
using Refit;

namespace Dikamon.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserApiCommand _userApiCommand;
        private const string UserStorageKey = "user";
        private const string TokenStorageKey = "token";
        private const string UserIdKey = "userId";

        [ObservableProperty]
        public Users user = new Users();

        [ObservableProperty]
        private bool _isLoading;

        public LoginViewModel(IUserApiCommand userApiCommand)
        {
            _userApiCommand = userApiCommand;
        }

        [RelayCommand]
        private async Task Login()
        {
            try
            {
                IsLoading = true; // Show loading indicator
                System.Diagnostics.Debug.WriteLine($"Login attempt for user: {User.Email}");

                var response = await _userApiCommand.LoginUser(User);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Login successful for user: {User.Email}, ID: {response.Content.Id}");

                    // Save the entire user object
                    var userJson = JsonSerializer.Serialize(response.Content);
                    await SecureStorage.SetAsync(UserStorageKey, userJson);
                    System.Diagnostics.Debug.WriteLine($"User JSON saved: {userJson}");

                    // Also save the ID separately for easier access
                    if (response.Content.Id.HasValue)
                    {
                        await SecureStorage.SetAsync(UserIdKey, response.Content.Id.Value.ToString());
                        System.Diagnostics.Debug.WriteLine($"User ID saved separately: {response.Content.Id.Value}");
                    }

                    // Save the token as a plain string
                    await SecureStorage.SetAsync(TokenStorageKey, response.Content.Token);
                    System.Diagnostics.Debug.WriteLine($"Token saved, length: {response.Content.Token?.Length ?? 0}");

                    // Save email and password for potential token refresh
                    await SecureStorage.SetAsync("userEmail", User.Email);
                    await SecureStorage.SetAsync("userPassword", User.Password);

                    await Application.Current.MainPage.DisplayAlert("Login", "Login successful", "OK");
                    await Shell.Current.GoToAsync($"{nameof(AfterLoginMainPage)}", true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Login failed for user: {User.Email}");
                    var errorResponse = await response.Error.GetContentAsAsync<ErrorMessage>();
                    await Application.Current.MainPage.DisplayAlert("Login", $"Login failed: {errorResponse.hu}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Login", "Login failed", "OK");
            }
            finally
            {
                IsLoading = false; 
            }
        }


        [RelayCommand]
        async Task GoToRegisterPage()
        {
            await Shell.Current.GoToAsync($"{nameof(RegisterPage)}", true);
        }
    }
}