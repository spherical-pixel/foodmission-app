
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
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
        private IAccessibilityService _accessibilityService;
        private FoodmissionVisualController _visualController;
        private IDisposableSubscription _scaleSubscription;
        private IDisposableSubscription _langSubscription;
        private IDisposableSubscription _backgroundSubscription;
        private Panel _panel;

        public FoodmissionApp()
        {
            Debug.Log($"[{GetType().Name}] FoodmissionApp");

#if UNITY_ANDROID || UNITY_IOS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
#endif
        }

        public override void InitializeComponent()
        {
            Debug.Log($"[{GetType().Name}] InitializeComponent");
            base.InitializeComponent();


            _themeService = services.GetService<IThemeService>();
            _storeService = services.GetService<IStoreService>();

            // Create and add the NavHost for navigation
            var navHost = new NavHost();
            navHost.navController.SetGraph(FoodmissionAppBuilder.instance.GraphAsset);
            _visualController = new FoodmissionVisualController();
            navHost.visualController = _visualController;
            

            rootVisualElement.Add(navHost);
            navHost.StretchToParentSize();

            // Add the menu drawer and notifications after the NavHost so they render on top
            _visualController.CreateMenuDrawer(rootVisualElement);
            _visualController.CreateNotificationsPanel(rootVisualElement, FoodmissionAppBuilder.instance.NotificationCardTemplate);

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

            // Initialize accessibility service and apply system settings
            _accessibilityService = services.GetRequiredService<IAccessibilityService>();
            ApplyFontScaleFromSystem();
            ApplyBoldTextFromSystem();
            _accessibilityService.FontScaleChanged += OnSystemFontScaleChanged;
            _accessibilityService.BoldTextStatusChanged += OnSystemBoldTextChanged;

            // Apply the initial scale from state
            ApplyScaleFromState();

            // Scale change subscription
            _scaleSubscription?.Dispose();
            _scaleSubscription = _storeService.store.Subscribe(
                state => state.scale,
                OnScaleChanged
            );

            // Apply the initial locale from state
            ApplyLocaleFromState();

            // Locale change subscription
            _langSubscription?.Dispose();
            _langSubscription = _storeService.store.Subscribe(
                state => state.lang,
                OnLangChanged
            );

            // Apply the initial background from state
            ApplyBackgroundFromState();

            // Background change subscription
            _backgroundSubscription?.Dispose();
            _backgroundSubscription = _storeService.store.Subscribe(
                state => state.backgroundPattern,
                OnBackgroundPatternChanged
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

            _langSubscription?.Dispose();
            _langSubscription = null;

            _backgroundSubscription?.Dispose();
            _backgroundSubscription = null;

            if (_accessibilityService != null)
            {
                _accessibilityService.FontScaleChanged -= OnSystemFontScaleChanged;
                _accessibilityService.BoldTextStatusChanged -= OnSystemBoldTextChanged;
            }

            _themeService?.Dispose();
            _themeService = null;

            _storeService = null;
            if (_visualController != null)
            {
                _visualController.Dispose();
                _visualController = null;
            }
            _panel = null;
        }

        private void ApplyBackgroundFromState()
        {
            if (_storeService == null)
                return;

            ApplyBackground(_storeService.GetAppState().backgroundPattern);
        }

        private void OnBackgroundPatternChanged(bool pattern)
        {
            ApplyBackground(pattern);
        }

        private void ApplyBackground(bool pattern)
        {
            if (pattern)
                rootVisualElement.RemoveFromClassList("fm-plain-background");
            else
                rootVisualElement.AddToClassList("fm-plain-background");
        }

        private void ApplyLocaleFromState()
        {
            if (_storeService == null)
            {
                return;
            }

            string currentLang = _storeService.GetAppState().lang;

            if( currentLang == "none")
            {
                currentLang = CheckLangCode(currentLang);
                Debug.Log($"[{GetType().Name}] - ApplyLocaleFromState - Applying locale from system: {currentLang}");
                _storeService.store.Dispatch(AppActions.setLanguage.Invoke(currentLang));
            }
            else
            {
                Debug.Log($"[{GetType().Name}] - ApplyLocaleFromState - Applying locale from state: {currentLang}");
                
            }


            ApplyLocale(currentLang);
            _visualController?.RefreshLocalizedContent();
        }

        private string CheckLangCode(string lang)
        {
            string currentLang = lang;
            
            if( currentLang != "none" && currentLang != string.Empty && currentLang != null)
            {
                foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    if (locale.Identifier.Code == currentLang)
                    {
                        return currentLang;
                    }
                }
            }

            
            switch (Application.systemLanguage)
            {
                case SystemLanguage.English:
                    currentLang = "en";
                    break;
                case SystemLanguage.Dutch:
                    currentLang = "nl";
                    break;
                case SystemLanguage.German:
                    currentLang = "de";
                    break;
                case SystemLanguage.Greek:
                    currentLang = "el";
                    break;
                case SystemLanguage.Italian:
                    currentLang = "it";
                    break;
                case SystemLanguage.Norwegian:
                    currentLang = "no";
                    break;
                case SystemLanguage.Polish:
                    currentLang = "pl";
                    break;
                case SystemLanguage.Slovenian:
                    currentLang = "sl";
                    break;
                case SystemLanguage.Spanish:
                    currentLang = "es";
                    break;
                default:
                    currentLang = "en";
                    break;
            }
                
            
            return currentLang;
        }

        private void OnLangChanged(string lang)
        {
            if( lang == "none")
            {
                lang = CheckLangCode(lang);
                if( _storeService.GetAppState().lang != lang)
                {
                    _storeService.store.Dispatch(AppActions.setLanguage.Invoke(lang));
                }
            }
            ApplyLocale(lang);
            _visualController?.RefreshLocalizedContent();
        }

        private static void ApplyLocale(string lang)
        {
            lang = "en"; // TEMP: force English until translations are reviewed
            Debug.Log($"[FoodmissionApp] - ApplyLocale -  Applying locale: {lang}");
            var locale = LocalizationSettings.AvailableLocales.GetLocale(lang);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
            else
            {
                Debug.LogWarning($"[FoodmissionApp] Locale not found for lang: {lang}");
            }
        }

        private void ApplyFontScaleFromSystem()
        {
            if (_panel == null || _accessibilityService == null) return;

            var systemScale = _accessibilityService.FontScale;
            if (Mathf.Approximately(systemScale, 1f)) return;

            float currentScale = float.TryParse(_panel.scale, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed) ? parsed : 1f;
            _panel.scale = (currentScale * systemScale).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void OnSystemFontScaleChanged(float scale)
        {
            if (_panel == null) return;
            var appState = _storeService?.GetAppState();
            var baseScale = appState != null ? appState.scale : "medium";

            float baseValue = baseScale switch
            {
                "small" => 0.85f,
                "large" => 1.15f,
                _ => 1.0f
            };

            _panel.scale = (baseValue * scale).ToString("F2");
        }

        private void ApplyBoldTextFromSystem()
        {
            if (_panel == null || _accessibilityService == null) return;

            if (_accessibilityService.IsBoldTextEnabled)
            {
                rootVisualElement.AddToClassList("fm-bold-text");
            }
            else
            {
                rootVisualElement.RemoveFromClassList("fm-bold-text");
            }
        }

        private void OnSystemBoldTextChanged(bool enabled)
        {
            if (_panel == null) return;

            if (enabled)
            {
                rootVisualElement.AddToClassList("fm-bold-text");
            }
            else
            {
                rootVisualElement.RemoveFromClassList("fm-bold-text");
            }
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
