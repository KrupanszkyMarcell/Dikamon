using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Api;
using Dikamon.Models;
using Dikamon.Pages;

namespace Dikamon.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IUserApiCommand _userApiCommand;

        [ObservableProperty]
        public Users user = new Users();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _confirmPassword;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [ObservableProperty]
        private bool _isConfirmPasswordVisible;

        public RegisterViewModel(IUserApiCommand userApiCommand)
        {
            _userApiCommand = userApiCommand;
        }

        [RelayCommand]
        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(User.Name))
            {
                await Application.Current.MainPage.DisplayAlert("Registration", "Please enter your name", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Email))
            {
                await Application.Current.MainPage.DisplayAlert("Registration", "Please enter your email", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Password))
            {
                await Application.Current.MainPage.DisplayAlert("Registration", "Please enter a password", "OK");
                return;
            }

            if (User.Password.Length < 8)
            {
                await Application.Current.MainPage.DisplayAlert("Registration", "Password must be at least 8 characters long", "OK");
                return;
            }

            if (User.Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Registration", "Passwords do not match", "OK");
                return;
            }

            try
            {
                IsLoading = true; // Show loading indicator
                var response = await _userApiCommand.RegisterUser(User);
                if (response.IsSuccessStatusCode)
                {
                    var successResponse = response.Content;
                    await Application.Current.MainPage.DisplayAlert("Registration", "Registration successful", "OK");

                    // Navigate to login page after successful registration
                    await Shell.Current.GoToAsync($"{nameof(LoginPage)}", true);
                }
                else
                {
                    var errorResponse = await response.Error.GetContentAsAsync<ErrorMessage>();
                    await Application.Current.MainPage.DisplayAlert("Registration", $"Registration failed: {errorResponse.hu}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Registration", $"Registration failed: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false; // Hide loading indicator
            }
        }

        [RelayCommand]
        async Task GoToLoginPage()
        {
            await Shell.Current.GoToAsync($"{nameof(LoginPage)}", true);
        }

        [RelayCommand]
        void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        void ToggleConfirmPasswordVisibility()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }
    }
}