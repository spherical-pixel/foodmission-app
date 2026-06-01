using System;
using System.Collections.Generic;
using eu.foodmission.platform;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    public class TestStoreService : IStoreService, IDisposable
    {
        private AppState _state = new AppState();
        public IStore<AppState> store { get; private set; }
        public List<string> DispatchedActionTypes { get; } = new List<string>();

        public TestStoreService()
        {
            _state = new AppState();
            store = StoreFactory.CreateStore<AppState>(IdentityReducer, _state);
        }

        public AppState GetAppState() => _state.Copy();

        public void SetAppState(AppState state)
        {
            _state = state;
            store = StoreFactory.CreateStore<AppState>(IdentityReducer, _state);
        }

        public void SaveAppState() { }

        public void RestoreAppState() { }

        public void SetAppStateFromStorage() { }

        public void ClearSessionData()
        {
            _state = new AppState();
            DispatchedActionTypes.Clear();
            store = StoreFactory.CreateStore<AppState>(IdentityReducer, _state);
        }

        public void Dispose()
        {
            store?.Dispose();
            store = null;
        }

        private AppState IdentityReducer(AppState state, IAction action)
        {
            DispatchedActionTypes.Add(action.type);
            return state;
        }
    }
}
