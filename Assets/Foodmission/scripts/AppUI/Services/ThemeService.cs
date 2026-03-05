using System;
using Unity.AppUI.UI;
using Unity.AppUI.Redux;
using UnityEngine;
using Unity.AppUI.Core;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Visual theme and safe area management service.
    /// Implements IDisposable for proper cleanup.
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly IStoreService _storeService;
        private IDisposableSubscription _subscription;

        private Panel _panel;
        private bool _isInitialized;

        public string currentTheme { get; private set; } = "system";
        public float safeAreaTop { get; private set; }
        public float safeAreaRight { get; private set; }
        public float safeAreaBottom { get; private set; }
        public float safeAreaLeft { get; private set; }

        /// <summary>
        /// Event fired when the theme changes.
        /// </summary>
        public event System.Action<string> ThemeChanged;

        /// <summary>
        /// Event fired when the safe area changes.
        /// </summary>
        public event System.Action SafeAreaChanged;

        public ThemeService(IStoreService storeService)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
        }

        /// <summary>
        /// Initializes the theme service with the main panel.
        /// Should be called only once, preferably from FoodmissionApp.InitializeComponent.
        /// </summary>
        /// <param name="panel">The main UI Toolkit panel</param>
        public void Initialize(Panel panel)
        {
            if (_isInitialized)
            {
                return;
            }

            if (panel == null)
            {
                Debug.LogError("[ThemeService] Cannot initialize with null panel");
                return;
            }

            _panel = panel;
            _isInitialized = true;

            // Apply initial theme from state
            AppState appState = _storeService.GetAppState();
            SetTheme(appState.theme);

            // Subscribe to state changes
            _subscription = _storeService.store.Subscribe(
                state => state.Get<AppState>(StoreService.APP_SLICE),
                OnAppStateChanged
            );

            // Register callback for geometry changes (safe area)
            _panel.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);

            // Listen to system theme changes
            Platform.darkModeChanged += OnSystemThemeChanged;

            // Calculate initial safe area
            UpdateSafeArea();
        }

        /// <summary>
        /// Changes the application visual theme.
        /// </summary>
        /// <param name="theme">"light", "dark", or "system"</param>
        public void SetTheme(string theme)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot set theme to '{theme}' - not initialized yet");
                return;
            }

            currentTheme = theme;

            string effectiveTheme;
            if (theme == "system")
            {
                effectiveTheme = Platform.darkMode ? "dark" : "light";
            }
            else
            {
                effectiveTheme = theme;
            }

            _panel.theme = effectiveTheme;
            ThemeChanged?.Invoke(effectiveTheme);
        }

        /// <summary>
        /// Applies safe area padding to a visual element.
        /// </summary>
        public void ApplySafeAreaPadding(VisualElement element,bool applyTop,bool applyBottom,bool applyLeft, bool applyRight)
        {
            if (element == null || !_isInitialized)
                return;

            if( applyTop) element.style.paddingTop = safeAreaTop;
            if( applyRight) element.style.paddingRight = safeAreaRight;
            if( applyBottom) element.style.paddingBottom = safeAreaBottom;
            if( applyLeft) element.style.paddingLeft = safeAreaLeft;
        }

        /// <summary>
        /// Applies safe area margin to a visual element.
        /// </summary>
        public void ApplySafeAreaMargin(VisualElement element)
        {
            if (element == null || !_isInitialized)
                return;

            element.style.marginTop = safeAreaTop;
            element.style.marginRight = safeAreaRight;
            element.style.marginBottom = safeAreaBottom;
            element.style.marginLeft = safeAreaLeft;
        }

        private void OnAppStateChanged(AppState state)
        {
            if (state.theme != currentTheme)
            {
                SetTheme(state.theme);
            }
        }

        private void OnSystemThemeChanged(bool darkMode)
        {
            if (currentTheme == "system")
            {
                string effectiveTheme = darkMode ? "dark" : "light";
                _panel.theme = effectiveTheme;
                ThemeChanged?.Invoke(effectiveTheme);
            }
        }

        private void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            UpdateSafeArea();
        }

        private void UpdateSafeArea()
        {
            if (!_isInitialized || _panel == null)
                return;

            Rect safeArea = Screen.safeArea;
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            // Calculate DPI scale if panel has resolved size
            float dpiScale = _panel.resolvedStyle.width > 0
                ? _panel.resolvedStyle.width / screenWidth
                : 1f;

            safeAreaTop = (screenHeight - safeArea.yMax) * dpiScale;
            safeAreaRight = (screenWidth - safeArea.xMax) * dpiScale;
            safeAreaBottom = safeArea.y * dpiScale;
            safeAreaLeft = safeArea.x * dpiScale;

            SafeAreaChanged?.Invoke();
        }

        /// <summary>
        /// Releases all resources and subscriptions.
        /// </summary>
        public void Dispose()
        {
            Platform.darkModeChanged -= OnSystemThemeChanged;

            if (_panel != null)
            {
                _panel.UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
            }

            _subscription?.Dispose();
            _subscription = null;

            _panel = null;
            _isInitialized = false;
        }
    }
}
