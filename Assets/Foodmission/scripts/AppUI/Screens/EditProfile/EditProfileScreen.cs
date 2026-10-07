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
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform
{
    [Preserve]
    class EditProfileScreen : NavigationScreenBase<EditProfileViewModel>
    {
        private Unity.AppUI.UI.Button _submitButton;
        private Unity.AppUI.UI.Button _btnEditGoals;
        private Unity.AppUI.UI.Button _btnEditLevels;
        private Unity.AppUI.UI.Button _btnTerms;
        private Unity.AppUI.UI.Button _btnPrivacy;
        private Unity.AppUI.UI.Button _btnConsent;
        private VisualElement _spacerConsent;
        private Unity.AppUI.UI.Button _btnDeleteAccount;
        private FormFieldItemDropDownField _genderDropdown;
        private FormFieldItemDropDownField _activityLevelDropdown;
        private FormFieldItemDropDownField _dietaryPreferencesDropdown;
        private FormFieldItemDropDownField _educationLevelDropdown;
        private FormFieldItemDropDownField _annualIncomeDropdown;
        private FormFieldItemDropDownField _shoppingResponsibilityDropdown;
        private FormFieldItemDropDownField _motivationDropdown;
        private FormFieldItemDropDownField _dailyTimeCommitmentDropdown;
        private FormFieldItemDropDownField _countryDropdown;
        private FormFieldItemDropDownField _regionDropdown;
        private FormFieldItemDropDownField _yearOfBirthDropdown;

        private AccessibilityNode _submitButtonNode;
        private AccessibilityNode _btnTermsNode;
        private AccessibilityNode _btnPrivacyNode;
        private AccessibilityNode _btnConsentNode;
        private AccessibilityNode _btnDeleteAccountNode;

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
            _btnEditGoals = contentContainer.Q<Unity.AppUI.UI.Button>("btn-edit-goals");
            _btnEditLevels = contentContainer.Q<Unity.AppUI.UI.Button>("btn-edit-levels");
            _btnTerms = contentContainer.Q<Unity.AppUI.UI.Button>("btn-terms");
            _btnPrivacy = contentContainer.Q<Unity.AppUI.UI.Button>("btn-privacy");
            _btnConsent = contentContainer.Q<Unity.AppUI.UI.Button>("btn-consent");
            _spacerConsent = contentContainer.Q<VisualElement>("spacer-consent");
            _btnDeleteAccount = contentContainer.Q<Unity.AppUI.UI.Button>("btn-delete-account");
            _genderDropdown = contentContainer.Q<FormFieldItemDropDownField>("gender-dropdown");
            _activityLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("activity-level-dropdown");
            _educationLevelDropdown = contentContainer.Q<FormFieldItemDropDownField>("education-level-dropdown");
            _dietaryPreferencesDropdown = contentContainer.Q<FormFieldItemDropDownField>("dietary-preferences-dropdown");
            _annualIncomeDropdown = contentContainer.Q<FormFieldItemDropDownField>("annual-income-dropdown");
            _shoppingResponsibilityDropdown = contentContainer.Q<FormFieldItemDropDownField>("shopping-responsibility-dropdown");
            _motivationDropdown = contentContainer.Q<FormFieldItemDropDownField>("motivation-dropdown");
            _dailyTimeCommitmentDropdown = contentContainer.Q<FormFieldItemDropDownField>("daily-time-commitment-dropdown");
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

            if (_btnEditGoals != null)
            {
                _btnEditGoals.clicked += OnEditGoalsClicked;
            }

            if (_btnEditLevels != null)
            {
                _btnEditLevels.clicked += OnEditLevelsClicked;
            }

            if (_btnTerms != null)
            {
                _btnTerms.clicked += OnTermsClicked;
            }

            if (_btnPrivacy != null)
            {
                _btnPrivacy.clicked += OnPrivacyClicked;
            }

            if (_btnConsent != null)
            {
                _btnConsent.clicked += OnConsentClicked;
            }

            if (_btnDeleteAccount != null)
            {
                _btnDeleteAccount.clicked += OnDeleteAccountClicked;
            }

            if (_genderDropdown != null)
            {
                _genderDropdown.Dropdown.RegisterValueChangedCallback(OnGenderChanged);
            }

            if (_activityLevelDropdown != null)
            {
                _activityLevelDropdown.Dropdown.RegisterValueChangedCallback(OnActivityLevelChanged);
            }

            if (_dietaryPreferencesDropdown != null)
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

            if (_countryDropdown != null)
            {
                _countryDropdown.SetEnabled(false);
                _countryDropdown.Dropdown.RegisterValueChangedCallback(OnCountryChanged);
            }

            if (_regionDropdown != null)
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

            if (_btnEditGoals != null)
            {
                _btnEditGoals.clicked -= OnEditGoalsClicked;
            }

            if (_btnEditLevels != null)
            {
                _btnEditLevels.clicked -= OnEditLevelsClicked;
            }

            if (_btnTerms != null)
            {
                _btnTerms.clicked -= OnTermsClicked;
            }

            if (_btnPrivacy != null)
            {
                _btnPrivacy.clicked -= OnPrivacyClicked;
            }

            if (_btnConsent != null)
            {
                _btnConsent.clicked -= OnConsentClicked;
            }

            if (_btnDeleteAccount != null)
            {
                _btnDeleteAccount.clicked -= OnDeleteAccountClicked;
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

            if (_yearOfBirthDropdown != null)
            {
                _yearOfBirthDropdown.Dropdown.UnregisterValueChangedCallback(OnYearOfBirthChanged);
            }
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (args != null && args.Length > 0)
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

                bool isPilot = _viewModel.IsPilotCountry;
                if (_btnConsent != null)
                {
                    _btnConsent.style.display = isPilot ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (_spacerConsent != null)
                {
                    _spacerConsent.style.display = isPilot ? DisplayStyle.Flex : DisplayStyle.None;
                }
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
            if (_viewModel == null) return;
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

        private void OnEditGoalsClicked()
        {
            _navController?.Navigate(Unity.AppUI.Navigation.Generated.Actions.editprofile_to_onboardinggoals, new Argument("fromEditProfile", "true"));
        }

        private void OnEditLevelsClicked()
        {
            _navController?.Navigate(Unity.AppUI.Navigation.Generated.Actions.go_to_dimension_levels, DimensionLevelsNavigation.EditArguments());
        }

        private async void OnTermsClicked()
        {
            if (_viewModel == null) return;
            string text = await _viewModel.GetTermsTextAsync();
            if (string.IsNullOrEmpty(text)) text = "Missing Terms and Conditions text.";

            FMDialog.ShowScrollableMD(
                this,
                "@UI:T&C_TITLE",
                text,
                acceptLabel: null,
                cancelLabel: "@UI:TXT_BACK"
            );
        }

        private async void OnPrivacyClicked()
        {
            if (_viewModel == null) return;
            string text = await _viewModel.GetPrivacyTextAsync();
            if (string.IsNullOrEmpty(text)) text = "Missing Privacy Policy text.";

            FMDialog.ShowScrollableMD(
                this,
                "@UI:PRIVACY_POLICY_TITLE",
                text,
                acceptLabel: null,
                cancelLabel: "@UI:TXT_BACK"
            );
        }

        private async void OnConsentClicked()
        {
            if (_viewModel == null) return;
            string text = await _viewModel.GetPilotConsentTextAsync();
            if (string.IsNullOrEmpty(text)) text = "Missing Pilot Consent text.";

            FMDialog.ShowScrollableMD(
                this,
                "@UI:PILOT_CONSENT_TITLE",
                text,
                acceptLabel: null,
                cancelLabel: "@UI:TXT_BACK"
            );
        }

        private void OnDeleteAccountClicked()
        {
            FMDialog.ShowAlert(
                App.current?.rootVisualElement,
                "@UI:DELETE_ACCOUNT_TITLE",
                "@UI:DELETE_ACCOUNT_MESSAGE",
                AlertSemantic.Destructive,
                "@UI:TXT_ACCEPT", onOk: async () =>
                {
                    if (_viewModel == null) return;

                    var (success, error) = await _viewModel.DeleteAccountAsync();
                    if (success)
                    {
                        // Full logout (token, timers, per-user caches), not just the store action
                        App.current?.services?.GetService<IAuthService>()?.Logout();
                        _navController?.Navigate(Actions.go_to_auth);
                    }
                    else
                    {
                        Debug.LogError($"[EditProfileScreen] Delete account failed: {error}");
                        OnShowErrorRequested(error);
                    }
                },
                "@UI:TXT_CANCEL", onKo: () => { }
            );
        }



        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            TrackLoadingOverlay(() => _viewModel.IsLoading || _viewModel.IsSubmitting, nameof(EditProfileViewModel.IsLoading), nameof(EditProfileViewModel.IsSubmitting));
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
            _btnTermsNode = CreateButtonNode(_accessibilityHierarchy, _btnTerms, "Terms and conditions");
            _btnPrivacyNode = CreateButtonNode(_accessibilityHierarchy, _btnPrivacy, "Privacy policy");
            if (_viewModel != null && _viewModel.IsPilotCountry)
            {
                _btnConsentNode = CreateButtonNode(_accessibilityHierarchy, _btnConsent, "Pilot consent");
            }
            _btnDeleteAccountNode = CreateButtonNode(_accessibilityHierarchy, _btnDeleteAccount, "Delete account");
            _yearOfBirthDropdown?.CreateAccessibilityNode(_accessibilityHierarchy, "Year of birth");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _submitButtonNode = null;
            _btnTermsNode = null;
            _btnPrivacyNode = null;
            _btnConsentNode = null;
            _btnDeleteAccountNode = null;
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