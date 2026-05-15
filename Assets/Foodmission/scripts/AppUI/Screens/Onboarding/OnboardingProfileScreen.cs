using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Core;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class OnboardingProfileScreen : NavigationScreenBase<OnboardingProfileViewModel>
    {
        private Unity.AppUI.UI.Button _submitButton;
        private Unity.AppUI.UI.Button _skipButton;
        private FormFieldItemDropDownField _genderDropdown;
        private FormFieldItemDropDownField _activityLevelDropdown;
        private FormFieldItemDropDownField _dietaryPreferencesDropdown;
        private FormFieldItemDropDownField _educationLevelDropdown;
        private FormFieldItemDropDownField _annualIncomeDropdown;
        private FormFieldItemDropDownField _shoppingResponsibilityDropdown;

        private AccessibilityNode _submitButtonNode;
        private AccessibilityNode _skipButtonNode;

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
            _skipButton = contentContainer.Q<Unity.AppUI.UI.Button>("skip-button");
            _genderDropdown = contentContainer.Q<FormFieldItemDropDownField>("gender-dropdown");
            _activityLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("activity-level-dropdown");
            _educationLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("education-level-dropdown");
            _dietaryPreferencesDropdown = contentContainer.Q<FormFieldItemDropDownField>("dietary-preferences-dropdown");
            _annualIncomeDropdown = contentContainer.Q<FormFieldItemDropDownField>("annual-income-dropdown");
            _shoppingResponsibilityDropdown = contentContainer.Q<FormFieldItemDropDownField>("shopping-responsibility-dropdown");
            
        }

        private void RegisterManualEvents()
        {
            if (_submitButton != null)
            {
                _submitButton.clicked += OnSubmitClicked;
            }

            if (_skipButton != null)
            {
                _skipButton.clicked += OnSkipClicked;
            }

            if (_genderDropdown != null)
            {
                _genderDropdown.Dropdown.RegisterValueChangedCallback(OnGenderChanged);
            }

            if (_activityLevelDropdown != null)
            {
                _activityLevelDropdown.Dropdown.RegisterValueChangedCallback(OnActivityLevelChanged);
            }

            if( _dietaryPreferencesDropdown != null)
            {
                _dietaryPreferencesDropdown.Dropdown.RegisterValueChangedCallback(OnDietaryPreferencesChanged);
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

            if (_skipButton != null)
            {
                _skipButton.clicked -= OnSkipClicked;
            }

            if (_genderDropdown != null)
            {
                _genderDropdown.Dropdown.UnregisterValueChangedCallback(OnGenderChanged);
            }

            if (_activityLevelDropdown != null)
            {
                _activityLevelDropdown.Dropdown.UnregisterValueChangedCallback(OnActivityLevelChanged);
            }

            if (_dietaryPreferencesDropdown != null)
            {
                _dietaryPreferencesDropdown.Dropdown.UnregisterValueChangedCallback(OnDietaryPreferencesChanged);
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

            if( args != null && args.Length > 0)
            {
                // Handle any arguments passed during navigation if needed
                Debug.Log($"OnboardingProfileScreen received {args.Length} arguments.");
            }

            if (_viewModel != null)
            {
                await _viewModel.LoadCatalogDataAsync();
                _viewModel.PrePopulateFromState();
                PopulateDropdowns();
                PrePopulateDropdownSelections();
                UpdateSubmitButtonState();
            }
        }

        private void PopulateDropdowns()
        {
            if (_viewModel == null) return;

            ConfigureDropdown(_genderDropdown, _viewModel.GenderOptions);
            ConfigureDropdown(_activityLevelDropdown, _viewModel.ActivityLevelOptions);
            ConfigureDropdown(_dietaryPreferencesDropdown, _viewModel.DietaryPreferenceOptions);
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

        
        private void PrePopulateDropdownSelections()
        {
            SetDropdownSelection(_genderDropdown, _viewModel.SelectedGenderIndex);
            SetDropdownSelection(_activityLevelDropdown, _viewModel.SelectedActivityLevelIndex);
            SetDropdownSelection(_educationLevelDropdown, _viewModel.SelectedEducationLevelIndex);
            SetDropdownSelection(_annualIncomeDropdown, _viewModel.SelectedAnnualIncomeIndex);
        }

        private static void SetDropdownSelection(FormFieldItemDropDownField dropdown, int index)
        {
            if (dropdown == null || index < 0) return;
            dropdown.Dropdown.value = new[] { index };
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

        private void OnDietaryPreferencesChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            // This callback may not be needed if we're using individual checkboxes for dietary preferences
            // But if the dropdown is used for something else related to dietary preferences, handle it here
            if( _viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedDietaryPreferenceIndices = value.ToArray<int>();
            }
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

        private void OnSkipClicked()
        {
            if (_viewModel != null)
            {
                _viewModel.Skip();
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

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _submitButtonNode = CreateButtonNode(h, _submitButton, "Submit profile");
            _skipButtonNode = CreateButtonNode(h, _skipButton, "Skip profile setup");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _submitButtonNode = null;
            _skipButtonNode = null;
            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label)
        {
            if (button == null) return null;
            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;
            if (!button.enabledSelf) node.state = AccessibilityState.Disabled;
            node.frameGetter = () =>
            {
                if (button.panel == null) return Rect.zero;
                var r = button.worldBound;
                var s = button.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
            node.invoked += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
                return true;
            };
            return node;
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