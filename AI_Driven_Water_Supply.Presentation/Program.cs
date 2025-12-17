using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ADD SERVICES FOR COOKIES & CONTROLLERS ---
builder.Services.AddControllers();          // API Controllers ke liye
builder.Services.AddHttpContextAccessor();  // Cookies read karne ke liye
builder.Services.AddHttpClient();           // API calls ke liye

// --- SUPABASE CONFIG ---
var supabaseUrl = "https://wejwiduabjiwztezvkdz.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndlandpZHVhYmppd3p0ZXp2a2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5MTQzMDgsImV4cCI6MjA4MDQ5MDMwOH0.nW7d_di1l2NTIMtSup4fCfrYJ9ZhW3U4uylAB6EOrvs";

var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
    Schema = "public"
};

builder.Services.AddScoped<Supabase.Client>(provider =>
{
    return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

// Auth Service
builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IAuthService, AI_Driven_Water_Supply.Infrastructure.Services.AuthService>();
// Add this line
builder.Services.AddScoped<AI_Driven_Water_Supply.Presentation.Services.ToastService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// --- 2. MAP CONTROLLERS ---
app.MapControllers(); // 👈 Yeh zaroori hai AuthController chalane ke liye

app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();