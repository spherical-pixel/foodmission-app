using System;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class KnowledgeProgressService : IKnowledgeProgressService
    {
        private readonly IStoreService _storeService;
        private readonly IGamificationService _gamificationService;
        private Task _inFlight;
        private string _inFlightUserId;
        private int _activeRequests;

        public KnowledgeProgressService(IStoreService storeService, IGamificationService gamificationService)
        {
            _storeService = storeService;
            _gamificationService = gamificationService;
        }

        public bool IsLoading => _activeRequests > 0;
        public event Action LoadingChanged;

        public Task RefreshAsync()
        {
            string userId = _storeService?.GetAppState()?.userId ?? "";
            if (_inFlight != null && !_inFlight.IsCompleted && _inFlightUserId == userId)
            {
                return _inFlight;
            }
            _inFlightUserId = userId;
            _inFlight = RefreshCoreAsync();
            return _inFlight;
        }

        /// <summary>True when the session that started a request is still the active one (no logout / user switch meanwhile).</summary>
        private bool IsSameSession(string userId)
        {
            AppState current = _storeService?.GetAppState();
            return current != null && !string.IsNullOrEmpty(current.accessToken) && (current.userId ?? "") == userId;
        }

        private async Task RefreshCoreAsync()
        {
            AppState state = _storeService?.GetAppState();
            if (state == null || string.IsNullOrEmpty(state.accessToken) || _gamificationService == null)
            {
                return;
            }

            string userId = state.userId ?? "";
            SetLoading(true);
            try
            {
                var (bars, error) = await _gamificationService.GetKnowledgeProgressAsync();
                if (!IsSameSession(userId))
                {
                    return;
                }
                if (error != null)
                {
                    Debug.LogWarning($"[{nameof(KnowledgeProgressService)}] Could not load knowledge progress: {error.statusCode} {error.message}");
                    return;
                }

                // The list endpoint has no items; strip them anyway so the persisted cache stays small.
                KnowledgeProgress[] withoutItems = Array.ConvertAll(bars ?? new KnowledgeProgress[0], b =>
                {
                    KnowledgeProgress copy = b?.Copy();
                    if (copy != null)
                    {
                        copy.items = null;
                    }
                    return copy;
                });
                _storeService.store.Dispatch(AppActions.setKnowledgeProgress.Invoke(withoutItems));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(KnowledgeProgressService)}] RefreshAsync failed: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        // Counter: a refresh for a new user may overlap one still finishing for the previous user.
        private void SetLoading(bool value)
        {
            bool wasLoading = IsLoading;
            _activeRequests = Math.Max(0, _activeRequests + (value ? 1 : -1));
            if (wasLoading != IsLoading)
            {
                LoadingChanged?.Invoke();
            }
        }
    }
}
