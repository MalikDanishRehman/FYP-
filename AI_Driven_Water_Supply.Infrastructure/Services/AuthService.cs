using Supabase;
using AI_Driven_Water_Supply.Application.Interfaces;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly Supabase.Client _client;

        // ✅ Clean constructor: only inject what is actually used
        public AuthService(Supabase.Client client)
        {
            _client = client;
        }

        // SignIn method
        public async Task<bool> SignIn(string email, string password)
        {
            var result = await _client.Auth.SignIn(email, password);
            return result?.User != null;
        }

        // SignUp method
        public async Task<bool> SignUp(string email, string password, string username)
        {
            var result = await _client.Auth.SignUp(email, password);
            return result?.User != null;
        }

        // Optional SignOut
        public async Task SignOut()
        {
            await _client.Auth.SignOut();
        }
    }
}
