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
            builder.Services.AddSingleton<ITokenService, TokenService>(); // Add a token service

            builder.Services.AddSingleton<CustomAuthenticatedHttpClientHandler>(sp =>
            {
                var tokenService = sp.GetRequiredService<ITokenService>();
                return new CustomAuthenticatedHttpClientHandler(
                    // Get token function
                    async () => await tokenService.GetToken(),
                    // Refresh token function
                    async () => await tokenService.RefreshToken()
                );
            });
            builder.Services.AddRefitClient<IUserApiCommand>()
                            .ConfigureHttpClient(async (sp, client) =>
                            {
                                var handler = sp.GetRequiredService<CustomAuthenticatedHttpClientHandler>();
                                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await SecureStorage.GetAsync("token") ?? string.Empty);
                                client.BaseAddress = new Uri("https://dkapbackend-cre8fwf4hdejhtdq.germanywestcentral-01.azurewebsites.net/api");
                            })
                .AddHttpMessageHandler<CustomUserResponseHandler>()
                .AddHttpMessageHandler<CustomAuthenticatedHttpClientHandler>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<RegisterViewModel>();

            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
