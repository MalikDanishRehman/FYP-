namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAuthService
    {
        Task<bool> SignUp(string email, string password ,string username);
        Task<bool> SignIn(string email, string password);
        Task SignOut();
    }
}
