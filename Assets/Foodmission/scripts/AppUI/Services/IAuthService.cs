using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IAuthService
    {
        event Action OnSessionExpired;
        Task<bool> CheckSessionAsync();
        Task<bool> RefreshAsync();
        Task<bool> HandleUnauthorizedAsync();
        Task<(bool success, string userId, string error)> LoginAsync(string username, string password);
        Task<(bool success, string userId, string error)> RegisterAsync(
            string username,
            string email,
            string password,
            int yearOfBirth = 0,
            string country = null,
            string region = null,
            string zip = null);
        void Logout();
        Task<(bool success, string message)> RequestPasswordResetAsync(string email);
        Task<(bool success, ApiErrorResponse error)> UpdateProfileAsync(ProfileUpdateRequest request);
        Task SyncSettingsAsync();
        Task<(bool success, string error)> DeleteAccountAsync();
    }
}

