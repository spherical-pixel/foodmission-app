using UnityEngine.Scripting;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    [Preserve]
    class SettingsScreen : NavigationScreenBase<SettingsViewModel>
    {
        private FormFieldItemArrowStepper _themeStepper;
        private FormFieldItemArrowStepper _langStepper;
        private FormFieldItemArrowStepper _scaleStepper;
        private FormFieldItemArrowStepper _fontStepper;
        private Label _userNameLabel;

        private static readonly string[] k_ThemeChoices = { "Light", "Dark", "System" };
        private static readonly string[] k_LangChoices  = { "English", "Español" };
        private static readonly string[] k_ScaleChoices = { "Small", "Medium", "Large" };
        private static readonly string[] k_FontChoices  = { "Roboto", "Open Sans", "OpenDyslexic" };

        public SettingsScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.SettingsTemplate);
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _themeStepper = contentContainer.Q<FormFieldItemArrowStepper>("stepper-theme");
            _langStepper  = contentContainer.Q<FormFieldItemArrowStepper>("stepper-lang");
            _scaleStepper = contentContainer.Q<FormFieldItemArrowStepper>("stepper-scale");
            _fontStepper  = contentContainer.Q<FormFieldItemArrowStepper>("stepper-font");
            _userNameLabel = contentContainer.Q<Label>("username");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            SetupSteppers();
            UpdateProfileHeader();
        }

        private void SetupSteppers()
        {
            if (_themeStepper != null)
            {
                _themeStepper.Choices = k_ThemeChoices;
                _themeStepper.SelectedIndex = ThemeLabelToIndex(_viewModel.Theme);
                _themeStepper.RegisterValueChangedCallback(OnThemeChanged);
            }

            if (_langStepper != null)
            {
                _langStepper.Choices = k_LangChoices;
                _langStepper.SelectedIndex = LangLabelToIndex(_viewModel.Lang);
                _langStepper.RegisterValueChangedCallback(OnLangChanged);
            }

            if (_scaleStepper != null)
            {
                _scaleStepper.Choices = k_ScaleChoices;
                _scaleStepper.SelectedIndex = ScaleLabelToIndex(_viewModel.Scale);
                _scaleStepper.RegisterValueChangedCallback(OnScaleChanged);
            }

            if (_fontStepper != null)
            {
                _fontStepper.Choices = k_FontChoices;
                _fontStepper.SelectedIndex = FontLabelToIndex(_viewModel.Font);
                _fontStepper.RegisterValueChangedCallback(OnFontChanged);
            }
        }

        private void UpdateProfileHeader()
        {
            if (_userNameLabel != null)
            {
                _userNameLabel.text = _viewModel?.UserName ?? "User";
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_themeStepper != null)
                _themeStepper.UnregisterValueChangedCallback(OnThemeChanged);
            if (_langStepper != null)
                _langStepper.UnregisterValueChangedCallback(OnLangChanged);
            if (_scaleStepper != null)
                _scaleStepper.UnregisterValueChangedCallback(OnScaleChanged);
            if (_fontStepper != null)
                _fontStepper.UnregisterValueChangedCallback(OnFontChanged);

            _themeStepper = null;
            _langStepper  = null;
            _scaleStepper = null;
            _fontStepper  = null;
            _userNameLabel = null;

            base.OnViewModelUnbinding();
        }

        private void OnThemeChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetTheme(IndexToTheme(evt.newValue));

        private void OnLangChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetLanguage(IndexToLang(evt.newValue));

        private void OnScaleChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetScale(IndexToScale(evt.newValue));

        private void OnFontChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetFont(IndexToFont(evt.newValue));

        private static int ThemeLabelToIndex(string theme) => theme switch
        {
            "light" => 0,
            "dark"  => 1,
            _       => 2  // system
        };

        private static int LangLabelToIndex(string lang) => lang switch
        {
            "en" => 0,
            _    => 1   // es
        };

        private static int ScaleLabelToIndex(string scale) => scale switch
        {
            "small" => 0,
            "large" => 2,
            _       => 1  // medium
        };

        private static int FontLabelToIndex(string font) => font switch
        {
            "open-sans"     => 1,
            "open-dyslexic" => 2,
            _               => 0  // roboto
        };

        private static string IndexToTheme(int index) => index switch
        {
            0 => "light",
            1 => "dark",
            _ => "system"
        };

        private static string IndexToLang(int index) => index switch
        {
            0 => "en",
            _ => "es"
        };

        private static string IndexToScale(int index) => index switch
        {
            0 => "small",
            2 => "large",
            _ => "medium"
        };

        private static string IndexToFont(int index) => index switch
        {
            1 => "open-sans",
            2 => "open-dyslexic",
            _ => "roboto"
        };
    }
}
