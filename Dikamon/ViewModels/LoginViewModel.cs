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
                    await SecureStorage.SetAsync(UserStorageKey,JsonSerializer.Serialize(successResponse));
                    await SecureStorage.SetAsync(TokenStorageKey, successResponse.Token);
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

        private IUserApiCommand Get_userApiCommand()
        {
            return _userApiCommand;
        }

        private void ConfigureHttpClient(string token, IUserApiCommand _userApiCommand)
        {
            var httpClientHandler = new CustomAuthenticatedHttpClientHandler(async () => await SecureStorage.GetAsync(TokenStorageKey));
            var httpClient = new HttpClient(httpClientHandler);
            var refitSettings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => httpClientHandler
            };
            _userApiCommand = RestService.For<IUserApiCommand>(httpClient, refitSettings);
        }
        [RelayCommand]
        async Task GoToRegisterPage()
        {
            await Shell.Current.GoToAsync($"{nameof(RegisterPage)}", true);
        }
    }
}
