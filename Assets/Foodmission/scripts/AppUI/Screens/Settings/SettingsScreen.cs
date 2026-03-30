using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class SettingsScreen : NavigationScreenBase<SettingsViewModel>
    {
        private DropdownField _themeDropdown;
        private DropdownField _langDropdown;
        private DropdownField _scaleDropdown;

        private static readonly List<string> k_ThemeChoices = new List<string> { "Light", "Dark", "System" };
        private static readonly List<string> k_LangChoices = new List<string> { "English", "Español" };
        private static readonly List<string> k_ScaleChoices = new List<string> { "Small", "Medium", "Large" };

        public SettingsScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.SettingsTemplate);
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _themeDropdown = contentContainer.Q<DropdownField>("dropdown-theme");
            _langDropdown = contentContainer.Q<DropdownField>("dropdown-lang");
            _scaleDropdown = contentContainer.Q<DropdownField>("dropdown-scale");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            SetupDropdowns();
        }

        private void SetupDropdowns()
        {
            if (_themeDropdown != null)
            {
                _themeDropdown.choices = k_ThemeChoices;
                _themeDropdown.SetValueWithoutNotify(ThemeToLabel(_viewModel.Theme));
                _themeDropdown.RegisterValueChangedCallback(OnThemeChanged);
            }

            if (_langDropdown != null)
            {
                _langDropdown.choices = k_LangChoices;
                _langDropdown.SetValueWithoutNotify(LangToLabel(_viewModel.Lang));
                _langDropdown.RegisterValueChangedCallback(OnLangChanged);
            }

            if (_scaleDropdown != null)
            {
                _scaleDropdown.choices = k_ScaleChoices;
                _scaleDropdown.SetValueWithoutNotify(ScaleToLabel(_viewModel.Scale));
                _scaleDropdown.RegisterValueChangedCallback(OnScaleChanged);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_themeDropdown != null)
            {
                _themeDropdown.UnregisterValueChangedCallback(OnThemeChanged);
            }

            if (_langDropdown != null)
            {
                _langDropdown.UnregisterValueChangedCallback(OnLangChanged);
            }

            if (_scaleDropdown != null)
            {
                _scaleDropdown.UnregisterValueChangedCallback(OnScaleChanged);
            }

            _themeDropdown = null;
            _langDropdown = null;
            _scaleDropdown = null;

            base.OnViewModelUnbinding();
        }

        private void OnThemeChanged(ChangeEvent<string> evt)
        {
            _viewModel?.SetTheme(LabelToTheme(evt.newValue));
        }

        private void OnLangChanged(ChangeEvent<string> evt)
        {
            _viewModel?.SetLanguage(LabelToLang(evt.newValue));
        }

        private void OnScaleChanged(ChangeEvent<string> evt)
        {
            _viewModel?.SetScale(LabelToScale(evt.newValue));
        }

        private static string ThemeToLabel(string theme) => theme switch
        {
            "light" => "Light",
            "dark" => "Dark",
            _ => "System"
        };

        private static string LangToLabel(string lang) => lang switch
        {
            "en" => "English",
            _ => "Español"
        };

        private static string ScaleToLabel(string scale) => scale switch
        {
            "small" => "Small",
            "large" => "Large",
            _ => "Medium"
        };

        private static string LabelToTheme(string label) => label switch
        {
            "Light" => "light",
            "Dark" => "dark",
            _ => "system"
        };

        private static string LabelToLang(string label) => label switch
        {
            "English" => "en",
            _ => "es"
        };

        private static string LabelToScale(string label) => label switch
        {
            "Small" => "small",
            "Large" => "large",
            _ => "medium"
        };
    }
}
