using System.Linq;
using Unity.AppUI.MVVM;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    [Preserve]
    class SettingsScreen : NavigationScreenBase<SettingsViewModel>
    {
        private FormFieldItemArrowStepperSettings _themeStepper;
        private FormFieldItemArrowStepperSettings _langStepper;
        private FormFieldItemArrowStepperSettings _scaleStepper;
        private FormFieldItemArrowStepperSettings _fontStepper;
        private FormFieldItemArrowStepperSettings _soundStepper;
        private FormFieldItemArrowStepperSettings _musicStepper;
        private FormFieldItemArrowStepperSettings _notificationsStepper;
        private FormFieldItemArrowStepperSettings _backgroundStepper;
        

        private static readonly string[] k_ThemeChoices = { "@UI:LIGHT", "@UI:DARK", "@UI:SYSTEM" };
        private static readonly string[] k_LangChoices = {
            "@UI:LANG_NL",   // nl Nederlands
            "@UI:LANG_EN",      // en
            "@UI:LANG_DE",      // de
            "@UI:LANG_EL",   // el
            "@UI:LANG_IT",     // it
            "@UI:LANG_NO",        // no
            "@UI:LANG_PL",       // pl
            "@UI:LANG_SL",  // sl
            "@UI:LANG_ES"  // es
        };
        private static readonly string[] k_ScaleChoices = { "@UI:SCALE_SM", "@UI:SCALE_MD", "@UI:SCALE_LG" };
        private static readonly string[] k_FontChoices  = { "Roboto", "Open Sans", "OpenDyslexic" };
        private static readonly string[] k_SoundChoices         = Enumerable.Range(0, 21).Select(i => (i * 5).ToString()).ToArray();
        private static readonly string[] k_NotificationsChoices = { "@UI:OFF", "@UI:ON" };
        private static readonly string[] k_BackgroundChoices    = { "@UI:PLAIN", "@UI:PATTERN" };

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public SettingsScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Settings));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _themeStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-theme");
            _langStepper  = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-lang");
            _scaleStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-scale");
            _fontStepper  = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-font");
            _soundStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-sound");
            _musicStepper         = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-music");
            _notificationsStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-notifications");
            _backgroundStepper    = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-background");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            SetupSteppers();
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

            if (_soundStepper != null)
            {
                _soundStepper.Choices = k_SoundChoices;
                _soundStepper.SelectedIndex = SoundValueToIndex(_viewModel.Sound);
                _soundStepper.RegisterValueChangedCallback(OnSoundChanged);
            }

            if (_musicStepper != null)
            {
                _musicStepper.Choices = k_SoundChoices;
                _musicStepper.SelectedIndex = SoundValueToIndex(_viewModel.Music);
                _musicStepper.RegisterValueChangedCallback(OnMusicChanged);
            }

            if (_notificationsStepper != null)
            {
                _notificationsStepper.Choices = k_NotificationsChoices;
                _notificationsStepper.SelectedIndex = _viewModel.PushNotifications ? 1 : 0;
                _notificationsStepper.RegisterValueChangedCallback(OnNotificationsChanged);
            }

            if (_backgroundStepper != null)
            {
                _backgroundStepper.Choices = k_BackgroundChoices;
                _backgroundStepper.SelectedIndex = _viewModel.BackgroundPattern ? 1 : 0;
                _backgroundStepper.RegisterValueChangedCallback(OnBackgroundChanged);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_themeStepper != null)
            {
                _themeStepper.UnregisterValueChangedCallback(OnThemeChanged);
            }

            if (_langStepper != null)
            {
                _langStepper.UnregisterValueChangedCallback(OnLangChanged);
            }

            if (_scaleStepper != null)
            {
                _scaleStepper.UnregisterValueChangedCallback(OnScaleChanged);
            }

            if (_fontStepper != null)
            {
                _fontStepper.UnregisterValueChangedCallback(OnFontChanged);
            }

            if (_soundStepper != null)
            {
                _soundStepper.UnregisterValueChangedCallback(OnSoundChanged);
            }

            if (_musicStepper != null)
            {
                _musicStepper.UnregisterValueChangedCallback(OnMusicChanged);
            }

            if (_notificationsStepper != null)
            {
                _notificationsStepper.UnregisterValueChangedCallback(OnNotificationsChanged);
            }

            if (_backgroundStepper != null)
            {
                _backgroundStepper.UnregisterValueChangedCallback(OnBackgroundChanged);
            }

            _themeStepper = null;
            _langStepper  = null;
            _scaleStepper = null;
            _fontStepper  = null;
            _soundStepper         = null;
            _musicStepper         = null;
            _notificationsStepper = null;
            _backgroundStepper    = null;

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

        private void OnSoundChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetSound(IndexToSoundValue(evt.newValue));

        private void OnMusicChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetMusic(IndexToSoundValue(evt.newValue));

        private void OnNotificationsChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetPushNotifications(evt.newValue == 1);

        private void OnBackgroundChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetBackgroundPattern(evt.newValue == 1);

        private static int ThemeLabelToIndex(string theme) => theme switch
        {
            "light" => 0,
            "dark"  => 1,
            _       => 2  // system
        };

        private static int LangLabelToIndex(string lang) => lang switch
        {
            "nl" => 0,
            "en" => 1,
            "de" => 2,
            "el" => 3,
            "it" => 4,
            "no" => 5,
            "pl" => 6,
            "sl" => 7,
            "es" => 8,
            _    => 1  // en as default
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
            0 => "nl",
            1 => "en",
            2 => "de",
            3 => "el",
            4 => "it",
            5 => "no",
            6 => "pl",
            7 => "sl",
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

        private static int SoundValueToIndex(int volume) =>
            System.Math.Clamp(volume / 5, 0, 20);

        private static int IndexToSoundValue(int index) =>
            index * 5;
    }
}
