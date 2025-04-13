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
            // Register the token service first since other services depend on it
            builder.Services.AddSingleton<ITokenService, TokenService>();

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

            // Configure HTTP Client
            var apiBaseUrl = "https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api";

            // Register API clients
            builder.Services.AddRefitClient<IUserApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>(); ;

            builder.Services.AddRefitClient<IIngredientsApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>(); ;

            builder.Services.AddRefitClient<IItemsApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>(); 

            builder.Services.AddRefitClient<IItemTypesApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>(); ;

            builder.Services.AddRefitClient<IRecipesApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>(); ;

            builder.Services.AddRefitClient<IStoredItemsApiCommand>()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    var token = await SecureStorage.GetAsync("token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                })
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>(); ;

            // Register pages and view models
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<MyKitchenPage>();
            builder.Services.AddTransient<MyKitchenViewModel>();
            builder.Services.AddTransient<AfterLoginMainPage>();
            builder.Services.AddTransient<AfterLoginMainViewModel>();
            builder.Services.AddTransient<CategoryItemsPage>();
            builder.Services.AddTransient<CategoryItemsViewModel>();
            builder.Services.AddTransient<RecipesPage>();
            builder.Services.AddTransient<RecipesViewModel>();

            // Register NewItemPage and NewItemViewModel
            builder.Services.AddTransient<NewItemPage>();
            builder.Services.AddTransient<NewItemViewModel>();

            // Register converters
            builder.Services.AddSingleton<QuantityToTextConverter>();
            builder.Services.AddSingleton<InvertedBoolConverter>();
            builder.Services.AddSingleton<StringNotEmptyConverter>();
            builder.Services.AddSingleton<Services.ImageSourceConverter>();
            builder.Services.AddSingleton<GreaterThanOneConverter>();

            // Register routes
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(MyKitchenPage), typeof(MyKitchenPage));
            Routing.RegisterRoute(nameof(AfterLoginMainPage), typeof(AfterLoginMainPage));
            Routing.RegisterRoute(nameof(CategoryItemsPage), typeof(CategoryItemsPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
            Routing.RegisterRoute(nameof(RecipesPage), typeof(RecipesPage));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}