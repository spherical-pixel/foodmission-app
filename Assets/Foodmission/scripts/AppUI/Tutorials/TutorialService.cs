using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    public sealed class TutorialService : ITutorialService
    {
        private static readonly TimeSpan k_DefaultFirstTimeDelay = TimeSpan.FromMilliseconds(600);

        private readonly IStoreService _store;
        private readonly IAuthService _auth;
        private readonly ITutorialPresenter _presenter;
        private readonly TimeSpan _firstTimeDelay;
        private bool _isShowing;

        // App UI's DI only picks constructors whose parameters are all registered services (no optional parameters)
        public TutorialService(IStoreService store, IAuthService auth, ITutorialPresenter presenter)
            : this(store, auth, presenter, k_DefaultFirstTimeDelay)
        {
        }

        public TutorialService(IStoreService store, IAuthService auth, ITutorialPresenter presenter, TimeSpan firstTimeDelay)
        {
            _store = store;
            _auth = auth;
            _presenter = presenter;
            _firstTimeDelay = firstTimeDelay;
        }

        public bool HasSeen(string id)
        {
            string[] seen = _store?.GetAppState()?.seenTutorials;
            return seen != null && Array.IndexOf(seen, id) >= 0;
        }

        public async Task ShowAsync(string id)
        {
            IReadOnlyList<string> keys = SectionTutorials.MessageKeys(id);
            if (keys.Count == 0 || _isShowing || _presenter == null)
            {
                return;
            }

            _isShowing = true;
            try
            {
                if (await _presenter.ShowAsync(keys))
                {
                    _store.store.Dispatch(AppActions.markTutorialSeen.Invoke(id));
                    if (_auth != null)
                    {
                        await _auth.SyncSettingsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TutorialService] Failed to show tutorial '{id}': {ex.Message}");
            }
            finally
            {
                _isShowing = false;
            }
        }

        public async Task ShowIfFirstTimeAsync(string destinationName, Func<string> currentDestination)
        {
            string id = SectionTutorials.IdForDestination(destinationName);
            if (id == null || id == SectionTutorials.Home || !IsLoggedIn() || HasSeen(id))
            {
                return;
            }

            if (_firstTimeDelay > TimeSpan.Zero)
            {
                await Task.Delay(_firstTimeDelay);
            }

            // The user may have moved on, or another dialog may have opened meanwhile: try again on the next visit
            if (currentDestination?.Invoke() != destinationName || !IsLoggedIn() || HasSeen(id) || _presenter == null || _presenter.IsAnyDialogOpen)
            {
                return;
            }

            await ShowAsync(id);
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(_store?.GetAppState()?.accessToken);
        }
    }
}
