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

        public RegisterViewModel(IUserApiCommand userApiCommand)
        {
            _userApiCommand = userApiCommand;
        }

        [RelayCommand]
        private async Task Register()
        {
            try
            {
                var response = await _userApiCommand.RegisterUser(User);
                if (response.IsSuccessStatusCode)
                {
                    var successResponse = response.Content;
                    await Application.Current.MainPage.DisplayAlert("Registration", "Registration successful", "OK");
                }
                else
                {
                    var errorResponse = await response.Error.GetContentAsAsync<ErrorMessage>();
                    await Application.Current.MainPage.DisplayAlert("Registration", $"Registration failed: {errorResponse.hu}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Registration", "Registration failed", "OK");
            }
        }
        [RelayCommand]
        async Task GoToLoginPage()
        {
            await Shell.Current.GoToAsync($"{nameof(LoginPage)}", true);
        }
    }
}
