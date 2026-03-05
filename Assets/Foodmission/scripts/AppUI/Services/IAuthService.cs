using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IAuthService
    {
        Task<bool> CheckSessionAsync();
        Task<(bool success, string userId, string error)> LoginAsync(string username, string password);
        Task<(bool success, string userId, string error)> RegisterAsync(string username, string email, string password);
        void Logout();
    }
}
