using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Infrastructure.Services;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supabase;

// ✅ Alias to avoid Client ambiguity
using SupabaseClient = Supabase.Client;

namespace AI_Driven_Water_Supply.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Load .env
            Env.TraversePath().Load();

            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                throw new Exception("Supabase URL or Key missing!");

            // Initialize Supabase client
            var client = new SupabaseClient(supabaseUrl, supabaseKey);

            // Async initialization (non-blocking)
            _ = client.InitializeAsync();

            // Register in DI
            services.AddSingleton(client);
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
