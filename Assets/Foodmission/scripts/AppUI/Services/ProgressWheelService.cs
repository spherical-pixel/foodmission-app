using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class ProgressWheelService : IProgressWheelService
    {
        private readonly IStoreService _storeService;
        private readonly IGamificationService _gamificationService;
        private readonly IAuthService _authService;
        private readonly HashSet<string> _recoveryAttemptedUsers = new HashSet<string>();
        private Task _inFlight;
        private bool _isLoading;

        public ProgressWheelService(IStoreService storeService, IGamificationService gamificationService, IAuthService authService)
        {
            _storeService = storeService;
            _gamificationService = gamificationService;
            _authService = authService;
        }

        public bool IsLoading => _isLoading;
        public event Action LoadingChanged;

        public Task RefreshAsync()
        {
            if (_inFlight != null && !_inFlight.IsCompleted)
            {
                return _inFlight;
            }
            _inFlight = RefreshCoreAsync();
            return _inFlight;
        }

        private async Task RefreshCoreAsync()
        {
            AppState state = _storeService?.GetAppState();
            if (state == null || string.IsNullOrEmpty(state.accessToken) || _gamificationService == null)
            {
                return;
            }

            SetLoading(true);
            try
            {
                var (wheels, error) = await _gamificationService.GetProgressWheelsAsync();
                if (error != null)
                {
                    Debug.LogWarning($"[{nameof(ProgressWheelService)}] Could not load progress wheels: {error.statusCode} {error.message}");
                    return;
                }

                if ((wheels == null || wheels.Length == 0) && ShouldRecover(state))
                {
                    _recoveryAttemptedUsers.Add(state.userId ?? "");
                    var (result, submitError) = await _gamificationService.SubmitOnboardingSurveyAsync(state.userOnboardingSurvey);
                    if (submitError == null && result != null)
                    {
                        _storeService.store.Dispatch(AppActions.setUserSegment.Invoke(result.segment));
                        wheels = result.progressWheels;
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(ProgressWheelService)}] Survey re-submission failed: {submitError?.statusCode} {submitError?.message}");
                    }
                }

                _storeService.store.Dispatch(AppActions.setProgressWheels.Invoke(wheels ?? new ProgressWheel[0]));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ProgressWheelService)}] RefreshAsync failed: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private bool ShouldRecover(AppState state)
        {
            return state.userOnboardingSurvey != null
                && state.userOnboardingSurvey.IsComplete()
                && !_recoveryAttemptedUsers.Contains(state.userId ?? "");
        }

        public async Task SetHiddenAsync(IReadOnlyCollection<string> hiddenKinds)
        {
            string[] hidden = hiddenKinds == null
                ? new string[0]
                : hiddenKinds.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToArray();
            _storeService.store.Dispatch(AppActions.setHiddenProgressWheels.Invoke(hidden));

            if (_authService != null)
            {
                await _authService.SyncSettingsAsync();
            }
        }

        public static IReadOnlyList<ProgressWheel> GetVisible(ProgressWheel[] wheels, string[] hidden)
        {
            if (wheels == null)
            {
                return Array.Empty<ProgressWheel>();
            }
            var hiddenSet = new HashSet<string>(hidden ?? new string[0]);
            return wheels.Where(w => w != null && !hiddenSet.Contains(w.kind)).ToList();
        }

        private void SetLoading(bool value)
        {
            if (_isLoading == value)
            {
                return;
            }
            _isLoading = value;
            LoadingChanged?.Invoke();
        }
    }
}
