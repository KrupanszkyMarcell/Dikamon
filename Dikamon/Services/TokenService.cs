using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public TokenService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<string> GetToken()
        {
            return await SecureStorage.GetAsync("token") ?? string.Empty;
        }

        public async Task<bool> ValidateToken()
        {
            var token = await GetToken();

            // If no token exists, it's not valid
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                // Try to use any authenticated API to validate the token
                // For simplicity, we'll use the ItemTypesApiCommand since it's lightweight
                var itemTypesApi = _serviceProvider.GetService<IItemTypesApiCommand>();
                if (itemTypesApi != null)
                {
                    var response = await itemTypesApi.GetItemTypesLength();

                    // If we get a successful response, the token is valid
                    return response.IsSuccessStatusCode;
                }

                // If we couldn't get the API service, try to refresh the token as a fallback
                return await RefreshToken();
            }
            catch
            {
                // On exception, try to refresh the token
                return await RefreshToken();
            }
        }

        public async Task<bool> RefreshToken()
        {
            await _semaphore.WaitAsync();
            try
            {
                var email = await SecureStorage.GetAsync("userEmail");
                var password = await SecureStorage.GetAsync("userPassword");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return false;
                }
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                var tempApi = RestService.For<IUserApiCommand>(client);

                try
                {
                    var user = new Users { Email = email, Password = password };
                    var response = await tempApi.LoginUser(user);

                    if (response.IsSuccessStatusCode && response.Content?.Token != null)
                    {
                        await SecureStorage.SetAsync("token", response.Content.Token);

                        // Also update the stored user data
                        var userJson = System.Text.Json.JsonSerializer.Serialize(response.Content);
                        await SecureStorage.SetAsync("user", userJson);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
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

                System.Diagnostics.Debug.WriteLine("All credentials have been cleared");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}