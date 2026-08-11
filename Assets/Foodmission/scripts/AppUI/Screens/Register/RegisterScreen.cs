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
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class RegisterScreen : StepFlowScreenBase<RegisterViewModel>
    {
        protected override int StepCount => 9;


        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:REGISTER";

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;

        // Reusable FormFieldItem components
        private FormFieldItemTextField _usernameField;
        private FormFieldItemTextField _emailField;
        private FormFieldItemPassword _passwordField;
        private FormFieldItemDropDownField _yearOfBirthDropdown;
        private FormFieldItemDropDownField _countryDropdown;
        private FormFieldItemDropDownField _regionDropdown;
        private FormFieldItemTextField _postalCodeField;
        private FormFieldItemCheckbox _termsCheckbox;
        private FormFieldItemCheckbox _privacyCheckbox;
        private FormFieldItemCheckbox _consentCheckbox;
        private VisualElement _pilotConsentContainer;

        private Unity.AppUI.UI.Text _noPilotText;

        protected override bool IsFixedContent => true;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public RegisterScreen() : base()
        {
            CreateFormFields();
        }

        private void CreateFormFields()
        {
            _usernameField = new FormFieldItemTextField
            {
                name = "username",
                HeadingText = "@UI:USERNAME",
                TextFieldPlaceholder = "@UI:PLACEHOLDER_USERNAME"
            };

            _emailField = new FormFieldItemTextField
            {
                name = "email",
                HeadingText = "@UI:EMAIL",
                TextFieldPlaceholder = "@UI:EMAIL_PLACEHOLDER"
            };

            _passwordField = new FormFieldItemPassword
            {
                name = "password",
                HeadingText = "@UI:PASSWORD",
                TextFieldPlaceholder = "@UI:PLACEHOLDER_PASSWORD"
            };

            _yearOfBirthDropdown = new FormFieldItemDropDownField
            {
                name = "yearofbirth-dropdown",
                HeadingText = "@UI:MEMBER_YEAR_OF_BIRTH",
                DropdownDefaultMessage = "@UI:MEMBER_YEAR_OF_BIRTH_PLACEHOLDER"
            };

            _countryDropdown = new FormFieldItemDropDownField
            {
                name = "country",
                HeadingText = "@UI:COUNTRY",
                DropdownDefaultMessage = "@UI:COUNTRY_PLACEHOLDER"
            };

            _regionDropdown = new FormFieldItemDropDownField
            {
                name = "region",
                HeadingText = "@UI:REGION",
                DropdownDefaultMessage = "@UI:REGION_PLACEHOLDER"
            };

            _postalCodeField = new FormFieldItemTextField
            {
                name = "postalcode",
                HeadingText = "@UI:LABEL_POSTAL_CODE",
                TextFieldPlaceholder = "@UI:PLACEHOLDER_POSTAL_CODE"
            };

            _termsCheckbox = new FormFieldItemCheckbox
            {
                name = "terms-checkbox",
                Text = "@UI:ACCEPT_TERMS"
            };

            _privacyCheckbox = new FormFieldItemCheckbox
            {
                name = "privacy-checkbox",
                Text = "@UI:ACCEPT_PRIVACY"
            };

            _consentCheckbox = new FormFieldItemCheckbox
            {
                name = "consent-checkbox",
                Text = "@UI:ACCEPT_PILOT_CONSENT"
            };
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            return stepIndex switch
            {
                0 => BuildWelcomeStep(),
                1 => BuildCardStep(_usernameField),
                2 => BuildCardStep(_emailField),
                3 => BuildCardStep(_passwordField),
                4 => BuildCardStep(_yearOfBirthDropdown),
                5 => BuildLocationStep(),
                6 => BuildTermsStep(),//BuildLegalStep(_termsCheckbox, "@UI:BTN_READ_TERMS", ShowTermsDialog),
                7 => BuildPrivacyStep(),//BuildLegalStep(_privacyCheckbox, "@UI:BTN_READ_PRIVACY", ShowPrivacyPolicyDialog),
                8 => BuildConsentStep(),//BuildLegalStep(_consentCheckbox, "@UI:BTN_READ_CONSENT", ShowPilotConsentDialog),
                _ => new VisualElement()
            };
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            var container = new VisualElement();
            slot.Add(container);

            _nutriView = new FMNutriView();
            _nutriView.AddToClassList("fm-step-flow__guide-nutri");
            container.Add(_nutriView);

            _messageCard = new ExVisualElement();
            _messageCard.AddToClassList("box-background");
            _messageCard.AddToClassList("fm-shadow-wrapper");
            _messageCard.AddToClassList("fm-step-flow__guide-card");

            _messageText = new Unity.AppUI.UI.Text { text = "" };
            _messageText.style.whiteSpace = WhiteSpace.Normal;
            _messageText.primary = false;
            _messageText.size = TextSize.L;
            _messageCard.Add(_messageText);

            _messageCard.style.display = DisplayStyle.None;
            slot.Add(_messageCard);
        }

        public void UpdateMascotMessage(string newMessage)
        {
            if (_messageCard == null || _messageText == null) return;

            newMessage ??= string.Empty;

            if (string.IsNullOrWhiteSpace(newMessage))
            {
                _messageCard.RemoveFromClassList("fm-step-flow__guide-card--visible");
                _messageCard.AddToClassList("fm-step-flow__guide-card--exit");
                _messageCard.style.display = DisplayStyle.None;
                _messageText.text = string.Empty;
                return;
            }

            if (_messageText.text == newMessage && _messageCard.style.display == DisplayStyle.Flex && _messageCard.ClassListContains("fm-step-flow__guide-card--visible"))
            {
                return;
            }

            _messageCard.style.display = DisplayStyle.Flex;

            _messageCard.RemoveFromClassList("fm-step-flow__guide-card--visible");
            _messageCard.AddToClassList("fm-step-flow__guide-card--exit");

            _messageCard.schedule.Execute(() =>
            {
                _messageText.text = newMessage;
                _messageCard.RemoveFromClassList("fm-step-flow__guide-card--exit");
                _messageCard.AddToClassList("fm-step-flow__guide-card--visible");
            }).StartingIn(150);
        }

        protected override void OnStepChanged(int stepIndex)
        {
            base.OnStepChanged(stepIndex);

            if (stepIndex == 8)
            {
                UpdateConsentStepUI();
            }

            string message = stepIndex switch
            {
                0 => string.Empty,//"@UI:TXT_REG_STEP_WELCOME",
                1 => "@UI:TXT_REG_STEP_USERNAME",
                2 => "@UI:TXT_REG_STEP_EMAIL",
                3 => "@UI:TXT_REG_STEP_PASSWORD",
                4 => "@UI:TXT_REG_STEP_BIRTHYEAR",
                5 => "@UI:TXT_REG_STEP_LOCATION",
                6 => "@UI:TXT_REG_STEP_TERMS",
                7 => "@UI:TXT_REG_STEP_PRIVACY",
                8 => _viewModel != null && !_viewModel.IsPilotCountry ? GetNoPilotMessageText() : "@UI:TXT_REG_STEP_CONSENT",
                _ => ""
            };

            UpdateMascotMessage(message);
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null)
            {
                await _viewModel.LoadCountriesAsync();

                if (_countryDropdown != null && _viewModel.CountryOptions.Count > 0)
                {
                    ConfigureDropdown(_countryDropdown, _viewModel.CountryOptions);
                }

                if (_yearOfBirthDropdown != null && _viewModel.YearOfBirthOptions.Count > 0)
                {
                    ConfigureDropdown(_yearOfBirthDropdown, _viewModel.YearOfBirthOptions);
                }
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel == null) return;

            _viewModel.PropertyChanged += OnPropertyChanged;
            _viewModel.ShowErrorRequest += OnShowErrorRequested;

            BindFieldsToViewModel();
            SyncUIFromViewModel();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
            }

            UnbindFieldsFromViewModel();
            base.OnViewModelUnbinding();
        }

        private void SyncUIFromViewModel()
        {
            if (_viewModel == null) return;

            if (_usernameField != null)
            {
                _usernameField.TextFieldValue = _viewModel.Username ?? "";
                _usernameField.HelpTextText = _viewModel.UsernameHelpTextValue ?? "";
                _usernameField.HelpTextVariant = _viewModel.UsernameHelpTextVariant;
            }

            if (_emailField != null)
            {
                _emailField.TextFieldValue = _viewModel.Email ?? "";
                _emailField.HelpTextText = _viewModel.EmailHelpTextValue ?? "";
                _emailField.HelpTextVariant = _viewModel.EmailHelpTextVariant;
            }

            if (_passwordField != null)
            {
                _passwordField.TextFieldValue = _viewModel.Password ?? "";
                _passwordField.HelpTextText = _viewModel.PasswordHelpTextValue ?? "";
                _passwordField.HelpTextVariant = _viewModel.PasswordHelpTextVariant;
            }

            if (_yearOfBirthDropdown != null)
            {
                _yearOfBirthDropdown.HelpTextText = _viewModel.YearOfBirthHelpTextValue ?? "";
                _yearOfBirthDropdown.HelpTextVariant = _viewModel.YearOfBirthHelpTextVariant;
            }

            if (_countryDropdown != null)
            {
                _countryDropdown.HelpTextText = _viewModel.CountryHelpTextValue ?? "";
                _countryDropdown.HelpTextVariant = _viewModel.CountryHelpTextVariant;
            }

            if (_regionDropdown != null)
            {
                _regionDropdown.HelpTextText = _viewModel.RegionHelpTextValue ?? "";
                _regionDropdown.HelpTextVariant = _viewModel.RegionHelpTextVariant;
            }

            if (_postalCodeField != null)
            {
                _postalCodeField.TextFieldValue = _viewModel.PostalCode ?? "";
                _postalCodeField.HelpTextText = _viewModel.PostalCodeHelpTextValue ?? "";
                _postalCodeField.HelpTextVariant = _viewModel.PostalCodeHelpTextVariant;
            }

            if (_termsCheckbox != null)
            {
                _termsCheckbox.CheckboxValue = _viewModel.HasAcceptedTerms;
                _termsCheckbox.HelpTextText = _viewModel.TermsHelpTextValue ?? "";
                _termsCheckbox.HelpTextVariant = _viewModel.TermsHelpTextVariant;
            }

            if (_privacyCheckbox != null)
            {
                _privacyCheckbox.CheckboxValue = _viewModel.HasAcceptedPrivacyPolicy;
                _privacyCheckbox.HelpTextText = _viewModel.PrivacyHelpTextValue ?? "";
                _privacyCheckbox.HelpTextVariant = _viewModel.PrivacyHelpTextVariant;
            }

            if (_consentCheckbox != null)
            {
                _consentCheckbox.CheckboxValue = _viewModel.HasAcceptedPilotConsent;
                _consentCheckbox.HelpTextText = _viewModel.ConsentHelpTextValue ?? "";
                _consentCheckbox.HelpTextVariant = _viewModel.ConsentHelpTextVariant;
            }
        }

        private void BindFieldsToViewModel()
        {
            // Username field binding
            var usernameText = _usernameField?.Q<Unity.AppUI.UI.TextField>();
            if (usernameText != null)
            {
                usernameText.RegisterValueChangedCallback(OnUsernameInputChanged);
            }

            // Email field binding
            var emailText = _emailField?.Q<Unity.AppUI.UI.TextField>();
            if (emailText != null)
            {
                emailText.RegisterValueChangedCallback(OnEmailInputChanged);
            }

            // Password field binding
            var passwordText = _passwordField?.Q<Unity.AppUI.UI.TextField>();
            if (passwordText != null)
            {
                passwordText.RegisterValueChangedCallback(OnPasswordInputChanged);
            }

            // Year of Birth dropdown
            if (_yearOfBirthDropdown != null)
            {
                ConfigureDropdown(_yearOfBirthDropdown, _viewModel.YearOfBirthOptions);
                _yearOfBirthDropdown.Dropdown.RegisterValueChangedCallback(OnYearOfBirthChanged);
            }

            // Country dropdown
            if (_countryDropdown != null)
            {
                ConfigureDropdown(_countryDropdown, _viewModel.CountryOptions);
                if (_viewModel.SelectedCountryIndex >= 0)
                {
                    _countryDropdown.Dropdown.SetValueWithoutNotify(new[] { _viewModel.SelectedCountryIndex });
                    UpdateRegionDropdown();
                }
                _countryDropdown.Dropdown.RegisterValueChangedCallback(OnCountryChanged);
            }

            // Region dropdown
            if (_regionDropdown != null)
            {
                _regionDropdown.Dropdown.RegisterValueChangedCallback(OnRegionChanged);
            }

            // Postal Code field binding
            var postalText = _postalCodeField?.Q<Unity.AppUI.UI.TextField>();
            if (postalText != null)
            {
                postalText.RegisterValueChangedCallback(OnPostalCodeInputChanged);
            }

            // Checkboxes bindings
            BindCheckbox(_termsCheckbox, state => _viewModel.HasAcceptedTerms = state, () => _viewModel.ValidateTerms());
            BindCheckbox(_privacyCheckbox, state => _viewModel.HasAcceptedPrivacyPolicy = state, () => _viewModel.ValidatePrivacyPolicy());
            BindCheckbox(_consentCheckbox, state => _viewModel.HasAcceptedPilotConsent = state, () => _viewModel.ValidatePilotConsent());
        }

        private void UnbindFieldsFromViewModel()
        {
            var usernameText = _usernameField?.Q<Unity.AppUI.UI.TextField>();
            if (usernameText != null) usernameText.UnregisterValueChangedCallback(OnUsernameInputChanged);

            var emailText = _emailField?.Q<Unity.AppUI.UI.TextField>();
            if (emailText != null) emailText.UnregisterValueChangedCallback(OnEmailInputChanged);

            var passwordText = _passwordField?.Q<Unity.AppUI.UI.TextField>();
            if (passwordText != null) passwordText.UnregisterValueChangedCallback(OnPasswordInputChanged);

            if (_yearOfBirthDropdown != null) _yearOfBirthDropdown.Dropdown.UnregisterValueChangedCallback(OnYearOfBirthChanged);
            if (_countryDropdown != null) _countryDropdown.Dropdown.UnregisterValueChangedCallback(OnCountryChanged);
            if (_regionDropdown != null) _regionDropdown.Dropdown.UnregisterValueChangedCallback(OnRegionChanged);

            var postalText = _postalCodeField?.Q<Unity.AppUI.UI.TextField>();
            if (postalText != null) postalText.UnregisterValueChangedCallback(OnPostalCodeInputChanged);
        }

        private void BindCheckbox(FormFieldItemCheckbox checkbox, Action<CheckboxState> setter, Func<bool> validator)
        {
            if (checkbox == null) return;
            var cb = checkbox.Q<Checkbox>();
            if (cb != null)
            {
                cb.RegisterValueChangedCallback(evt =>
                {
                    setter?.Invoke(evt.newValue);
                    validator?.Invoke();
                    _viewModel?.InvalidateValidation();
                });
            }
        }

        private void OnUsernameInputChanged(ChangeEvent<string> evt)
        {
            if (_viewModel == null) return;
            _viewModel.Username = evt.newValue;
            _viewModel.ValidateUsername();
            _viewModel.InvalidateValidation();
        }

        private void OnEmailInputChanged(ChangeEvent<string> evt)
        {
            if (_viewModel == null) return;
            _viewModel.Email = evt.newValue;
            _viewModel.ValidateEmail();
            _viewModel.InvalidateValidation();
        }

        private void OnPasswordInputChanged(ChangeEvent<string> evt)
        {
            if (_viewModel == null) return;
            _viewModel.Password = evt.newValue;
            _viewModel.ValidatePassword();
            _viewModel.InvalidateValidation();
        }

        private void OnPostalCodeInputChanged(ChangeEvent<string> evt)
        {
            if (_viewModel == null) return;
            _viewModel.PostalCode = evt.newValue;
            _viewModel.ValidatePostalCode();
            _viewModel.InvalidateValidation();
        }

        private void OnYearOfBirthChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;

            _viewModel.SelectedYearOfBirthIndex = value[0];
            _viewModel.ValidateYearOfBirth();
            _viewModel.InvalidateValidation();
        }

        private void OnCountryChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;

            _viewModel.SelectedCountryIndex = value[0];
            UpdateRegionDropdown();
            _viewModel.InvalidateValidation();
        }

        private void OnRegionChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            var value = evt.newValue?.ToArray();
            if (_viewModel == null || value == null || value.Length == 0) return;

            _viewModel.SelectedRegionIndex = value[0];
            _viewModel.InvalidateValidation();
        }

        private async void UpdateRegionDropdown()
        {
            if (_regionDropdown == null || _viewModel == null) return;

            try
            {
                await _viewModel.UpdateRegionsForSelectedCountryAsync();

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

                _viewModel.ValidateRegion(showError: false);
                _viewModel.InvalidateValidation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RegisterScreen] UpdateRegionDropdown exception: {ex.Message}");
            }
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

        // ── Card Container Builders ──────────────────────────────────
        private VisualElement BuildWelcomeStep()
        {
            var root = new ExVisualElement();
            root.AddToClassList("box-background");
            root.AddToClassList("fm-shadow-wrapper");
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.Center;
            root.style.paddingTop = 40;
            root.style.paddingBottom = 40;
            root.style.paddingLeft = 40;
            root.style.paddingRight = 40;
            //root.style.height = new StyleLength(Length.Percent(100));

            var welcomeHeader = new Unity.AppUI.UI.Heading { text = "@UI:WELCOME_TO_FOODMISSION" };
            welcomeHeader.size = HeadingSize.L;
            welcomeHeader.AddToClassList("centered-text");
            root.Add(welcomeHeader);

            var spacer = new Unity.AppUI.UI.Spacer { spacing = SpacerSpacing.XL };
            root.Add(spacer);

            var welcomeBody = new Unity.AppUI.UI.Text
            {
                text = "@UI:TXT_REG_STEP_WELCOME",
                primary = false
            };
            welcomeBody.style.whiteSpace = WhiteSpace.Normal;
            welcomeBody.size = TextSize.L;
            // welcomeBody.AddToClassList("centered-text");
            root.Add(welcomeBody);

            return root;
        }

        private VisualElement BuildCardStep(VisualElement element)
        {
            var root = new ExVisualElement();
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));

            var box = new ExVisualElement();
            box.AddToClassList("box-background");
            box.AddToClassList("fm-shadow-wrapper");
            box.style.flexDirection = FlexDirection.Column;
            box.style.paddingTop = 30;
            box.style.paddingBottom = 30;
            box.style.paddingLeft = 30;
            box.style.paddingRight = 30;
            box.style.width = new StyleLength(Length.Percent(100));

            root.Add(box);

            if (element != null)
            {
                box.Add(element);
            }

            return root;
        }

        private VisualElement BuildLocationStep()
        {
            var root = new ExVisualElement();
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));

            var box = new ExVisualElement();
            box.AddToClassList("box-background");
            box.AddToClassList("fm-shadow-wrapper");
            box.style.flexDirection = FlexDirection.Column;
            box.style.paddingTop = 30;
            box.style.paddingBottom = 30;
            box.style.paddingLeft = 30;
            box.style.paddingRight = 30;

            if (_countryDropdown != null)
            {
                box.Add(_countryDropdown);
                box.Add(new Unity.AppUI.UI.Spacer { spacing = SpacerSpacing.XL });
            }

            if (_regionDropdown != null)
            {
                box.Add(_regionDropdown);
                box.Add(new Unity.AppUI.UI.Spacer { spacing = SpacerSpacing.XL });
            }

            if (_postalCodeField != null)
            {
                box.Add(_postalCodeField);
                box.Add(new Unity.AppUI.UI.Spacer { spacing = SpacerSpacing.XL });
            }

            root.Add(box);
            return root;
        }

        private VisualElement BuildTermsStep()
        {
            return BuildLegalStep(_termsCheckbox, "@UI:BTN_READ_TERMS", ShowTermsDialog);
        }

        private VisualElement BuildPrivacyStep()
        {
            return BuildLegalStep(_privacyCheckbox, "@UI:BTN_READ_PRIVACY", ShowPrivacyPolicyDialog);
        }

        private VisualElement BuildConsentStep()
        {
            var root = new ExVisualElement();
            root.AddToClassList("box-background");
            root.AddToClassList("fm-shadow-wrapper");
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));

            // Pilot container (shown when IsPilotCountry is true)
            _pilotConsentContainer = new VisualElement();
            _pilotConsentContainer.style.flexDirection = FlexDirection.Column;
            _pilotConsentContainer.style.width = new StyleLength(Length.Percent(100));


            if (_consentCheckbox != null)
            {
                _pilotConsentContainer.Add(_consentCheckbox);
            }

            var btnRead = new FMButton
            {
                title = "@UI:BTN_READ_CONSENT",
                variant = ButtonVariant.Default,
                size = Size.M
            };
            btnRead.AddToClassList("fm-button");
            btnRead.style.marginTop = 12;
            btnRead.clicked += ShowPilotConsentDialog;
            _pilotConsentContainer.Add(btnRead);

            root.Add(_pilotConsentContainer);



            UpdateConsentStepUI();

            return root;
        }

        private string GetNoPilotMessageText()
        {
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MSG_NO_PILOT_COUNTRY");
            if (!string.IsNullOrEmpty(localized) && localized != "MSG_NO_PILOT_COUNTRY")
            {
                return localized;
            }
            return "Your selected country does not participate in the active pilot study. However, you can register and use Foodmission normally!";
        }

        private void UpdateConsentStepUI()
        {
            if (_viewModel == null || _pilotConsentContainer == null)
                return;

            bool isPilot = _viewModel.IsPilotCountry;

            if (isPilot)
            {
                _pilotConsentContainer.style.display = DisplayStyle.Flex;
                _pilotConsentContainer.parent.style.display = DisplayStyle.Flex;
            }
            else
            {
                _pilotConsentContainer.style.display = DisplayStyle.None;
                _pilotConsentContainer.parent.style.display = DisplayStyle.None;
            }

            if (_noPilotText != null)
            {
                _noPilotText.text = GetNoPilotMessageText();
            }
        }

        private VisualElement BuildLegalStep(FormFieldItemCheckbox checkbox, string buttonTitleKey, Action onReadClicked)
        {
            var root = new ExVisualElement();
            root.AddToClassList("box-background");
            root.AddToClassList("fm-shadow-wrapper");
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));

            if (checkbox != null)
            {
                root.Add(checkbox);
            }

            var btnRead = new FMButton
            {
                title = buttonTitleKey,
                variant = ButtonVariant.Default,
                size = Size.M
            };
            btnRead.AddToClassList("fm-button");
            btnRead.style.marginTop = 12;
            btnRead.clicked += onReadClicked;
            root.Add(btnRead);

            return root;
        }

        // ── Legal Dialog Helpers ─────────────────────────────────────
        private void ShowTermsDialog()
        {
            FMDialog.ShowScrollableMD(
                this,
                "@UI:T&C_TITLE",
                // TODO: Right now we don't have text for T&C it should come from backend at some point
                "Missing Terms and Conditions text.",
                onAccept: () =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.HasAcceptedTerms = CheckboxState.Checked;
                        _viewModel.ValidateTerms();
                        _viewModel.InvalidateValidation();
                    }
                }
            );
        }

        private void ShowPrivacyPolicyDialog()
        {
            FMDialog.ShowScrollable(
                this,
                "@UI:PRIVACY_POLICY_TITLE",
                // TODO: Right now we don't have text for Privacy Policy it should come from backend at some point
                "Missing Privacy Policy text.",
                onAccept: () =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.HasAcceptedPrivacyPolicy = CheckboxState.Checked;
                        _viewModel.ValidatePrivacyPolicy();
                        _viewModel.InvalidateValidation();
                    }
                }
            );
        }

        private void ShowPilotConsentDialog()
        {
            string consentMD = _viewModel != null && !string.IsNullOrEmpty(_viewModel.PilotConsentText)
                ? _viewModel.PilotConsentText
                : "MISSING CONSENT TEXT";

            FMDialog.ShowScrollableMD(
                this,
                "@UI:PILOT_CONSENT_TITLE",
                consentMD,
                onAccept: () =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.HasAcceptedPilotConsent = CheckboxState.Checked;
                        _viewModel.ValidatePilotConsent();
                        _viewModel.InvalidateValidation();
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
                    case nameof(RegisterViewModel.UsernameHelpTextValue):
                    case nameof(RegisterViewModel.UsernameHelpTextVariant):
                        if (_usernameField != null)
                        {
                            _usernameField.HelpTextText = _viewModel.UsernameHelpTextValue ?? "";
                            _usernameField.HelpTextVariant = _viewModel.UsernameHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.Email):
                        _viewModel.ValidateEmail();
                        break;
                    case nameof(RegisterViewModel.EmailHelpTextValue):
                    case nameof(RegisterViewModel.EmailHelpTextVariant):
                        if (_emailField != null)
                        {
                            _emailField.HelpTextText = _viewModel.EmailHelpTextValue ?? "";
                            _emailField.HelpTextVariant = _viewModel.EmailHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.Password):
                        _viewModel.ValidatePassword();
                        break;
                    case nameof(RegisterViewModel.PasswordHelpTextValue):
                    case nameof(RegisterViewModel.PasswordHelpTextVariant):
                        if (_passwordField != null)
                        {
                            _passwordField.HelpTextText = _viewModel.PasswordHelpTextValue ?? "";
                            _passwordField.HelpTextVariant = _viewModel.PasswordHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.YearOfBirthHelpTextValue):
                    case nameof(RegisterViewModel.YearOfBirthHelpTextVariant):
                        if (_yearOfBirthDropdown != null)
                        {
                            _yearOfBirthDropdown.HelpTextText = _viewModel.YearOfBirthHelpTextValue ?? "";
                            _yearOfBirthDropdown.HelpTextVariant = _viewModel.YearOfBirthHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.CountryHelpTextValue):
                    case nameof(RegisterViewModel.CountryHelpTextVariant):
                        if (_countryDropdown != null)
                        {
                            _countryDropdown.HelpTextText = _viewModel.CountryHelpTextValue ?? "";
                            _countryDropdown.HelpTextVariant = _viewModel.CountryHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.RegionHelpTextValue):
                    case nameof(RegisterViewModel.RegionHelpTextVariant):
                        if (_regionDropdown != null)
                        {
                            _regionDropdown.HelpTextText = _viewModel.RegionHelpTextValue ?? "";
                            _regionDropdown.HelpTextVariant = _viewModel.RegionHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.PostalCode):
                        _viewModel.ValidatePostalCode();
                        break;
                    case nameof(RegisterViewModel.PostalCodeHelpTextValue):
                    case nameof(RegisterViewModel.PostalCodeHelpTextVariant):
                        if (_postalCodeField != null)
                        {
                            _postalCodeField.HelpTextText = _viewModel.PostalCodeHelpTextValue ?? "";
                            _postalCodeField.HelpTextVariant = _viewModel.PostalCodeHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.HasAcceptedTerms):
                        _viewModel.ValidateTerms();
                        if (_termsCheckbox != null)
                        {
                            _termsCheckbox.CheckboxValue = _viewModel.HasAcceptedTerms;
                        }
                        break;
                    case nameof(RegisterViewModel.TermsHelpTextValue):
                    case nameof(RegisterViewModel.TermsHelpTextVariant):
                        if (_termsCheckbox != null)
                        {
                            _termsCheckbox.HelpTextText = _viewModel.TermsHelpTextValue ?? "";
                            _termsCheckbox.HelpTextVariant = _viewModel.TermsHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.HasAcceptedPrivacyPolicy):
                        _viewModel.ValidatePrivacyPolicy();
                        if (_privacyCheckbox != null)
                        {
                            _privacyCheckbox.CheckboxValue = _viewModel.HasAcceptedPrivacyPolicy;
                        }
                        break;
                    case nameof(RegisterViewModel.PrivacyHelpTextValue):
                    case nameof(RegisterViewModel.PrivacyHelpTextVariant):
                        if (_privacyCheckbox != null)
                        {
                            _privacyCheckbox.HelpTextText = _viewModel.PrivacyHelpTextValue ?? "";
                            _privacyCheckbox.HelpTextVariant = _viewModel.PrivacyHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.HasAcceptedPilotConsent):
                        _viewModel.ValidatePilotConsent();
                        if (_consentCheckbox != null)
                        {
                            _consentCheckbox.CheckboxValue = _viewModel.HasAcceptedPilotConsent;
                        }
                        break;
                    case nameof(RegisterViewModel.ConsentHelpTextValue):
                    case nameof(RegisterViewModel.ConsentHelpTextVariant):
                        if (_consentCheckbox != null)
                        {
                            _consentCheckbox.HelpTextText = _viewModel.ConsentHelpTextValue ?? "";
                            _consentCheckbox.HelpTextVariant = _viewModel.ConsentHelpTextVariant;
                        }
                        break;
                    case nameof(RegisterViewModel.IsPilotCountry):
                    case nameof(RegisterViewModel.PilotConsentText):
                        UpdateConsentStepUI();
                        break;
                }
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

        // ── Accessibility ───────────────────────────────────────────
        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _usernameField?.CreateAccessibilityNode(h, "Username");
            _emailField?.CreateAccessibilityNode(h, "Email");
            _passwordField?.CreateAccessibilityNode(h, "Password");
            _yearOfBirthDropdown?.CreateAccessibilityNode(h, "Year of birth");
            _countryDropdown?.CreateAccessibilityNode(h, "Country");
            _regionDropdown?.CreateAccessibilityNode(h, "Region");
            _postalCodeField?.CreateAccessibilityNode(h, "Postal code");
            _termsCheckbox?.CreateAccessibilityNode(h, "Terms and conditions");
            _privacyCheckbox?.CreateAccessibilityNode(h, "Privacy Policy");
            _consentCheckbox?.CreateAccessibilityNode(h, "Pilot Study Consent Form");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _usernameField?.DestroyAccessibilityNode();
            _emailField?.DestroyAccessibilityNode();
            _passwordField?.DestroyAccessibilityNode();
            _yearOfBirthDropdown?.DestroyAccessibilityNode();
            _countryDropdown?.DestroyAccessibilityNode();
            _regionDropdown?.DestroyAccessibilityNode();
            _postalCodeField?.DestroyAccessibilityNode();
            _termsCheckbox?.DestroyAccessibilityNode();
            _privacyCheckbox?.DestroyAccessibilityNode();
            _consentCheckbox?.DestroyAccessibilityNode();

            base.TeardownAccessibilityNodes();
        }
    }
}
