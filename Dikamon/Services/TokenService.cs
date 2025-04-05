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

        public async Task<bool> RefreshToken()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Get stored credentials
                var email = await SecureStorage.GetAsync("userEmail");
                var password = await SecureStorage.GetAsync("userPassword");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return false; // Can't refresh without credentials
                }

                // Create a temporary client without the token handler
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                var tempApi = RestService.For<IUserApiCommand>(client);

                // Try to login
                var user = new Users { Email = email, Password = password };

                try
                {
                    var response = await tempApi.LoginUser(user);

                    if (response.IsSuccessStatusCode && response.Content?.Token != null)
                    {
                        // Save the new token
                        await SecureStorage.SetAsync("token", response.Content.Token);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token refresh failed: {ex.Message}");
                }

                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
