using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register Supabase + Auth + WaterService
builder.Services.AddInfrastructure();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
