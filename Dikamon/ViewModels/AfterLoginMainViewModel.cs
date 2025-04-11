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

            // Try to get the user name from secure storage
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
                // Handle exception or use default name
                System.Diagnostics.Debug.WriteLine($"Error loading user name: {ex.Message}");
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
            // Navigate to recipes page (to be implemented)
            await Shell.Current.DisplayAlert("Receptek", "A receptek funkcionalitás hamarosan elérhető lesz!", "OK");
        }

        [RelayCommand]
        async Task Logout()
        {
            bool answer = await Shell.Current.DisplayAlert("Kijelentkezés",
                "Biztosan ki szeretnél jelentkezni?", "Igen", "Nem");

            if (answer)
            {
                // Clear all stored credentials
                if (_tokenService != null)
                {
                    await _tokenService.ClearAllCredentials();
                }
                else
                {
                    // Fallback if token service isn't available
                    SecureStorage.Remove("token");
                    SecureStorage.Remove("user");
                    SecureStorage.Remove("userEmail");
                    SecureStorage.Remove("userPassword");
                    SecureStorage.Remove("userId");
                }

                // Navigate back to the main page
                await Shell.Current.GoToAsync("//MainPage", true);
            }
        }
    }
}