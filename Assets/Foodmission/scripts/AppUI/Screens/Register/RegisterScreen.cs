using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Basic register screen
    /// </summary>
    [Preserve]
    class RegisterScreen : NavigationScreenBase<RegisterViewModel>
    {
        private Unity.AppUI.UI.Button _registerButton;
        private FormFieldItemDropDownField _countryDropdown;
        private FormFieldItemDropDownField _regionDropdown;
        private FormFieldItemCheckbox _termsCheckbox;
        
        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public RegisterScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.RegisterTemplate);
            CacheUIElements();
            RegisterManualEvents();
        }

        

        /// <summary>
        /// Cache UI elements for later use.
        /// </summary>
        private void CacheUIElements()
        {
            _registerButton = contentContainer.Q<Unity.AppUI.UI.Button>("register-button");
            _countryDropdown = contentContainer.Q<FormFieldItemDropDownField>("country");
            _regionDropdown = contentContainer.Q<FormFieldItemDropDownField>("region");
            _termsCheckbox = contentContainer.Q<FormFieldItemCheckbox>("checkbox");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel == null) return;

            _viewModel.PropertyChanged += OnPropertyChanged;
            _viewModel.ShowErrorRequest += OnShowErrorRequested;

            

            

            // Configure country dropdown
            if (_countryDropdown != null)
            {
                _countryDropdown.Dropdown.sourceItems = _viewModel.CountryOptions;
                _countryDropdown.Dropdown.bindItem = (item, index) =>
                {
                    item.label = _viewModel.CountryOptions[index];
                    item.icon = null;
                };

                // Set initial value if available
                if (_viewModel.SelectedCountryIndex >= 0)
                {
                    _countryDropdown.Dropdown.SetValueWithoutNotify(new[] { _viewModel.SelectedCountryIndex });
                    UpdateRegionDropdown();
                }

                // Subscribe to value changes using RegisterValueChangedCallback
                _countryDropdown.Dropdown.RegisterValueChangedCallback(OnCountryChanged);
            }

            // Configure region dropdown
            if (_regionDropdown != null)
            {
                _regionDropdown.Dropdown.RegisterValueChangedCallback(OnRegionChanged);
            }            
        }

        /// <summary>
        /// Updates the region dropdown based on selected country
        /// </summary>
        private void UpdateRegionDropdown()
        {
            if (_regionDropdown == null || _viewModel == null)
            {
                return;
            }

            _viewModel.UpdateRegionsForSelectedCountry();

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

        /// <summary>
        /// Manually register events
        /// </summary>
        private void RegisterManualEvents()
        {
            if (_registerButton != null)
            {
                _registerButton.clicked += OnRegisterClicked;
            }

            if( _termsCheckbox != null)
            {
                _termsCheckbox.Button.clicked += OnTermsLinkClicked;
            }
        }

        /// <summary>
        /// Un register manual events
        /// </summary>
        private void UnregisterManualEvents()
        {
            if (_registerButton != null)
            {
                _registerButton.clicked -= OnRegisterClicked;
            }

            if( _termsCheckbox != null)
            {
                _termsCheckbox.Button.clicked -= OnTermsLinkClicked;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;
            _viewModel.ShowErrorRequest -= OnShowErrorRequested;

            UnregisterManualEvents();

            // Unregister dropdown callbacks
            if (_countryDropdown != null)
            {
                _countryDropdown.Dropdown.UnregisterValueChangedCallback(OnCountryChanged);
            }
            if (_regionDropdown != null)
            {
                _regionDropdown.Dropdown.UnregisterValueChangedCallback(OnRegionChanged);
            }

            

            _registerButton = null;
            _countryDropdown = null;
            _regionDropdown = null;
            



            base.OnViewModelUnbinding();
        }

        private void OnRegisterClicked()
        {
            _viewModel?.Register();
        }

        

        private void OnTermsLinkClicked()
        {
            ShowTermsDialog();
        }

        private void ShowTermsDialog()
        {
            var termsContent = "@UI:T&C_TEXT";

            AlertDialog dialog = new AlertDialog
            {
                title = "@UI:T&C_TITLE",
                description = termsContent,
                variant = AlertSemantic.Information
            };

            dialog.SetPrimaryAction(0, "@UI:TXT_ACCEPT", () =>
            {
                if (_viewModel != null)
                {
                    _viewModel.HasAcceptedTerms = CheckboxState.Checked;
                }
            });

            dialog.SetCancelAction(1, "@UI:TXT_CANCEL");

            var modal = Modal.Build(_termsCheckbox, dialog);
            modal.Show();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if( sender == _viewModel)
            {
                switch (e.PropertyName)
                {
                    case nameof(RegisterViewModel.Username):
                        _viewModel.ValidateUsername();
                        break;
                    case nameof(RegisterViewModel.Email):
                        _viewModel.ValidateEmail();
                        break;
                    case nameof(RegisterViewModel.Password):
                        _viewModel.ValidatePassword();
                        break;
                    case nameof(RegisterViewModel.YearOfBirth):
                        _viewModel.ValidateYearOfBirth();
                        break;
                    case nameof(RegisterViewModel.PostalCode):
                        _viewModel.ValidatePostalCode();
                        break;
                    case nameof(RegisterViewModel.HasAcceptedTerms):
                        _viewModel.ValidateTerms();
                        break;
                }
            }
        }

        void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Toast.Build(this, message, NotificationDuration.Long)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }

        
    }
}
