using System;
using System.Collections.Generic;
using eu.foodmission.platform;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    public class TestStoreService : IStoreService, IDisposable
    {
        public IStore<AppState> store { get; private set; }
        public List<string> DispatchedActionTypes { get; } = new List<string>();

        public TestStoreService()
        {
            store = BuildStore(new AppState());
        }

        public AppState GetAppState() => store.GetState();

        public void SetAppState(AppState state)
        {
            store?.Dispose();
            store = BuildStore(state);
        }

        public void SaveAppState() { }

        public void RestoreAppState() { }

        public void SetAppStateFromStorage() { }

        public void ClearSessionData()
        {
            DispatchedActionTypes.Clear();
            store?.Dispose();
            store = BuildStore(new AppState());
        }

        public void Dispose()
        {
            store?.Dispose();
            store = null;
        }

        private IStore<AppState> BuildStore(AppState initialState)
        {
            var reducerBuilder = new SliceReducerSwitchBuilder<AppState>("app");
            reducerBuilder
                .AddCase(AppActions.setOnboardingSurvey, AppReducers.SetOnboardingSurveyReducer)
                .AddCase(AppActions.setExtendedProfile, AppReducers.SetExtendedProfileReducer)
                .AddCase(AppActions.setUser, AppReducers.SetUserReducer);

            var realReducer = reducerBuilder.GetReducer();

            Reducer<AppState> trackingReducer = (state, action) =>
            {
                DispatchedActionTypes.Add(action.type);
                return realReducer(state, action);
            };

            return StoreFactory.CreateStore(trackingReducer, initialState);
        }
    }
}
