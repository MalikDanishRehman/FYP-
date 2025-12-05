using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// --- STEP 1: DIRECT CONFIGURATION ---
var supabaseUrl = "https://wejwiduabjiwztezvkdz.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndlandpZHVhYmppd3p0ZXp2a2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5MTQzMDgsImV4cCI6MjA4MDQ5MDMwOH0.nW7d_di1l2NTIMtSup4fCfrYJ9ZhW3U4uylAB6EOrvs";

Console.WriteLine($"✅ Supabase URL Loaded: {supabaseUrl}");

// --- STEP 2: REGISTER SUPABASE WITH SCHEMA ---
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
    // 👇 YEH LINE SABSE ZAROORI HAI DATABASE ACCESS KE LIYE
    Schema = "public"
};

// Scoped Service Register (With Initialization)
builder.Services.AddScoped<Supabase.Client>(provider =>
{
    var client = new Supabase.Client(supabaseUrl, supabaseKey, options);
    // Client ko initialize karo taake purana session load ho sake
    client.InitializeAsync().Wait();
    return client;
});

// Auth Service Registration
builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IAuthService, AI_Driven_Water_Supply.Infrastructure.Services.AuthService>();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();