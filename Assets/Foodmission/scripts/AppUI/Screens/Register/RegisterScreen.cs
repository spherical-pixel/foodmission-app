using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.Navigation.Generated;
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
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel == null) return;

            _viewModel.PropertyChanged += OnPropertyChanged;
            
            // Configure country dropdown
            if (_countryDropdown != null)
            {
                _countryDropdown.Dropdown.sourceItems = _viewModel.CountryOptions;
                Debug.Log("Country options set successfully. -> " + _viewModel.CountryOptions.Count);
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
                return;

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
            Debug.LogError($"[RegisterScreen] OnCountryChanged called with: {value?.Length} items");
            if (_viewModel == null || value == null || value.Length == 0)
                return;

            _viewModel.SelectedCountryIndex = value[0];
            UpdateRegionDropdown();
        }

        /// <summary>
        /// Handles region selection change
        /// </summary>
        private void OnRegionChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            Debug.LogError($"[RegisterScreen] OnRegionChanged called with: {value?.Length} items");
            if (_viewModel == null || value == null || value.Length == 0)
                return;

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

        }

        protected override void OnViewModelUnbinding()
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;

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

                }
            }
        }

        
    }
}
