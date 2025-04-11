using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Dikamon.ViewModels;
using Dikamon.Pages;
using Refit;
using Dikamon.Api;
using Dikamon.DelegatingHandlers;
using Dikamon.Models;
using Dikamon.Services;

namespace Dikamon
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Tiny5-Regular.ttf", "Tiny5");
                });

            builder.Services.AddTransient<CustomUserResponseHandler>();
            builder.Services.AddTransient<CustomAuthenticatedHttpClientHandler>(sp =>
            {
                var tokenService = sp.GetRequiredService<ITokenService>();
                var handler = new CustomAuthenticatedHttpClientHandler(
                    async () => await tokenService.GetToken(),
                    async () => await tokenService.RefreshToken()
                );
                return handler;
            });
            builder.Services.AddSingleton<ITokenService, TokenService>();

            builder.Services.AddRefitClient<IUserApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();
            builder.Services.AddRefitClient<IIngredientsApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                    client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();
            builder.Services.AddRefitClient<IItemsApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();
            builder.Services.AddRefitClient<IItemTypesApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();
            builder.Services.AddRefitClient<IRecipesApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();
            builder.Services.AddRefitClient<IStoredItemsApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();


            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<RegisterViewModel>();
            builder.Services.AddSingleton<MyKitchenPage>();
            builder.Services.AddSingleton<MyKitchenViewModel>();
            builder.Services.AddSingleton<AfterLoginMainPage>();
            builder.Services.AddSingleton<AfterLoginMainViewModel>();
            builder.Services.AddSingleton<CategoryItemsPage>();
            builder.Services.AddSingleton<CategoryItemsViewModel>();

            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(MyKitchenPage), typeof(MyKitchenPage));
            Routing.RegisterRoute(nameof(AfterLoginMainPage), typeof(AfterLoginMainPage));
            Routing.RegisterRoute(nameof(CategoryItemsPage), typeof(CategoryItemsPage));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
