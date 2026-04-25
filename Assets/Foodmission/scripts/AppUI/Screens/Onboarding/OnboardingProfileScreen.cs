using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.Core;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class OnboardingProfileScreen : NavigationScreenBase<OnboardingProfileViewModel>
    {
        private Unity.AppUI.UI.Button _submitButton;
        private FormFieldItemDropDownField _genderDropdown;
        private FormFieldItemDropDownField _activityLevelDropdown;
        private FormFieldItemDropDownField _educationLevelDropdown;
        private FormFieldItemDropDownField _annualIncomeDropdown;
        private FormFieldItemDropDownField _shoppingResponsibilityDropdown;
        private VisualElement _dietaryPreferencesContainer;
        private readonly List<FormFieldItemCheckbox> _dietaryCheckboxes = new List<FormFieldItemCheckbox>();

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public OnboardingProfileScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.OnboardingProfile));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _submitButton = contentContainer.Q<Unity.AppUI.UI.Button>("submit-button");
            _genderDropdown = contentContainer.Q<FormFieldItemDropDownField>("gender-dropdown");
            _activityLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("activity-level-dropdown");
            _educationLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("education-level-dropdown");
            _annualIncomeDropdown = contentContainer.Q<FormFieldItemDropDownField>("annual-income-dropdown");
            _shoppingResponsibilityDropdown = contentContainer.Q<FormFieldItemDropDownField>("shopping-responsibility-dropdown");
            _dietaryPreferencesContainer = contentContainer.Q<VisualElement>("dietary-preferences-container");
        }

        private void RegisterManualEvents()
        {
            if (_submitButton != null)
            {
                _submitButton.clicked += OnSubmitClicked;
            }

            if (_genderDropdown != null)
            {
                _genderDropdown.Dropdown.RegisterValueChangedCallback(OnGenderChanged);
            }

            if (_activityLevelDropdown != null)
            {
                _activityLevelDropdown.Dropdown.RegisterValueChangedCallback(OnActivityLevelChanged);
            }

            if (_educationLevelDropdown != null)
            {
                _educationLevelDropdown.Dropdown.RegisterValueChangedCallback(OnEducationLevelChanged);
            }

            if (_annualIncomeDropdown != null)
            {
                _annualIncomeDropdown.Dropdown.RegisterValueChangedCallback(OnAnnualIncomeChanged);
            }

            if (_shoppingResponsibilityDropdown != null)
            {
                _shoppingResponsibilityDropdown.Dropdown.RegisterValueChangedCallback(OnShoppingResponsibilityChanged);
            }
        }

        private void UnregisterManualEvents()
        {
            if (_submitButton != null)
            {
                _submitButton.clicked -= OnSubmitClicked;
            }

            if (_genderDropdown != null)
            {
                _genderDropdown.Dropdown.UnregisterValueChangedCallback(OnGenderChanged);
            }

            if (_activityLevelDropdown != null)
            {
                _activityLevelDropdown.Dropdown.UnregisterValueChangedCallback(OnActivityLevelChanged);
            }

            if (_educationLevelDropdown != null)
            {
                _educationLevelDropdown.Dropdown.UnregisterValueChangedCallback(OnEducationLevelChanged);
            }

            if (_annualIncomeDropdown != null)
            {
                _annualIncomeDropdown.Dropdown.UnregisterValueChangedCallback(OnAnnualIncomeChanged);
            }

            if (_shoppingResponsibilityDropdown != null)
            {
                _shoppingResponsibilityDropdown.Dropdown.UnregisterValueChangedCallback(OnShoppingResponsibilityChanged);
            }
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null)
            {
                await _viewModel.LoadCatalogDataAsync();
                PopulateDropdowns();
                PopulateDietaryCheckboxes();
                UpdateSubmitButtonState();
            }
        }

        private void PopulateDropdowns()
        {
            if (_viewModel == null) return;

            ConfigureDropdown(_genderDropdown, _viewModel.GenderOptions);
            ConfigureDropdown(_activityLevelDropdown, _viewModel.ActivityLevelOptions);
            ConfigureDropdown(_educationLevelDropdown, _viewModel.EducationLevelOptions);
            ConfigureDropdown(_annualIncomeDropdown, _viewModel.AnnualIncomeOptions);
            ConfigureDropdown(_shoppingResponsibilityDropdown, _viewModel.ShoppingResponsibilityOptions);
        }

        private void ConfigureDropdown(FormFieldItemDropDownField dropdown, IList<string> options)
        {
            if (dropdown == null || options == null || options.Count == 0) return;

            dropdown.Dropdown.sourceItems = (System.Collections.IList)options;
            dropdown.Dropdown.bindItem = (item, index) =>
            {
                item.label = options[index];
                item.icon = null;
            };
        }

        private void PopulateDietaryCheckboxes()
        {
            if (_viewModel == null || _dietaryPreferencesContainer == null) return;

            // Clear any existing checkboxes
            foreach (var checkbox in _dietaryCheckboxes)
            {
                _dietaryPreferencesContainer.Remove(checkbox);
            }
            _dietaryCheckboxes.Clear();

            var options = _viewModel.DietaryPreferenceOptions;
            if (options == null) return;

            for (int i = 0; i < options.Count; i++)
            {
                var checkbox = new FormFieldItemCheckbox();
                checkbox.Text = options[i];
                checkbox.CheckboxValue = CheckboxState.Unchecked;

                int index = i; // capture for closure
                checkbox.Button.clicked += () =>
                {
                    _viewModel.SetDietaryPreference(index, checkbox.CheckboxValue == CheckboxState.Checked);
                };

                _dietaryPreferencesContainer.Add(checkbox);
                _dietaryCheckboxes.Add(checkbox);
            }
        }

        private void UpdateSubmitButtonState()
        {
            if (_submitButton != null && _viewModel != null)
            {
                _submitButton.SetEnabled(/*_viewModel.IsFormValid && */!_viewModel.IsSubmitting);
            }
        }

        private void OnGenderChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedGenderIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnActivityLevelChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedActivityLevelIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnEducationLevelChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedEducationLevelIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnAnnualIncomeChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedAnnualIncomeIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnShoppingResponsibilityChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedShoppingResponsibilityIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private async void OnSubmitClicked()
        {
            if (_viewModel != null)
            {
                await _viewModel.SubmitAsync();
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
                _viewModel.ShowErrorRequest += OnShowErrorRequested;
                _viewModel.NavigationRequested += OnNavigationRequested;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
                _viewModel.NavigationRequested -= OnNavigationRequested;
            }

            UnregisterManualEvents();
            base.OnViewModelUnbinding();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OnboardingProfileViewModel.IsSubmitting) ||
                e.PropertyName == nameof(OnboardingProfileViewModel.IsFormValid))
            {
                UpdateSubmitButtonState();
            }
        }

        private void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Toast.Build(this, message, NotificationDuration.Long)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }

        
    }
}