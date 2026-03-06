using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Base class for all ViewModels in the application.
    /// Provides automatic cleanup of Redux subscriptions and App.shuttingDown events.
    /// Implements INavigationAware for navigation support.
    /// Note: Derived classes should add [ObservableObject] attribute.
    /// </summary>
    public abstract class ViewModelBase : IDisposable, INavigationAware
    {
        // --------------------------------------------------------------------
        // Navigation
        // --------------------------------------------------------------------

        /// <summary>
        /// Event fired when the ViewModel requests navigation.
        /// Screens using NavigationScreenBase are automatically subscribed.
        /// </summary>
        public event System.Action<string> NavigationRequested;

        /// <summary>
        /// Invokes the NavigationRequested event. Derived classes should use this to request navigation.
        /// </summary>
        /// <param name="action">The navigation action to execute</param>
        protected void RaiseNavigationRequested(string action)
        {
            NavigationRequested?.Invoke(action);
        }

        // --------------------------------------------------------------------
        // Protected fields for derived classes
        // --------------------------------------------------------------------

        protected readonly IStoreService _storeService;
        protected readonly IStore<PartitionedState> _store;
        protected IDisposableSubscription _storeSubscription;

        // --------------------------------------------------------------------
        // Private state
        // --------------------------------------------------------------------

        private bool m_IsDisposed;

        // --------------------------------------------------------------------
        // Construction & Cleanup
        // --------------------------------------------------------------------

        protected ViewModelBase(IStoreService storeService)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
            _store = storeService.store;

            App.shuttingDown += OnAppShuttingDown;
        }

        public virtual void Dispose()
        {
            if (m_IsDisposed) return;

            m_IsDisposed = true;
            App.shuttingDown -= OnAppShuttingDown;

            _storeSubscription?.Dispose();
            _storeSubscription = null;

            OnDispose();
        }

        protected virtual void OnDispose() { }

        private void OnAppShuttingDown()
        {
            Dispose();
        }
    }
}
