using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Application.Models;
using Supabase.Gotrue;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SupabaseClient = Supabase.Client;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly SupabaseClient _client;
        private readonly IJSRuntime _js;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();

        public AuthService(SupabaseClient client, IJSRuntime js, IHttpContextAccessor httpContextAccessor)
        {
            _client = client;
            _js = js;
            _httpContextAccessor = httpContextAccessor;
        }

        public User? CurrentUser => _client.Auth.CurrentUser;

        // 1. SIGN IN
        public async Task<bool> SignIn(string email, string password)
        {
            Console.WriteLine($"🔑 Login Attempt for: {email}"); // Log 1

            try
            {
                var result = await _client.Auth.SignIn(email, password);

                if (result?.User != null && result.AccessToken != null)
                {
                    Console.WriteLine("✅ Supabase Login Success! Setting Cookie..."); // Log 2

                    try
                    {
                        // JavaScript Helper call
                        await _js.InvokeVoidAsync("cookieHelper.setCookie", result.AccessToken, result.RefreshToken);
                        Console.WriteLine("🍪 Cookie Set Successfully!"); // Log 3
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"❌ JS Error (Cookie Set Fail): {jsEx.Message}");
                        // Agar JS fail bhi ho jaye, to bhi true return karo taake login na ruke (Debugging ke liye)
                    }

                    NotifyStateChanged();
                    return true;
                }

                Console.WriteLine("❌ Supabase returned NULL User.");
                return false;
            }
            catch (Exception ex)
            {
                // Asli Error yahan dikhega
                Console.WriteLine($"🔥 CRITICAL LOGIN ERROR: {ex.Message}");
                return false;
            }
        }

        // 2. SIGN OUT
        public async Task SignOut()
        {
            await _client.Auth.SignOut();

            // ✅ Cookie delete karo
            await _js.InvokeVoidAsync("cookieHelper.removeCookie");

            NotifyStateChanged();
        }

        // 3. REFRESH SESSION (Restore from Cookie)
        public async Task TryRefreshSession()
        {
            try
            {
                if (_client.Auth.CurrentUser != null) return;

                // ✅ Secure Cookie Read
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    var token = context.Request.Cookies["supabase_token"];
                    var refresh = context.Request.Cookies["supabase_refresh"];

                    if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refresh))
                    {
                        await _client.Auth.SetSession(token, refresh);
                        NotifyStateChanged();
                    }
                }
            }
            catch
            {
                await SignOut();
            }
        }

        public async Task<bool> SignUp(string email, string password, string username)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return false;
            try
            {
                var options = new SignUpOptions
                {
                    Data = new Dictionary<string, object> { { "username", username ?? "User" } }
                };
                var result = await _client.Auth.SignUp(email, password, options);
                return result?.User != null;
            }
            catch { throw; }
        }

        public async Task<bool> UpdateUserRole(string role)
        {
            try
            {
                var user = _client.Auth.CurrentUser;
                if (user == null) return false;

                string username = "User";
                if (user.UserMetadata != null && user.UserMetadata.TryGetValue("username", out var nameObj))
                {
                    username = nameObj?.ToString() ?? "User";
                }

                var profile = new Profile
                {
                    Id = user.Id,
                    Role = role,
                    Username = username
                };

                await _client.From<Profile>().Upsert(profile);
                NotifyStateChanged();
                return true;
            }
            catch { return false; }
        }
    }
}