using AI_Driven_Water_Supply.Application.Interfaces;
using Supabase;
using Supabase.Gotrue;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Supabase.Gotrue.Constants;


// ✅ Aliases to remove ambiguity
using SupabaseClient = Supabase.Client;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly SupabaseClient _client;

        public AuthService(SupabaseClient client)
        {
            _client = client;
        }

        // Sign In
        public async Task<bool> SignIn(string email, string password)
        {
            var result = await _client.Auth.SignIn(email, password);
            return result?.User != null;
        }

        // Sign Up
        public async Task<bool> SignUp(string email, string password, string username)
        {
            var result = await _client.Auth.SignUp(
                SignUpType.Email,  // ✅ Explicit email signup
                email,
                password,
                new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                { "username", username }
                    }
                });

            return result?.User != null;
        }

                // Sign Out
        public async Task SignOut()
        {
            await _client.Auth.SignOut();
        }
    }
}
