
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Foodmission app's main class
    /// Manages the inicialization of services (navigation, themes, scale)
    /// </summary>
    public class FoodmissionApp : App
    {
        public new static FoodmissionApp current => (FoodmissionApp)App.current;
        
        private IThemeService _themeService;
        private IStoreService _storeService;
        private IDisposableSubscription _scaleSubscription;
        private Panel _panel;

        public FoodmissionApp()
        {
            Debug.Log($"[{GetType().Name}] FoodmissionApp");
        }

        public override void InitializeComponent()
        {
            Debug.Log($"[{GetType().Name}] InitializeComponent");
            base.InitializeComponent();


            _themeService = services.GetService<IThemeService>();
            _storeService = services.GetService<IStoreService>();

            // Create and add the NavHost for navigation
            var navHost = new NavHost();
            // Set the Navigation graph asset
            navHost.navController.SetGraph(FoodmissionAppBuilder.instance.GraphAsset);
            navHost.visualController = new FoodmissionVisualController();

            rootVisualElement.Add(navHost);
            navHost.StretchToParentSize();

            // rootVisualElement in AppUI is a Panel
            _panel = rootVisualElement as Panel;

            if (_panel != null)
            {
                // Panel available, initialize the theme and scale
                InitializeThemeAndScale();
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] rootVisualElement is null!");
            }

            // Register for cleaning when shutting down the app
            App.shuttingDown += OnShuttingDown;
        }

        /// <summary>
        /// Initializes the theme and scale once the panel is available.
        /// </summary>
        private void InitializeThemeAndScale()
        {
            Debug.Log($"[{GetType().Name}] InitializeThemeAndScale");
            if (_panel == null)
            {
                Debug.LogError($"[{GetType().Name}] Cannot initialize - panel is null");
                return;
            }

            // Initialize the theme service
            _themeService.Initialize(_panel);

            // Apply the initial scale from state
            ApplyScaleFromState();

            // Scale change subscription
            _scaleSubscription?.Dispose();
            _scaleSubscription = _storeService.store.Subscribe(
                state => state.Get<AppState>(StoreService.APP_SLICE).scale,
                OnScaleChanged
            );
        }

        /// <summary>
        /// Cleaning on app shutting down
        /// </summary>
        private void OnShuttingDown()
        {
            App.shuttingDown -= OnShuttingDown;

            // Dispose services and subscriptions
            _scaleSubscription?.Dispose();
            _scaleSubscription = null;

            _themeService?.Dispose();
            _themeService = null;

            _storeService = null;
            _panel = null;
        }

        /// <summary>
        /// Applies the scale from actual state
        /// </summary>
        private void ApplyScaleFromState()
        {
            if (_panel == null || _storeService == null)
                return;

            AppState appState = _storeService.GetAppState();
            _panel.scale = appState.scale;
        }

        /// <summary>
        /// Callback when scale changed in state
        /// </summary>
        private void OnScaleChanged(string scale)
        {
            if (_panel == null)
                return;

            _panel.scale = scale;
        }
    }
}
