using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using System;

namespace eu.foodmission.platform
{
    [Preserve]
    class RegisterScreen : NavigationScreenBase<RegisterViewModel>
    {
        private Unity.AppUI.UI.Button _registerButton;
        private FormFieldItemDropDownField _countryDropdown;
        private FormFieldItemDropDownField _regionDropdown;
        private FormFieldItemCheckbox _termsCheckbox;
        private FormFieldItemTextField _usernameField;
        private FormFieldItemTextField _emailField;
        private FormFieldItemPassword _passwordField;
        private FormFieldItemIntField _yearOfBirthField;
        private FormFieldItemTextField _postalCodeField;
        private Unity.AppUI.UI.Heading _heading;

        private AccessibilityNode _headingNode;
        private AccessibilityNode _registerButtonNode;

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public RegisterScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Register));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _registerButton = contentContainer.Q<Unity.AppUI.UI.Button>("register-button");
            _countryDropdown = contentContainer.Q<FormFieldItemDropDownField>("country");
            _regionDropdown = contentContainer.Q<FormFieldItemDropDownField>("region");
            _termsCheckbox = contentContainer.Q<FormFieldItemCheckbox>("checkbox");
            _usernameField = contentContainer.Q<FormFieldItemTextField>("username");
            _emailField = contentContainer.Q<FormFieldItemTextField>("email");
            _passwordField = contentContainer.Q<FormFieldItemPassword>("password");
            _yearOfBirthField = contentContainer.Q<FormFieldItemIntField>("yearofbirth");
            _postalCodeField = contentContainer.Q<FormFieldItemTextField>("postalcode");
            _heading = contentContainer.Q<Unity.AppUI.UI.Heading>();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel == null) return;

            _viewModel.PropertyChanged += OnPropertyChanged;
            _viewModel.ShowErrorRequest += OnShowErrorRequested;

            if (_countryDropdown != null)
            {
                _countryDropdown.Dropdown.sourceItems = _viewModel.CountryOptions;
                _countryDropdown.Dropdown.bindItem = (item, index) =>
                {
                    item.label = _viewModel.CountryOptions[index];
                    item.icon = null;
                };

                if (_viewModel.SelectedCountryIndex >= 0)
                {
                    _countryDropdown.Dropdown.SetValueWithoutNotify(new[] { _viewModel.SelectedCountryIndex });
                    UpdateRegionDropdown();
                }

                _countryDropdown.Dropdown.RegisterValueChangedCallback(OnCountryChanged);
            }

            if (_regionDropdown != null)
            {
                _regionDropdown.Dropdown.RegisterValueChangedCallback(OnRegionChanged);
            }
        }

        private void UpdateRegionDropdown()
        {
            if (_regionDropdown == null || _viewModel == null) return;

            _viewModel.UpdateRegionsForSelectedCountry();

            _regionDropdown.Dropdown.sourceItems = _viewModel.RegionOptions;
            _regionDropdown.Dropdown.bindItem = (item, index) =>
            {
                item.label = _viewModel.RegionOptions[index];
                item.icon = null;
            };

            if (_viewModel.SelectedRegionIndex >= 0)
            {
                _regionDropdown.Dropdown.SetValueWithoutNotify(new[] { _viewModel.SelectedRegionIndex });
            }
            else
            {
                _regionDropdown.Dropdown.SetValueWithoutNotify(new int[0]);
            }
        }

        private void OnCountryChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;

            _viewModel.SelectedCountryIndex = value[0];
            UpdateRegionDropdown();
        }

        private void OnRegionChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;

            _viewModel.SelectedRegionIndex = value[0];
        }

        private void RegisterManualEvents()
        {
            if (_registerButton != null)
            {
                _registerButton.clicked += OnRegisterClicked;
            }

            if (_termsCheckbox != null)
            {
                _termsCheckbox.Button.clicked += OnTermsLinkClicked;
            }
        }

        private void UnregisterManualEvents()
        {
            if (_registerButton != null)
            {
                _registerButton.clicked -= OnRegisterClicked;
            }

            if (_termsCheckbox != null)
            {
                _termsCheckbox.Button.clicked -= OnTermsLinkClicked;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;
            _viewModel.ShowErrorRequest -= OnShowErrorRequested;

            UnregisterManualEvents();

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
            _usernameField = null;
            _emailField = null;
            _passwordField = null;
            _yearOfBirthField = null;
            _postalCodeField = null;
            _termsCheckbox = null;
            _heading = null;

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

            _usernameField?.CreateAccessibilityNode(h, "Username");
            _emailField?.CreateAccessibilityNode(h, "Email");
            _passwordField?.CreateAccessibilityNode(h, "Password");
            _yearOfBirthField?.CreateAccessibilityNode(h, "Year of birth");
            _countryDropdown?.CreateAccessibilityNode(h, "Country");
            _regionDropdown?.CreateAccessibilityNode(h, "Region");
            _postalCodeField?.CreateAccessibilityNode(h, "Postal code");
            _termsCheckbox?.CreateAccessibilityNode(h, "Terms and conditions");

            if (_heading != null)
            {
                _headingNode = h.AddNode(_heading.text);
                _headingNode.role = AccessibilityRole.StaticText;
                _headingNode.frameGetter = MakeElementFrameGetter(_heading);
            }

            _registerButtonNode = CreateButtonNode(h, _registerButton, "Create account");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _headingNode = null;
            _registerButtonNode = null;

            _usernameField?.DestroyAccessibilityNode();
            _emailField?.DestroyAccessibilityNode();
            _passwordField?.DestroyAccessibilityNode();
            _yearOfBirthField?.DestroyAccessibilityNode();
            _countryDropdown?.DestroyAccessibilityNode();
            _regionDropdown?.DestroyAccessibilityNode();
            _postalCodeField?.DestroyAccessibilityNode();
            _termsCheckbox?.DestroyAccessibilityNode();

            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label)
        {
            if (button == null) return null;

            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;

            if (!button.enabledSelf)
            {
                node.state = AccessibilityState.Disabled;
            }

            node.frameGetter = MakeElementFrameGetter(button);

            node.invoked += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
                return true;
            };

            return node;
        }

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var rect = element.worldBound;
                var scale = element.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
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
            FMDialog.ShowScrollable(
                this,
                "@UI:T&C_TITLE",
                "@UI:T&C_TEXT",
                onAccept: () =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.HasAcceptedTerms = CheckboxState.Checked;
                    }
                }
            );
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == _viewModel)
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
            if (string.IsNullOrEmpty(message)) return;

            Toast.Build(this, message, NotificationDuration.Long)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }
    }
}
