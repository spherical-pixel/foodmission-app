
using System.Collections.Generic;
using System.Linq;
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
        private IAudioService _audioService;
        private FoodmissionVisualController _visualController;
        private IDisposableSubscription _scaleSubscription;
        private IDisposableSubscription _langSubscription;
        private IDisposableSubscription _backgroundSubscription;
        private Panel _panel;

#if UNITY_EDITOR
        private UIDocument _editorUidoc;
        private float _lastEditorDpi;
#endif

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
            _audioService = services.GetService<IAudioService>();

            if (_audioService != null)
            {
                var mixer = FoodmissionAppBuilder.instance != null ? FoodmissionAppBuilder.instance.audioMixer : null;
                if (mixer == null)
                {
                    mixer = Resources.Load<UnityEngine.Audio.AudioMixer>("AudioMixer");
                }
                var catalog = FoodmissionAppBuilder.instance != null ? FoodmissionAppBuilder.instance.audioCatalog : null;
                if (catalog == null)
                {
                    catalog = Resources.Load<AudioCatalogSO>("AudioCatalog");
                }
                _audioService.Initialize(mixer, catalog);
            }

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

            // Setup notification routing and in-app message listener
            var routingService = services.GetService<NotificationRoutingService>();
            if (routingService != null)
            {
                routingService.SetNavigationHandler((action, args) =>
                {
                    navHost.navController.Navigate(action, args);
                });
                routingService.SetNotificationsDrawerHandler(() =>
                {
                    _visualController?.OpenNotificationsPanel();
                });
            }

            var notificationService = services.GetService<INotificationService>();
            if (notificationService != null)
            {
                notificationService.OnNotificationReceived += model =>
                {
                    _visualController?.AddNotification(model);
                };
            }

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

            _panel.RegisterCallback<ClickEvent>(OnGlobalClick, TrickleDown.TrickleDown);


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

#if UNITY_EDITOR
            ApplyEditorDpiCorrection();
            _panel.RegisterCallback<GeometryChangedEvent>(_ => ApplyEditorDpiCorrection());
#endif

            // Initialize the theme service
            _themeService.Initialize(_panel);

            // Initialize accessibility service and apply system settings
            _accessibilityService = services.GetRequiredService<IAccessibilityService>();
            ApplyBoldTextFromSystem();
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

            // Sync Editor Localization Simulator changes to Redux state
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

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
                _accessibilityService.BoldTextStatusChanged -= OnSystemBoldTextChanged;
            }

            _themeService?.Dispose();
            _themeService = null;

            _audioService?.Dispose();
            _audioService = null;

            _panel?.UnregisterCallback<ClickEvent>(OnGlobalClick, TrickleDown.TrickleDown);

            _storeService = null;
            _visualController = null;
            _panel = null;
        }

#if UNITY_EDITOR
        private void ApplyEditorDpiCorrection()
        {
            if (_panel == null) return;

            float dpi = Screen.dpi;
            if (dpi <= 0f) return;

            const float referenceDpi = 264f;
            if (Mathf.Approximately(dpi, _lastEditorDpi)) return;

            if (_editorUidoc == null)
            {
                _editorUidoc = Object.FindObjectOfType<UIDocument>();
                if (_editorUidoc == null || _editorUidoc.panelSettings == null) return;
            }

            var settings = Object.Instantiate(_editorUidoc.panelSettings);
            settings.referenceDpi = dpi;
            _editorUidoc.panelSettings = settings;

            _lastEditorDpi = dpi;

            Debug.Log($"[{GetType().Name}] Editor DPI: {referenceDpi} \u2192 {dpi}");
        }
#endif

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

            if (currentLang == "none")
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

            if (currentLang != "none" && currentLang != string.Empty && currentLang != null)
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
            if (lang == "none")
            {
                lang = CheckLangCode(lang);
                if (_storeService.GetAppState().lang != lang)
                {
                    _storeService.store.Dispatch(AppActions.setLanguage.Invoke(lang));
                }
            }
            ApplyLocale(lang);
            _visualController?.RefreshLocalizedContent();
        }

        private static void ApplyLocale(string lang)
        {
            //lang = "en"; // TEMP: force English until translations are reviewed
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

        private void OnSelectedLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            if (locale == null || _storeService == null) return;
            string code = locale.Identifier.Code;
            if (!string.IsNullOrEmpty(code) && _storeService.GetAppState().lang != code)
            {
                Debug.Log($"[FoodmissionApp] Unity Localization locale changed to: {code}, updating Redux state");
                _storeService.store.Dispatch(AppActions.setLanguage.Invoke(code));
            }
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



        /// <summary>
        /// Global click event listener for ALL buttons in the application (FMButton, AppUI Button, BottomNavBar, Overlays, Modals, etc.).
        /// Uses ClickEvent during TrickleDown phase on containers so audio triggers for all UI components.
        /// </summary>
        private void OnGlobalClick(ClickEvent evt)
        {
            if (_audioService == null) return;


            var targetElement = evt.target as VisualElement;
            if (targetElement == null)
            {
                return;
            }

            VisualElement button = targetElement;

            // // Search up the visual tree for a Button or clickable component
            // while (button != null && !IsButtonElement(button))
            // {
            //     button = button.parent;
            // }




            // Skip if no button container was clicked or if the button is disabled
            if (button == null)
            {
                return;
            }


            if (!IsButtonElement(button))
            {
                return;
            }

            // Debug.LogWarning($"[FoodmissionApp] OnGlobalClick [ ISBUTTON ] - target: '{targetElement.name}' ({targetElement.GetType().Name})");

            // if (button.ClassListContains("unity-disabled"))
            // {
            //     Debug.LogWarning($"[FoodmissionApp] OnGlobalClick [ unity-disabled ] - target: '{targetElement.name}' ({targetElement.GetType().Name}");
            //     List<string> classes = button.GetClasses().ToList<string>();
            //     classes.ForEach(c => Debug.LogWarning($"[FoodmissionApp] OnGlobalClick [ unity-disabled ] - class: '{c}'"));
            //     return;
            // }

            // Debug.LogWarning($"[FoodmissionApp] OnGlobalClick [ !unity-disabled ] - target: '{targetElement.name}' ({targetElement.GetType().Name}");




            // Allow elements to opt out of button click SFX using class "no-sfx"
            if (button.ClassListContains("no-sfx"))
            {
                return;
            }


            // Determine if button is destructive
            bool isDestructive = false;
            if (button is Unity.AppUI.UI.Button appUiBtn)
            {
                isDestructive = appUiBtn.variant == ButtonVariant.Destructive;
            }
            else if (button.ClassListContains("fm-button-destructive") ||
                     button.ClassListContains("btn-destructive") ||
                     (!string.IsNullOrEmpty(button.name) && button.name.IndexOf("delete", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                isDestructive = true;
            }

            SfxType sfx = isDestructive ? SfxType.NegativeButton : SfxType.PositiveButton;
            _audioService.PlaySfx(sfx, 0.25f);

            TriggerHapticFeedback(isDestructive ? Unity.AppUI.Core.HapticFeedbackType.HEAVY : Unity.AppUI.Core.HapticFeedbackType.LIGHT);
        }

        private static void TriggerHapticFeedback(Unity.AppUI.Core.HapticFeedbackType type)
        {
            if (Unity.AppUI.Core.Platform.isHapticFeedbackSupported)
            {
                Unity.AppUI.Core.Platform.RunHapticFeedback(type);
            }
        }


        private static bool IsButtonElement(VisualElement ve)
        {
            if (ve == null) return false;

            if (ve is Unity.AppUI.UI.Button ||
                ve is UnityEngine.UIElements.Button ||
                ve is Unity.AppUI.UI.BottomNavBarItem ||
                ve is Unity.AppUI.UI.ActionButton ||
                ve is Components.FMButton)
            {
                return true;
            }

            if (ve.ClassListContains("appui-button") ||
                ve.ClassListContains("fm-button") ||
                ve.ClassListContains("appui-bottom-nav-bar__item") ||
                ve.ClassListContains("fm-click-sound"))
            {
                return true;
            }

            return false;
        }
    }
}
