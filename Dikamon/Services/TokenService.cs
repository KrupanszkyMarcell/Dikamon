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
    }
}
