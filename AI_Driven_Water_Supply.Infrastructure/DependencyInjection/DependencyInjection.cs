using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Infrastructure.Services;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supabase;

namespace AI_Driven_Water_Supply.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // ✅ Load .env from parent folder
            Env.Load(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, ".env"));

            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                throw new Exception("❌ Supabase URL or Key missing. Check your .env file.");

            // ✅ Create Supabase client
            var client = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoConnectRealtime = true
            });

            // ✅ Dependency Injection setup
            services.AddSingleton(client);
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
