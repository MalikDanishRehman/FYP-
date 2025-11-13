using DotNetEnv;
using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;

var builder = WebApplication.CreateBuilder(args);

// ✅ Load .env file (from parent or same directory)
Env.Load(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, ".env"));

// ✅ Register Supabase + Auth + Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// ✅ Razor + Antiforgery support
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery(); // 🧩 Required for Blazor Server forms

var app = builder.Build();

// ✅ Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ Add antiforgery middleware (must be between routing & endpoints)
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
