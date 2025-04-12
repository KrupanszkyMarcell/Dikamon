using System;
using System.Diagnostics;
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
        Task<bool> ValidateToken();
        Task ClearAllCredentials();
    }

    public class TokenService : ITokenService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private string _cachedToken = null;
        private DateTime _tokenCacheTime = DateTime.MinValue;

        public TokenService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<string> GetToken()
        {
            // Check if we have a cached token that's less than 5 minutes old
            if (!string.IsNullOrEmpty(_cachedToken) &&
                (DateTime.Now - _tokenCacheTime).TotalMinutes < 5)
            {
                Debug.WriteLine("[TOKEN] Using cached token");
                return _cachedToken;
            }

            try
            {
                var token = await SecureStorage.GetAsync("token");

                if (!string.IsNullOrEmpty(token))
                {
                    // Cache the token
                    _cachedToken = token;
                    _tokenCacheTime = DateTime.Now;

                    Debug.WriteLine("[TOKEN] Retrieved token from secure storage");
                    return token;
                }
                else
                {
                    Debug.WriteLine("[TOKEN] No token found in secure storage");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TOKEN] Error getting token: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> ValidateToken()
        {
            var token = await GetToken();

            // If no token exists, it's not valid
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("[TOKEN] No token to validate");
                return false;
            }

            try
            {
                // Try to use any authenticated API to validate the token
                // For simplicity, we'll use the ItemTypesApiCommand since it's lightweight
                var itemTypesApi = _serviceProvider.GetService<IItemTypesApiCommand>();
                if (itemTypesApi != null)
                {
                    Debug.WriteLine("[TOKEN] Validating token with API call");
                    var response = await itemTypesApi.GetItemTypesLength();

                    // If we get a successful response, the token is valid
                    var isValid = response.IsSuccessStatusCode;
                    Debug.WriteLine($"[TOKEN] Token validation result: {isValid}");
                    return isValid;
                }
                else
                {
                    Debug.WriteLine("[TOKEN] ItemTypesApiCommand service not available");
                }

                // If we couldn't get the API service, try to refresh the token as a fallback
                Debug.WriteLine("[TOKEN] Falling back to token refresh");
                return await RefreshToken();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TOKEN] Error validating token: {ex.Message}");
                // On exception, try to refresh the token
                return await RefreshToken();
            }
        }

        public async Task<bool> RefreshToken()
        {
            await _semaphore.WaitAsync();
            try
            {
                Debug.WriteLine("[TOKEN] Attempting to refresh token");

                var email = await SecureStorage.GetAsync("userEmail");
                var password = await SecureStorage.GetAsync("userPassword");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    Debug.WriteLine("[TOKEN] Cannot refresh - missing credentials");
                    return false;
                }

                Debug.WriteLine($"[TOKEN] Using stored credentials to refresh token for {email}");
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                var tempApi = RestService.For<IUserApiCommand>(client);

                try
                {
                    var user = new Users { Email = email, Password = password };
                    var response = await tempApi.LoginUser(user);

                    if (response.IsSuccessStatusCode && response.Content?.Token != null)
                    {
                        Debug.WriteLine("[TOKEN] Successfully obtained new token");

                        // Update the stored token
                        await SecureStorage.SetAsync("token", response.Content.Token);

                        // Update cache
                        _cachedToken = response.Content.Token;
                        _tokenCacheTime = DateTime.Now;

                        // Also update the stored user data
                        var userJson = System.Text.Json.JsonSerializer.Serialize(response.Content);
                        await SecureStorage.SetAsync("user", userJson);

                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("[TOKEN] Failed to get new token from API");
                        if (response.Error != null)
                        {
                            Debug.WriteLine($"[TOKEN] Error: {response.Error.Content}");
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TOKEN] Exception during token refresh: {ex.Message}");
                    return false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearAllCredentials()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Clear all stored credentials
                SecureStorage.Remove("token");
                SecureStorage.Remove("user");
                SecureStorage.Remove("userEmail");
                SecureStorage.Remove("userPassword");
                SecureStorage.Remove("userId");

                // Clear cache
                _cachedToken = null;
                _tokenCacheTime = DateTime.MinValue;

                Debug.WriteLine("[TOKEN] All credentials have been cleared");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}