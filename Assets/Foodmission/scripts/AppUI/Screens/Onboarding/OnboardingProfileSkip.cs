using System;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Records that the onboarding profile was skipped (backend timestamp + store), so Home reminds it later.</summary>
    public static class OnboardingProfileSkip
    {
        public static async Task MarkAsync(IAuthService authService, IStoreService storeService)
        {
            try
            {
                var request = new ProfileUpdateRequest
                {
                    preferences = new ProfileUpdatePreferences
                    {
                        onboardingProfileSkippedAt = DateTime.UtcNow.ToString("o")
                    }
                };

                if (authService != null)
                {
                    await authService.UpdateProfileAsync(request);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnboardingProfileSkip] MarkAsync profile update error: {ex.Message}");
            }
            finally
            {
                storeService?.store?.Dispatch(AppActions.setSkippedExtendedProfile.Invoke());
            }
        }
    }
}
