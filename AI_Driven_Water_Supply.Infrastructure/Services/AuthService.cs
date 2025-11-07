using Supabase;
using AI_Driven_Water_Supply.Application.Interfaces;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly Client _client;

        public AuthService(Client client)
        {
            _client = client;
        }

        public async Task<bool> SignUp(string email, string password)
            => (await _client.Auth.SignUp(email, password)) != null;

        public async Task<bool> SignIn(string email, string password)
            => (await _client.Auth.SignIn(email, password)) != null;

        public async Task SignOut()
            => await _client.Auth.SignOut();
    }
}
