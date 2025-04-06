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
        Task StoreUserData(Users user);
        Task ClearUserData();
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
            {
                Debug.WriteLine("Using cached token");
                return _cachedToken;
            }

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
                _cachedToken = user?.Token;

                if (string.IsNullOrEmpty(_cachedToken))
                {
                    Debug.WriteLine("Token is empty or null from storage");
                    return string.Empty;
                }

                Debug.WriteLine("Token retrieved from storage successfully");
                return _cachedToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving token: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task StoreUserData(Users user)
        {
            if (user == null)
            {
                Debug.WriteLine("Cannot store null user");
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                _cachedUser = user;
                _cachedToken = user.Token;

                // Ensure we're not saving a null token
                if (!string.IsNullOrEmpty(_cachedToken))
                {
                    Debug.WriteLine($"Storing user with token in secure storage");
                    await SecureStorage.SetAsync(UserStorageKey, JsonSerializer.Serialize(user));
                }
                else
                {
                    Debug.WriteLine("User has no token, not storing");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error storing user data: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearUserData()
        {
            await _semaphore.WaitAsync();
            try
            {
                _cachedUser = null;
                _cachedToken = null;
                SecureStorage.Remove(UserStorageKey);
                Debug.WriteLine("User data cleared");
            }
            finally
            {
                _semaphore.Release();
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
                        // Update user ID if it was provided
                        if (response.Content.Id.HasValue)
                        {
                            user.Id = response.Content.Id;
                        }

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