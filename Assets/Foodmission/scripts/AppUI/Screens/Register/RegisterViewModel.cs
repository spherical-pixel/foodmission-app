using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class RegisterViewModel : StepFlowViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ICatalogService _catalogService;

        // Country/region data loaded from backend catalog (with JSON fallback)
        private List<CatalogItem> _countries = new();
        private List<CatalogItem> _regions = new();

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _usernameHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _usernameHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private string _email = "";

        [ObservableProperty]
        private string _emailHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _emailHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _passwordHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _passwordHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private IList<string> _yearOfBirthOptions = new List<string>();

        [ObservableProperty]
        private int _selectedYearOfBirthIndex = -1;

        [ObservableProperty]
        private string _yearOfBirthHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _yearOfBirthHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private int _selectedCountryIndex = -1;

        [ObservableProperty]
        private string _countryHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _countryHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private int _selectedRegionIndex = -1;

        [ObservableProperty]
        private string _regionHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _regionHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private string _postalCode = "";

        [ObservableProperty]
        private string _postalCodeHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _postalCodeHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private CheckboxState _hasAcceptedTerms = CheckboxState.Unchecked;

        [ObservableProperty]
        private string _termsHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _termsHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private CheckboxState _hasAcceptedPrivacyPolicy = CheckboxState.Unchecked;

        [ObservableProperty]
        private string _privacyHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _privacyHelpTextVariant = HelpTextVariant.Default;

        [ObservableProperty]
        private CheckboxState _hasAcceptedPilotConsent = CheckboxState.Unchecked;

        [ObservableProperty]
        private string _consentHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _consentHelpTextVariant = HelpTextVariant.Default;

        /// <summary>
        /// Country dropdown options list (format: "🇦🇹 Austria")
        /// </summary>
        public List<string> CountryOptions { get; private set; } = new();

        /// <summary>
        /// Region options list for the selected country
        /// </summary>
        public List<string> RegionOptions { get; private set; } = new();

        public event System.Action<string> ShowErrorRequest;

        private bool _registrationCompleted = false;

        public RegisterViewModel(IAuthService authService, ICatalogService catalogService, IStoreService storeService) : base(storeService)
        {
            _authService = authService;
            _catalogService = catalogService;

            BuildYearOfBirthOptions();

            _storeSubscription = _store.Subscribe(
                SelectAuthState,
                OnAuthStateChanged
            );
        }

        private void BuildYearOfBirthOptions()
        {
            var maxYear = DateTime.Now.Year - 18;
            var minYear = DateTime.Now.Year - 100;
            YearOfBirthOptions = Enumerable.Range(0, maxYear - minYear + 1)
                .Select(i => (maxYear - i).ToString())
                .ToList();
        }

        /// <summary>
        /// Loads country data from the backend catalog (with local JSON fallback).
        /// Call this from the Screen's OnEnter.
        /// </summary>
        public async Task LoadCountriesAsync()
        {
            string lang = _storeService.GetAppState().lang ?? "en";
            var (countries, _) = await _catalogService.GetCountriesAsync(lang);

            if (countries == null || countries.Count == 0)
            {
                Debug.LogError($"[{GetType().Name}] LoadCountriesAsync — no countries loaded");
                return;
            }

            _countries = countries;
            CountryOptions = _countries
                .Select(c => $"{CountryUtils.CountryCodeToFlag(c.code)} {c.label}")
                .ToList();
        }

        /// <summary>
        /// Updates available regions based on the selected country (async — fetches from backend).
        /// </summary>
        public async Task UpdateRegionsForSelectedCountryAsync()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countries.Count)
            {
                _regions = new List<CatalogItem>();
                RegionOptions = new List<string>();
                SelectedRegionIndex = -1;
                return;
            }

            string countryCode = _countries[SelectedCountryIndex].code;
            string lang = _storeService.GetAppState().lang ?? "en";
            var (regions, _) = await _catalogService.GetRegionsAsync(countryCode, lang);

            _regions = regions ?? new List<CatalogItem>();
            RegionOptions = _regions.Select(r => r.label).ToList();
            SelectedRegionIndex = RegionOptions.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// Gets the ISO code of the selected country
        /// </summary>
        public string GetSelectedCountryIso()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countries.Count)
                return null;
            return _countries[SelectedCountryIndex].code;
        }

        /// <summary>
        /// Gets the ISO code of the selected region
        /// </summary>
        public string GetSelectedRegionIso()
        {
            if (SelectedRegionIndex < 0 || SelectedRegionIndex >= _regions.Count)
                return null;
            return _regions[SelectedRegionIndex].code;
        }

        private (bool isAuthenticating, string authError, string userId) SelectAuthState(AppState state)
        {
            return (state.isAuthenticating, state.authError, state.userId);
        }

        private void OnAuthStateChanged((bool isAuthenticating, string authError, string userId) authState)
        {
            if (!string.IsNullOrEmpty(authState.authError))
            {
                Debug.LogError($"[{GetType().Name}] - OnAuthStateChanged -> authError = {authState.authError}");
                ShowErrorRequest?.Invoke(authState.authError);
            }
            else if (!string.IsNullOrEmpty(authState.userId) && !_registrationCompleted)
            {
                Debug.Log($"[{GetType().Name}] - OnAuthStateChanged -> userId = {authState.userId}, registration completed -> " + JsonUtility.ToJson(authState));
                _registrationCompleted = true;
                // NutriMessageDialog.Show(
                //     message: "@UI:COMPLETE_WELCOME_MESSAGE",
                //     actions: new[]
                //     {
                //         new FMDialogAction("@UI:BTN_CREATE_AVATAR", () => {}, isPrimary: true),
                //         new FMDialogAction("@UI:BTN_COMPLETE_PROFILE", () =>
                //         {
                //             RaiseNavigationRequested(Actions.register_to_onboarding);
                //         }, isPrimary: true),
                //         new FMDialogAction("@UI:BTN_ENTER_APP", () =>
                //         {
                //             RaiseNavigationRequested(Actions.loading_to_home);
                //         }, isPrimary: true)
                //     }
                // );           

                // Check if extended profile is needed
                // AppState state = _storeService.GetAppState();
                // if (!state.hasCompletedExtendedProfile)
                // {
                //     Debug.Log($"[{GetType().Name}] - OnAuthStateChanged -> userId = {authState.userId}, navigating to onboarding profile");
                //     RaiseNavigationRequested(Actions.register_to_onboarding);
                // }
                // else
                // {
                //     Debug.Log($"[{GetType().Name}] - OnAuthStateChanged -> userId = {authState.userId}, navigating to home");
                //     RaiseNavigationRequested(Actions.loading_to_home);
                // }
                //RaiseNavigationRequested(Actions.loading_to_home);
            }
        }

        public async void Register()
        {
            bool fieldsOk = true;

            if (!ValidateUsername())
            {
                fieldsOk = false;
            }

            if (!ValidatePassword())
            {
                fieldsOk = false;
            }

            if (!ValidateEmail())
            {
                fieldsOk = false;
            }

            if (!ValidateYearOfBirth())
            {
                fieldsOk = false;
            }

            if (!ValidateCountry())
            {
                fieldsOk = false;
            }

            if (!ValidateRegion())
            {
                fieldsOk = false;
            }

            if (!ValidateTerms())
            {
                fieldsOk = false;
            }

            if (!ValidatePrivacyPolicy())
            {
                fieldsOk = false;
            }

            if (!ValidatePilotConsent())
            {
                fieldsOk = false;
            }

            if (fieldsOk)
            {
                // Get country and region ISO codes
                string countryIso = GetSelectedCountryIso();
                string regionIso = GetSelectedRegionIso();

                // Call RegisterAsync with optional fields
                var result = await _authService.RegisterAsync(
                    Username,
                    Email,
                    Password,
                    SelectedYearOfBirthIndex >= 0 ? int.Parse(YearOfBirthOptions[SelectedYearOfBirthIndex]) : 0,
                    country: !string.IsNullOrEmpty(countryIso) ? countryIso : null,
                    region: !string.IsNullOrEmpty(regionIso) ? regionIso : null,
                    zip: !string.IsNullOrEmpty(PostalCode) ? PostalCode : null
                );

                if (result.success)
                {
                    // Registration and auto-login successful - navigation handled by auth state change
                    Debug.Log($"[RegisterViewModel] Registration completed successfully for user: {result.userId}");

                    string lang = _storeService.GetAppState().lang ?? "en";
                    _ = Components.FMQuantityUnitPanel.InitializeAsync(_catalogService, lang);

                    NutriMessageDialog.Show(
                        message: "@UI:COMPLETE_WELCOME_MESSAGE",
                        actions: new[]
                        {
                            new FMDialogAction("@UI:BTN_CREATE_AVATAR", () =>
                            {
                                RaiseNavigationRequested(Actions.go_to_avatar_editor, new Unity.AppUI.Navigation.Argument("fromOnboarding", "true"));
                            }, isPrimary: true),
                            new FMDialogAction("@UI:BTN_COMPLETE_PROFILE", () =>
                            {
                                RaiseNavigationRequested(Actions.register_to_onboarding);
                            }, isPrimary: true),
                            new FMDialogAction("@UI:BTN_ENTER_APP", () =>
                            {
                                RaiseNavigationRequested(Actions.loading_to_home);
                            }, isPrimary: true)
                        }
                    );           


                }
                else
                {
                    ShowErrorRequest?.Invoke(result.error);
                }
            }
            else
            {
                ShowErrorRequest?.Invoke("@UI:ERROR_FIELDS_VALIDATION");
            }
        }

        public bool ValidateUsername(bool showError = true)
        {
            if (string.IsNullOrEmpty(Username))
            {
                if (showError)
                {
                    UsernameHelpTextValue = "@UI:ERROR_NO_EMPTY";
                    UsernameHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    UsernameHelpTextValue = string.Empty;
                    UsernameHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            UsernameHelpTextValue = string.Empty;
            UsernameHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateEmail(bool showError = true)
        {
            if (string.IsNullOrEmpty(Email))
            {
                if (showError)
                {
                    EmailHelpTextValue = "@UI:ERROR_NO_EMPTY";
                    EmailHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    EmailHelpTextValue = string.Empty;
                    EmailHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            if (!Email.Contains("@") || !Email.Contains("."))
            {
                EmailHelpTextValue = "@UI:ERROR_EMAIL_INVALID";
                EmailHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            EmailHelpTextValue = string.Empty;
            EmailHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidatePassword(bool showError = true)
        {
            if (string.IsNullOrEmpty(Password))
            {
                if (showError)
                {
                    PasswordHelpTextValue = "@UI:ERROR_NO_EMPTY";
                    PasswordHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    PasswordHelpTextValue = string.Empty;
                    PasswordHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            if (Password.Length < 6)
            {
                PasswordHelpTextValue = "@UI:ERROR_PASS_SHORT";
                PasswordHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            PasswordHelpTextValue = string.Empty;
            PasswordHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateYearOfBirth(bool showError = true)
        {
            if (SelectedYearOfBirthIndex < 0)
            {
                YearOfBirthHelpTextValue = string.Empty;
                YearOfBirthHelpTextVariant = HelpTextVariant.Default;
                return true;
            }

            if (SelectedYearOfBirthIndex >= YearOfBirthOptions.Count)
            {
                if (showError)
                {
                    YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_1";
                    YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    YearOfBirthHelpTextValue = string.Empty;
                    YearOfBirthHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            int year = int.Parse(YearOfBirthOptions[SelectedYearOfBirthIndex]);

            if (year < DateTime.Now.Year - 100)
            {
                if (showError)
                {
                    YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_1";
                    YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    YearOfBirthHelpTextValue = string.Empty;
                    YearOfBirthHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            if (year > DateTime.Now.Year - 18)
            {
                if (showError)
                {
                    YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_2";
                    YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    YearOfBirthHelpTextValue = string.Empty;
                    YearOfBirthHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            YearOfBirthHelpTextValue = string.Empty;
            YearOfBirthHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateCountry(bool showError = true)
        {
            if (_selectedCountryIndex == -1)
            {
                if (showError)
                {
                    CountryHelpTextValue = "@UI:ERROR_COUNTRY_SELECT";
                    CountryHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    CountryHelpTextValue = string.Empty;
                    CountryHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            CountryHelpTextValue = string.Empty;
            CountryHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateRegion(bool showError = true)
        {
            if (_selectedRegionIndex == -1)
            {
                if (showError)
                {
                    RegionHelpTextValue = "@UI:ERROR_REGION_SELECT";
                    RegionHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    RegionHelpTextValue = string.Empty;
                    RegionHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            RegionHelpTextValue = string.Empty;
            RegionHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        /// <summary>
        /// Validates the postal code field. Since postal code formats vary widely by country,
        /// this only checks length constraints when a value is provided.
        /// </summary>
        public bool ValidatePostalCode(bool showError = true)
        {
            // Postal code is optional - only validate if a value is provided
            if (string.IsNullOrEmpty(PostalCode))
            {
                PostalCodeHelpTextValue = string.Empty;
                PostalCodeHelpTextVariant = HelpTextVariant.Default;
                return true;
            }

            if (PostalCode.Length < 2 || PostalCode.Length > 10)
            {
                PostalCodeHelpTextValue = "@UI:ERROR_PC_FORMAT";
                PostalCodeHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            PostalCodeHelpTextValue = string.Empty;
            PostalCodeHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateTerms(bool showError = true)
        {
            if (HasAcceptedTerms != CheckboxState.Checked)
            {
                if (showError)
                {
                    TermsHelpTextValue = "@UI:ACCEPT_TERMS";
                    TermsHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    TermsHelpTextValue = string.Empty;
                    TermsHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            TermsHelpTextValue = string.Empty;
            TermsHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidatePrivacyPolicy(bool showError = true)
        {
            if (HasAcceptedPrivacyPolicy != CheckboxState.Checked)
            {
                if (showError)
                {
                    PrivacyHelpTextValue = "@UI:ACCEPT_PRIVACY";
                    PrivacyHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    PrivacyHelpTextValue = string.Empty;
                    PrivacyHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            PrivacyHelpTextValue = string.Empty;
            PrivacyHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidatePilotConsent(bool showError = true)
        {
            if (HasAcceptedPilotConsent != CheckboxState.Checked)
            {
                if (showError)
                {
                    ConsentHelpTextValue = "@UI:ACCEPT_PILOT_CONSENT";
                    ConsentHelpTextVariant = HelpTextVariant.Destructive;
                }
                else
                {
                    ConsentHelpTextValue = string.Empty;
                    ConsentHelpTextVariant = HelpTextVariant.Default;
                }
                return false;
            }

            ConsentHelpTextValue = string.Empty;
            ConsentHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        // ── StepFlow Implementation ─────────────────────────────────
        protected override int GetStepCount() => 9;

        protected override bool ValidateStep(int stepIndex)
        {
            return ValidateStep(stepIndex, showError: false);
        }

        protected override bool ValidateStep(int stepIndex, bool showError)
        {
            return stepIndex switch
            {
                0 => true, // Welcome step is always valid
                1 => ValidateUsername(showError),
                2 => ValidateEmail(showError),
                3 => ValidatePassword(showError),
                4 => ValidateYearOfBirth(showError),
                5 => ValidateCountry(showError) && ValidateRegion(showError) && ValidatePostalCode(showError),
                6 => ValidateTerms(showError),
                7 => ValidatePrivacyPolicy(showError),
                8 => ValidatePilotConsent(showError),
                _ => true
            };
        }

        protected override string GetStepTitle(int stepIndex)
        {
            return stepIndex switch
            {
                0 => "@UI:STEP_TITLE_WELCOME",
                1 => "@UI:STEP_TITLE_USERNAME",
                2 => "@UI:STEP_TITLE_EMAIL",
                3 => "@UI:STEP_TITLE_PASSWORD",
                4 => "@UI:STEP_TITLE_BIRTHYEAR",
                5 => "@UI:STEP_TITLE_LOCATION",
                6 => "@UI:STEP_TITLE_TERMS",
                7 => "@UI:STEP_TITLE_PRIVACY",
                8 => "@UI:STEP_TITLE_CONSENT",
                _ => ""
            };
        }

        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;

        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;

        protected override Task OnFlowCompletedAsync()
        {
            Register();
            return Task.CompletedTask;
        }
    }
}
