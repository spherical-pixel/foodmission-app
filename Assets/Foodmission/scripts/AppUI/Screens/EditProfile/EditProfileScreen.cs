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
    class EditProfileScreen : NavigationScreenBase<EditProfileViewModel>
    {
        private Unity.AppUI.UI.Button _submitButton;
        private FormFieldItemDropDownField _genderDropdown;
        private FormFieldItemDropDownField _activityLevelDropdown;
        private FormFieldItemDropDownField _dietaryPreferencesDropdown;
        private FormFieldItemDropDownField _educationLevelDropdown;
        private FormFieldItemDropDownField _annualIncomeDropdown;
        private FormFieldItemDropDownField _shoppingResponsibilityDropdown;
        private FormFieldItemDropDownField _motivationDropdown;
        private FormFieldItemDropDownField _dailyTimeCommitmentDropdown;
        private FormFieldItemDropDownField _segmentDropdown;
        private FormFieldItemDropDownField _countryDropdown;
        private FormFieldItemDropDownField _regionDropdown;
        private FormFieldItemDropDownField _yearOfBirthDropdown;

        private AccessibilityNode _submitButtonNode;

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public EditProfileScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.EditProfile));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _submitButton = contentContainer.Q<Unity.AppUI.UI.Button>("submit-button");
            _genderDropdown = contentContainer.Q<FormFieldItemDropDownField>("gender-dropdown");
            _activityLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("activity-level-dropdown");
            _educationLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("education-level-dropdown");
            _dietaryPreferencesDropdown = contentContainer.Q<FormFieldItemDropDownField>("dietary-preferences-dropdown");
            _annualIncomeDropdown = contentContainer.Q<FormFieldItemDropDownField>("annual-income-dropdown");
            _shoppingResponsibilityDropdown = contentContainer.Q<FormFieldItemDropDownField>("shopping-responsibility-dropdown");
            _motivationDropdown = contentContainer.Q<FormFieldItemDropDownField>("motivation-dropdown");
            _dailyTimeCommitmentDropdown = contentContainer.Q<FormFieldItemDropDownField>("daily-time-commitment-dropdown");
            _segmentDropdown = contentContainer.Q<FormFieldItemDropDownField>("segment-dropdown");
            _countryDropdown = contentContainer.Q<FormFieldItemDropDownField>("country");
            _regionDropdown = contentContainer.Q<FormFieldItemDropDownField>("region");
            _yearOfBirthDropdown = contentContainer.Q<FormFieldItemDropDownField>("yearofbirth-dropdown");
            
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

            if (_motivationDropdown != null)
            {
                _motivationDropdown.Dropdown.RegisterValueChangedCallback(OnMotivationChanged);
            }

            if (_dailyTimeCommitmentDropdown != null)
            {
                _dailyTimeCommitmentDropdown.Dropdown.RegisterValueChangedCallback(OnDailyTimeCommitmentChanged);
            }

            if (_segmentDropdown != null)
            {
                _segmentDropdown.Dropdown.RegisterValueChangedCallback(OnSegmentChanged);
            }

            if( _countryDropdown != null)
            {
                _countryDropdown.Dropdown.RegisterValueChangedCallback(OnCountryChanged);
            }

            if( _regionDropdown != null)
            {
                _regionDropdown.Dropdown.RegisterValueChangedCallback(OnRegionChanged);
            }

            if (_yearOfBirthDropdown != null)
            {
                _yearOfBirthDropdown.Dropdown.RegisterValueChangedCallback(OnYearOfBirthChanged);
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

            if (_motivationDropdown != null)
            {
                _motivationDropdown.Dropdown.UnregisterValueChangedCallback(OnMotivationChanged);
            }

            if (_dailyTimeCommitmentDropdown != null)
            {
                _dailyTimeCommitmentDropdown.Dropdown.UnregisterValueChangedCallback(OnDailyTimeCommitmentChanged);
            }

            if (_segmentDropdown != null)
            {
                _segmentDropdown.Dropdown.UnregisterValueChangedCallback(OnSegmentChanged);
            }

            if (_yearOfBirthDropdown != null)
            {
                _yearOfBirthDropdown.Dropdown.UnregisterValueChangedCallback(OnYearOfBirthChanged);
            }
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if( args != null && args.Length > 0)
            {
                // Handle any arguments passed during navigation if needed
                Debug.Log($"EditProfileScreen received {args.Length} arguments.");
            }

            if (_viewModel != null)
            {
                await _viewModel.LoadCatalogDataAsync();
                await _viewModel.LoadCountriesAsync();
                await _viewModel.PrePopulateFromState();
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
            ConfigureDropdown(_motivationDropdown, _viewModel.MotivationOptions);
            ConfigureDropdown(_dailyTimeCommitmentDropdown, _viewModel.DailyTimeCommitmentOptions);
            ConfigureDropdown(_segmentDropdown, _viewModel.SegmentOptions);
            ConfigureDropdown(_countryDropdown, _viewModel.CountryOptions);
            ConfigureDropdown(_regionDropdown, _viewModel.RegionOptions);
            ConfigureDropdown(_yearOfBirthDropdown, _viewModel.YearOfBirthOptions);
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
            SetDropdownSelection(_shoppingResponsibilityDropdown, _viewModel.SelectedShoppingResponsibilityIndex);
            SetDropdownSelection(_motivationDropdown, _viewModel.SelectedMotivationIndex);
            SetDropdownSelection(_dailyTimeCommitmentDropdown, _viewModel.SelectedDailyTimeCommitmentIndex);
            SetDropdownSelection(_segmentDropdown, _viewModel.SelectedSegmentIndex);
            SetDropdownSelection(_countryDropdown, _viewModel.SelectedCountryIndex);
            SetDropdownSelection(_regionDropdown, _viewModel.SelectedRegionIndex);
            SetDropdownSelection(_yearOfBirthDropdown, _viewModel.SelectedYearOfBirthIndex);
            SetDropdownSelectionMulti(_dietaryPreferencesDropdown, _viewModel.SelectedDietaryPreferenceIndices);
        }

        private static void SetDropdownSelection(FormFieldItemDropDownField dropdown, int index)
        {
            if (dropdown == null || index < 0) return;
            dropdown.Dropdown.value = new[] { index };
        }

        private static void SetDropdownSelectionMulti(FormFieldItemDropDownField dropdown, int[] indices)
        {
            if (dropdown == null || indices == null || indices.Length == 0) return;
            dropdown.Dropdown.SetValueWithoutNotify(indices);
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
            // Multi-select dropdown: always sync (including empty selection) so deselection clears the array.
            if( _viewModel == null) return;
            _viewModel.SelectedDietaryPreferenceIndices = evt.newValue?.ToArray() ?? new int[0];
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

        private void OnMotivationChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedMotivationIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnDailyTimeCommitmentChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedDailyTimeCommitmentIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnSegmentChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedSegmentIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        private void OnYearOfBirthChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            if (_viewModel == null) return;
            var value = evt.newValue?.ToArray();
            if (value != null && value.Length > 0)
            {
                _viewModel.SelectedYearOfBirthIndex = value[0];
            }
            UpdateSubmitButtonState();
        }

        /// <summary>
        /// Handles country selection change
        /// </summary>
        private void OnCountryChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            
            if (_viewModel == null || value == null || value.Length == 0)
            {
                return;
            }

            _viewModel.SelectedCountryIndex = value[0];
            UpdateRegionDropdown();
        }

        /// <summary>
        /// Updates the region dropdown based on selected country (async — fetches from backend)
        /// </summary>
        private async void UpdateRegionDropdown()
        {
            if (_regionDropdown == null || _viewModel == null)
            {
                return;
            }

            try
            {
                await _viewModel.UpdateRegionsForSelectedCountryAsync();

                _regionDropdown.Dropdown.sourceItems = _viewModel.RegionOptions;
                _regionDropdown.Dropdown.bindItem = (item, index) =>
                {
                    item.label = _viewModel.RegionOptions[index];
                    item.icon = null;
                };

                // Set value to first item or clear
                if (_viewModel.SelectedRegionIndex >= 0)
                {
                    _regionDropdown.Dropdown.SetValueWithoutNotify(new[] { _viewModel.SelectedRegionIndex });
                }
                else
                {
                    _regionDropdown.Dropdown.SetValueWithoutNotify(new int[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditProfileScreen] UpdateRegionDropdown exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles region selection change
        /// </summary>
        private void OnRegionChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            //Debug.Log($"[RegisterScreen] OnRegionChanged called with: {value?.Length} items");
            if (_viewModel == null || value == null || value.Length == 0)
            {
                return;
            }

            _viewModel.SelectedRegionIndex = value[0];
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

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            _submitButtonNode = CreateButtonNode(_accessibilityHierarchy, _submitButton, "Save profile");
            _yearOfBirthDropdown?.CreateAccessibilityNode(_accessibilityHierarchy, "Year of birth");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _submitButtonNode = null;
            _yearOfBirthDropdown?.DestroyAccessibilityNode();
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
            if (e.PropertyName == nameof(EditProfileViewModel.IsSubmitting) ||
                e.PropertyName == nameof(EditProfileViewModel.IsFormValid))
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