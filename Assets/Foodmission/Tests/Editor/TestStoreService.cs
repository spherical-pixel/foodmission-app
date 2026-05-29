using System;
using eu.foodmission.platform;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    public class TestStoreService : IStoreService, IDisposable
    {
        public IStore<AppState> store { get; private set; }
        private AppState _state;

        public TestStoreService()
        {
            _state = new AppState();
            store = StoreFactory.CreateStore<AppState>(_state);
            AppReducers.Register(store);
            AuthReducers.Register(store);
        }

        public AppState GetAppState() => _state.Copy();

        public void SetAppStateFromStorage()
        {
        }

        public void ClearSessionData()
        {
            _state = new AppState();
            store = StoreFactory.CreateStore<AppState>(_state);
            AppReducers.Register(store);
            AuthReducers.Register(store);
        }

        public void Dispose()
        {
            store?.Dispose();
            store = null;
        }
    }
}
