using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var userJson = await SecureStorage.GetAsync("user");
            if (string.IsNullOrEmpty(userJson))
                return string.Empty;
            try
            {
                var user = JsonSerializer.Deserialize<Users>(userJson);
                return user?.Token ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<bool> RefreshToken()
        {
            await _semaphore.WaitAsync();
            try
            {
                var userJson = await SecureStorage.GetAsync("user");
                if (string.IsNullOrEmpty(userJson))
                    return false;

                var user = JsonSerializer.Deserialize<Users>(userJson);
                if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
                    return false;

                var client = new HttpClient();
                client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                var tempApi = RestService.For<IUserApiCommand>(client);

                try
                {
                    var loginUser = new Users { Email = user.Email, Password = user.Password };
                    var response = await tempApi.LoginUser(loginUser);

                    if (response.IsSuccessStatusCode && response.Content?.Token != null)
                    {
                        user.Token = response.Content.Token;
                        await SecureStorage.SetAsync("user", JsonSerializer.Serialize(user));
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
