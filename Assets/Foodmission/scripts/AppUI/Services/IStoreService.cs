using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Interface for the Store service - enables testability and DI
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Reference to the Redux store
        /// </summary>
        IStore<PartitionedState> store { get; }

        /// <summary>
        /// Gets the global application state
        /// </summary>
        AppState GetAppState();

        /// <summary>
        /// Persists the app state to local storage
        /// </summary>
        void SaveAppState();

        /// <summary>
        /// Restores the app state from local storage
        /// </summary>
        void RestoreAppState();
    }
}