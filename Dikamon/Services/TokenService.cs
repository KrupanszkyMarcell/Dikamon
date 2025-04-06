using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Dikamon.Api;
using Dikamon.Models;
using Refit;

namespace Dikamon.Services
{
    public interface ITokenService
    {
        Task<string> GetToken();
        Task<bool> RefreshToken();
        Task<Users> GetCurrentUser();
    }

    public class TokenService : ITokenService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private const string UserStorageKey = "user";
        private string _cachedToken;
        private Users _cachedUser;

        public TokenService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<Users> GetCurrentUser()
        {
            if (_cachedUser != null)
                return _cachedUser;

            var userJson = await SecureStorage.GetAsync(UserStorageKey);
            if (string.IsNullOrEmpty(userJson))
                return null;

            try
            {
                _cachedUser = JsonSerializer.Deserialize<Users>(userJson);
                return _cachedUser;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deserializing user: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetToken()
        {
            // Return cached token if available
            if (!string.IsNullOrEmpty(_cachedToken))
                return _cachedToken;

            // Try getting the user first
            var userJson = await SecureStorage.GetAsync(UserStorageKey);
            if (string.IsNullOrEmpty(userJson))
            {
                Debug.WriteLine("No user found in secure storage");
                return string.Empty;
            }

            try
            {
                var user = JsonSerializer.Deserialize<Users>(userJson);
                _cachedUser = user;
                _cachedToken = user?.Token ?? string.Empty;

                Debug.WriteLine($"Token retrieved: {!string.IsNullOrEmpty(_cachedToken)}");
                return _cachedToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving token: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> RefreshToken()
        {
            await _semaphore.WaitAsync();
            try
            {
                Debug.WriteLine("Attempting to refresh token");
                var userJson = await SecureStorage.GetAsync(UserStorageKey);
                if (string.IsNullOrEmpty(userJson))
                {
                    Debug.WriteLine("No user found for token refresh");
                    return false;
                }

                var user = JsonSerializer.Deserialize<Users>(userJson);
                if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
                {
                    Debug.WriteLine("Invalid user data for token refresh");
                    return false;
                }

                try
                {
                    var client = new HttpClient();
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var api = RestService.For<IUserApiCommand>(client);

                    var loginUser = new Users { Email = user.Email, Password = user.Password };
                    var response = await api.LoginUser(loginUser);

                    if (response.IsSuccessStatusCode && response.Content?.Token != null)
                    {
                        user.Token = response.Content.Token;

                        _cachedToken = user.Token;
                        _cachedUser = user;
                        await SecureStorage.SetAsync(UserStorageKey, JsonSerializer.Serialize(user));

                        Debug.WriteLine("Token refreshed successfully");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("Failed to refresh token");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception during token refresh: {ex.Message}");
                    return false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}