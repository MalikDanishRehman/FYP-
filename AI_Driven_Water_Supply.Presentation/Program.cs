using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// --- STEP 1: DIRECT CONFIGURATION (No .env confusion) ---
// Hum seedha values yahan daal rahe hain taake "Null" error khatam ho.
var supabaseUrl = "https://wejwiduabjiwztezvkdz.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndlandpZHVhYmppd3p0ZXp2a2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5MTQzMDgsImV4cCI6MjA4MDQ5MDMwOH0.nW7d_di1l2NTIMtSup4fCfrYJ9ZhW3U4uylAB6EOrvs";

// Debug Check (Console me print hoga)
Console.WriteLine($"✅ Supabase URL Loaded: {supabaseUrl}");

// --- STEP 2: REGISTER SUPABASE ---
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
};

// Scoped Service Register karo
builder.Services.AddScoped<Supabase.Client>(_ =>
    new Supabase.Client(supabaseUrl, supabaseKey, options));

// Tumhara existing Infrastructure method (Agar isme bhi Supabase logic hai to check karna padega)
// Filhal hum upar manual register kar chuke hain, isliye niche wali line shayad conflict kare.
// Agar 'AddInfrastructure' ke andar dubara Supabase add ho raha hai, to wahan se hata dena.
// Safe side ke liye, main ise abhi comment kar raha hu, aur Auth service manually add kar raha hu:

// builder.Services.AddInfrastructure(builder.Configuration); 

// 👇 Manually Register Services (Taake confusion na ho)
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