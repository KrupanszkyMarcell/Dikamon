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
            if (!string.IsNullOrEmpty(_cachedToken) &&
                (DateTime.Now - _tokenCacheTime).TotalMinutes < 5)
            {
                return _cachedToken;
            }

            try
            {
                var token = await SecureStorage.GetAsync("token");

                if (!string.IsNullOrEmpty(token))
                {

                    _cachedToken = token;
                    _tokenCacheTime = DateTime.Now;
                    return token;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public async Task<bool> ValidateToken()
        {
            var token = await GetToken();

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                var itemTypesApi = _serviceProvider.GetService<IItemTypesApiCommand>();
                if (itemTypesApi != null)
                {
                    var response = await itemTypesApi.GetItemTypesLength();
                    var isValid = response.IsSuccessStatusCode;
                    return isValid;
                }
                return await RefreshToken();
            }
            catch (Exception ex)
            {
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
                        _cachedToken = response.Content.Token;
                        _tokenCacheTime = DateTime.Now;
                        var userJson = System.Text.Json.JsonSerializer.Serialize(response.Content);
                        await SecureStorage.SetAsync("user", userJson);

                        return true;
                    }
                    else
                    {
                        if (response.Error != null)
                        {
                        }
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
                SecureStorage.Remove("token");
                SecureStorage.Remove("user");
                SecureStorage.Remove("userEmail");
                SecureStorage.Remove("userPassword");
                SecureStorage.Remove("userId");
                _cachedToken = null;
                _tokenCacheTime = DateTime.MinValue;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}