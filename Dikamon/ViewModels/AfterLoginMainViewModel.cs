using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dikamon.Pages;
using Dikamon.Services;

namespace Dikamon.ViewModels
{
    public partial class AfterLoginMainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string userName = string.Empty;

        private readonly ITokenService _tokenService;

        public AfterLoginMainViewModel(ITokenService tokenService = null)
        {
            _tokenService = tokenService;

            LoadUserName();
        }

        private async void LoadUserName()
        {
            try
            {
                var userJson = await SecureStorage.GetAsync("user");
                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<Models.Users>(userJson);
                    if (user != null && !string.IsNullOrEmpty(user.Name))
                    {
                        UserName = user.Name;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        async Task GoToMyKitchen()
        {
            await Shell.Current.GoToAsync(nameof(MyKitchenPage), true);
        }

        [RelayCommand]
        async Task GoToRecipes()
        {

            await Shell.Current.GoToAsync(nameof(RecipesPage), true);
        }

        [RelayCommand]
        async Task Logout()
        {
            bool answer = await Shell.Current.DisplayAlert("Kijelentkezés",
                "Biztosan ki szeretnél jelentkezni?", "Igen", "Nem");

            if (answer)
            {

                if (_tokenService != null)
                {
                    await _tokenService.ClearAllCredentials();
                }
                else
                {

                    SecureStorage.Remove("token");
                    SecureStorage.Remove("user");
                    SecureStorage.Remove("userEmail");
                    SecureStorage.Remove("userPassword");
                    SecureStorage.Remove("userId");
                }


                await Shell.Current.GoToAsync("//MainPage", true);
            }
        }
    }
}