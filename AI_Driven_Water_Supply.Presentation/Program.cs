using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Infrastructure.Services;
using AI_Driven_Water_Supply.Presentation.Components;

var builder = WebApplication.CreateBuilder(args);

// ? Dependency Injection
builder.Services.AddScoped<IWaterService, WaterService>();

// ? Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ? Middleware order (important)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();       // Add this
app.UseAntiforgery();   // Must come after routing

// ? Map components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
