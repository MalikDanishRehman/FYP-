using DotNetEnv;
using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, ".env"));

// Register Infrastructure (DI + Supabase)
builder.Services.AddInfrastructure(builder.Configuration);

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery(); // For form security

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
