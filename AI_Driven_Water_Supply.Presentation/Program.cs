using AI_Driven_Water_Supply.Infrastructure.DependencyInjection;
using AI_Driven_Water_Supply.Presentation.Components;
using Supabase;
using DotNetEnv; 

var builder = WebApplication.CreateBuilder(args);

// Load .env from current directory, then from project root (e.g. when running from solution or from bin/)
Env.Load();
var projectRoot = Path.GetFullPath(Path.Combine(
    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
    "..", "..", ".."));
if (Directory.Exists(projectRoot))
{
    var envPath = Path.Combine(projectRoot, ".env");
    if (File.Exists(envPath))
        Env.Load(envPath);
}

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");


if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
{
    var currentDir = Directory.GetCurrentDirectory();
    throw new Exception($"❌ Supabase Keys Missing! Put SUPABASE_URL and SUPABASE_PUBLIC_KEY in a .env file in the project folder (e.g. '{projectRoot}') or in '{currentDir}'. The app cannot start until then.");
}



builder.Services.AddControllers();          
builder.Services.AddHttpContextAccessor();  
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("ExternalHttp", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8000/") });
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
    Schema = "public"
};


builder.Services.AddScoped<Supabase.Client>(provider =>
{
    return new Supabase.Client(supabaseUrl!, supabaseKey!, options);
});

builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IAuthService, AI_Driven_Water_Supply.Infrastructure.Services.AuthService>();
builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IToastService, AI_Driven_Water_Supply.Infrastructure.Services.ToastService>();
builder.Services.AddScoped<AI_Driven_Water_Supply.Application.Interfaces.IOrderStatusService, AI_Driven_Water_Supply.Infrastructure.Services.OrderStatusService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); 
app.UseRouting();     

app.MapControllers();

app.UseAntiforgery(); 

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); 

app.Run();