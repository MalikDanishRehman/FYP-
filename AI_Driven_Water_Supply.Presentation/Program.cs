using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// --- STEP 1: CONFIGURATION ---
var supabaseUrl = "https://wejwiduabjiwztezvkdz.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndlandpZHVhYmppd3p0ZXp2a2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5MTQzMDgsImV4cCI6MjA4MDQ5MDMwOH0.nW7d_di1l2NTIMtSup4fCfrYJ9ZhW3U4uylAB6EOrvs";

Console.WriteLine($"✅ Supabase URL Loaded: {supabaseUrl}");

// --- STEP 2: REGISTER SUPABASE ---
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
    Schema = "public"
};

// 🛑 FIX 1: Blocking '.Wait()' hata diya. App fast start hogi.
builder.Services.AddScoped<Supabase.Client>(provider =>
{
    return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

// Auth Service Registration
builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IAuthService, AI_Driven_Water_Supply.Infrastructure.Services.AuthService>();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 🛑 FIX 2: HTTPS Redirection ko Development me band kar diya
// Kyunki localhost par kabhi kabhi SSL certificate ka masla hota hai aur app atak jati hai.
// app.UseHttpsRedirection(); // <--- ISE COMMENT HI REHNE DO ABHI KE LIYE

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();