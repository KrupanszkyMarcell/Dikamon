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

            // Register services and handlers
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

            // Register API clients
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

            // Register other API clients...
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

            // Register pages and view models
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

            // Register converters
            builder.Services.AddSingleton<QuantityToTextConverter>();

            Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
            Routing.RegisterRoute(nameof(Pages.RegisterPage), typeof(Pages.RegisterPage));
            Routing.RegisterRoute(nameof(Pages.MyKitchenPage), typeof(Pages.MyKitchenPage));
            Routing.RegisterRoute(nameof(Pages.AfterLoginMainPage), typeof(Pages.AfterLoginMainPage));
            Routing.RegisterRoute(nameof(Pages.CategoryItemsPage), typeof(Pages.CategoryItemsPage));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}