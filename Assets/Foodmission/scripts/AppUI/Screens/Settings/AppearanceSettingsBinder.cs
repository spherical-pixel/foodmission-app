using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Wires the appearance steppers (text size, theme, background, font, language) to a SettingsViewModel.
    /// Shared by SettingsScreen and AppearanceSettingsDialog (opened from Login / Register), so both stay in sync.
    /// Any stepper may be null.
    /// </summary>
    public sealed class AppearanceSettingsBinder
    {
        public static readonly string[] ThemeChoices = { "@UI:LIGHT", "@UI:DARK", "@UI:SYSTEM" };
        public static readonly string[] LangChoices = {
            "@UI:LANG_NL",
            "@UI:LANG_EN",
            "@UI:LANG_DE",
            "@UI:LANG_EL",
            "@UI:LANG_IT",
            "@UI:LANG_NO",
            "@UI:LANG_PL",
            "@UI:LANG_SL",
            "@UI:LANG_ES"
        };
        public static readonly string[] ScaleChoices = { "@UI:SCALE_SM", "@UI:SCALE_MD", "@UI:SCALE_LG" };
        public static readonly string[] FontChoices = { "Roboto", "Open Sans", "OpenDyslexic" };
        public static readonly string[] BackgroundChoices = { "@UI:PLAIN", "@UI:PATTERN" };

        private static readonly string[] k_LangCodes = { "nl", "en", "de", "el", "it", "no", "pl", "sl", "es" };

        private SettingsViewModel _viewModel;
        private readonly FormFieldItemArrowStepperSettings _scaleStepper;
        private readonly FormFieldItemArrowStepperSettings _themeStepper;
        private readonly FormFieldItemArrowStepperSettings _backgroundStepper;
        private readonly FormFieldItemArrowStepperSettings _fontStepper;
        private readonly FormFieldItemArrowStepperSettings _langStepper;

        public AppearanceSettingsBinder(
            FormFieldItemArrowStepperSettings scaleStepper,
            FormFieldItemArrowStepperSettings themeStepper,
            FormFieldItemArrowStepperSettings backgroundStepper,
            FormFieldItemArrowStepperSettings fontStepper,
            FormFieldItemArrowStepperSettings langStepper)
        {
            _scaleStepper = scaleStepper;
            _themeStepper = themeStepper;
            _backgroundStepper = backgroundStepper;
            _fontStepper = fontStepper;
            _langStepper = langStepper;
        }

        /// <summary>Sets choices and current values from the ViewModel, then listens for changes.</summary>
        public void Bind(SettingsViewModel viewModel)
        {
            if (_viewModel != null || viewModel == null)
            {
                return;
            }
            _viewModel = viewModel;

            if (_scaleStepper != null)
            {
                _scaleStepper.Choices = ScaleChoices;
                _scaleStepper.SelectedIndex = ScaleToIndex(_viewModel.Scale);
                _scaleStepper.RegisterValueChangedCallback(OnScaleChanged);
            }

            if (_themeStepper != null)
            {
                _themeStepper.Choices = ThemeChoices;
                _themeStepper.SelectedIndex = ThemeToIndex(_viewModel.Theme);
                _themeStepper.RegisterValueChangedCallback(OnThemeChanged);
            }

            if (_backgroundStepper != null)
            {
                _backgroundStepper.Choices = BackgroundChoices;
                _backgroundStepper.SelectedIndex = _viewModel.BackgroundPattern ? 1 : 0;
                _backgroundStepper.RegisterValueChangedCallback(OnBackgroundChanged);
            }

            if (_fontStepper != null)
            {
                _fontStepper.Choices = FontChoices;
                _fontStepper.SelectedIndex = FontToIndex(_viewModel.Font);
                _fontStepper.RegisterValueChangedCallback(OnFontChanged);
            }

            if (_langStepper != null)
            {
                _langStepper.Choices = LangChoices;
                _langStepper.SelectedIndex = LangToIndex(_viewModel.Lang);
                _langStepper.RegisterValueChangedCallback(OnLangChanged);
            }
        }

        public void Unbind()
        {
            if (_viewModel == null)
            {
                return;
            }
            _viewModel = null;

            _scaleStepper?.UnregisterValueChangedCallback(OnScaleChanged);
            _themeStepper?.UnregisterValueChangedCallback(OnThemeChanged);
            _backgroundStepper?.UnregisterValueChangedCallback(OnBackgroundChanged);
            _fontStepper?.UnregisterValueChangedCallback(OnFontChanged);
            _langStepper?.UnregisterValueChangedCallback(OnLangChanged);
        }

        public void CreateAccessibilityNodes(AccessibilityHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                return;
            }
            _scaleStepper?.CreateAccessibilityNode(hierarchy, "Scale");
            _themeStepper?.CreateAccessibilityNode(hierarchy, "Theme");
            _backgroundStepper?.CreateAccessibilityNode(hierarchy, "Background");
            _fontStepper?.CreateAccessibilityNode(hierarchy, "Font");
            _langStepper?.CreateAccessibilityNode(hierarchy, "Language");
        }

        public void DestroyAccessibilityNodes()
        {
            _scaleStepper?.DestroyAccessibilityNode();
            _themeStepper?.DestroyAccessibilityNode();
            _backgroundStepper?.DestroyAccessibilityNode();
            _fontStepper?.DestroyAccessibilityNode();
            _langStepper?.DestroyAccessibilityNode();
        }

        private void OnScaleChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetScale(IndexToScale(evt.newValue));

        private void OnThemeChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetTheme(IndexToTheme(evt.newValue));

        private void OnBackgroundChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetBackgroundPattern(evt.newValue == 1);

        private void OnFontChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetFont(IndexToFont(evt.newValue));

        private void OnLangChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetLanguage(IndexToLang(evt.newValue));

        public static int ThemeToIndex(string theme) => theme switch
        {
            "light" => 0,
            "dark" => 1,
            _ => 2  // system
        };

        public static string IndexToTheme(int index) => index switch
        {
            0 => "light",
            1 => "dark",
            _ => "system"
        };

        public static int ScaleToIndex(string scale) => scale switch
        {
            "small" => 0,
            "large" => 2,
            _ => 1  // medium
        };

        public static string IndexToScale(int index) => index switch
        {
            0 => "small",
            2 => "large",
            _ => "medium"
        };

        public static int FontToIndex(string font) => font switch
        {
            "open-sans" => 1,
            "open-dyslexic" => 2,
            _ => 0  // roboto
        };

        public static string IndexToFont(int index) => index switch
        {
            1 => "open-sans",
            2 => "open-dyslexic",
            _ => "roboto"
        };

        public static int LangToIndex(string lang)
        {
            int index = System.Array.IndexOf(k_LangCodes, lang);
            return index >= 0 ? index : 1;  // en as default
        }

        public static string IndexToLang(int index)
        {
            if (index >= 0 && index < k_LangCodes.Length)
            {
                return k_LangCodes[index];
            }
            return "es";
        }
    }
}
