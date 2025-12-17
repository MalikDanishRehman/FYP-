using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;
using Supabase;
using DotNetEnv; // 👈 .env file load karne ke liye zaroori hai

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. LOAD ENVIRONMENT VARIABLES (.env)
// =========================================================

// Ye line .env file ko dhoond kar load karegi
Env.Load();

// Variables ko fetch karo
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

// 🔒 SECURITY CHECK
// Agar keys load nahi huin to app crash kar jayegi taake aap fix kar sako
if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
{
    // Debugging ke liye path print karega taake pata chale system kahan dhoond raha hai
    var currentDir = Directory.GetCurrentDirectory();
    throw new Exception($"❌ Supabase Keys Missing! Make sure .env exists in '{currentDir}'");
}

// =========================================================
// 2. REGISTER SERVICES (DI Container)
// =========================================================

builder.Services.AddControllers();          // API Controllers ke liye
builder.Services.AddHttpContextAccessor();  // Cookies access karne ke liye
builder.Services.AddHttpClient();           // HTTP Requests ke liye

// --- Supabase Client Config ---
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
    Schema = "public"
};

// Supabase Client ko Scoped banaya (Har request par same instance rahega)
builder.Services.AddScoped<Supabase.Client>(provider =>
{
    return new Supabase.Client(supabaseUrl!, supabaseKey!, options);
});

// --- Custom Services ---
// Auth Service
builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IAuthService, AI_Driven_Water_Supply.Infrastructure.Services.AuthService>();

// Toast Service (Popup messages ke liye)
builder.Services.AddScoped<AI_Driven_Water_Supply.Presentation.Services.ToastService>();

// --- Blazor Components ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// =========================================================
// 3. HTTP REQUEST PIPELINE (Middleware)
// =========================================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // CSS, JS, Images load karne ke liye
app.UseRouting();     // Routing system activate karne ke liye

// ⚠️ Important: AuthController access karne ke liye yeh zaroori hai
app.MapControllers();

app.UseAntiforgery(); // Blazor forms security ke liye

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); // Interactive mode on

// App Start
app.Run();