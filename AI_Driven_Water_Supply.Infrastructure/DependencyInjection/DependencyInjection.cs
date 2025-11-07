using Microsoft.Extensions.DependencyInjection;
using Supabase;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Infrastructure.Services;

namespace AI_Driven_Water_Supply.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        var client = new Client(
            "https://YOUR_PROJECT_URL.supabase.co",
            "YOUR_PUBLIC_ANON_KEY"
        );

        services.AddSingleton(client);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWaterService, WaterService>(); // if you have IWaterService

        return services;
    }
}
