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

        [ObservableProperty]
        public Users user = new Users();

        public LoginViewModel(IUserApiCommand userApiCommand)
        {
            _userApiCommand = userApiCommand;
        }

        [RelayCommand]
        private async Task Login()
        {
            try
            {
                var response = await _userApiCommand.LoginUser(User);
                if (response.IsSuccessStatusCode)
                {
                    var successResponse = response.Content;
                    await Application.Current.MainPage.DisplayAlert("Login", "Login successful", "OK");
                    await SecureStorage.SetAsync(UserStorageKey, JsonSerializer.Serialize(successResponse));
                    await SecureStorage.SetAsync(TokenStorageKey, successResponse.Token);

                    // Save credentials for potential token refresh (securely)
                    await SecureStorage.SetAsync("userEmail", User.Email);
                    await SecureStorage.SetAsync("userPassword", User.Password);
                }
                else
                {
                    var errorResponse = await response.Error.GetContentAsAsync<ErrorMessage>();
                    await Application.Current.MainPage.DisplayAlert("Login", $"Login failed: {errorResponse.hu}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Login", "Login failed", "OK");
            }
        }
        [RelayCommand]
        private async Task Test()
        {
            try
            {
                // This will automatically trigger the token refresh if needed
                var response = await _userApiCommand.GetUserById(2);

                // If we get here, the request was successful (possibly after a token refresh)
                var user = response.Content;
                await Application.Current.MainPage.DisplayAlert("Success", "User retrieved successfully", "OK");

                // Do something with the user...
            }
            catch (ApiException ex)
            {
                // This will only be hit if the token refresh also failed
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Session expired. Please log in again.", "OK");
                    // Navigate to login page
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
        }


        [RelayCommand]
        async Task GoToRegisterPage()
        {
            await Shell.Current.GoToAsync($"{nameof(RegisterPage)}", true);
        }
    }
}
