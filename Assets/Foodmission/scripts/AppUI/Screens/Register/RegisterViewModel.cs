using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        // Country data loaded from JSON
        private List<CountryData> _countriesData = new();

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
        private int _yearOfBirth = 0;

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

        /// <summary>
        /// Country dropdown options list (format: "🇦🇹 Austria")
        /// </summary>
        public List<string> CountryOptions { get; private set; } = new();

        /// <summary>
        /// Region options list for the selected country
        /// </summary>
        public List<string> RegionOptions { get; private set; } = new();

        public event System.Action<string> ShowErrorRequest;

        public RegisterViewModel(IAuthService authService, IStoreService storeService) : base(storeService)
        {
            _authService = authService;

            LoadCountriesData();

            _storeSubscription = _store.Subscribe(
                SelectAuthState,
                OnAuthStateChanged
            );
        }

        /// <summary>
        /// Loads country data from the JSON in Resources
        /// </summary>
        private void LoadCountriesData()
        {
            var jsonAsset = Resources.Load<TextAsset>("ue_countries_regions");
            if (jsonAsset == null)
            {
                Debug.LogError($"[{GetType().Name}] - LoadCountriesData - ue_countries_regions.json could not be loaded");
                return;
            }

            var wrapper = JsonUtility.FromJson<CountriesList>("{\"countries\":" + jsonAsset.text + "}");
            if (wrapper?.countries == null)
            {
                Debug.LogError($"[{GetType().Name}] - LoadCountriesData - Error parsing countries JSON");
                return;
            }

            _countriesData = wrapper.countries;

            CountryOptions = _countriesData.Select(c => $"{c.flag} {c.country_name_local}").ToList();
        }

        /// <summary>
        /// Updates available regions based on the selected country
        /// </summary>
        public void UpdateRegionsForSelectedCountry()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countriesData.Count)
            {
                RegionOptions = new List<string>();
                SelectedRegionIndex = -1;
                return;
            }

            var country = _countriesData[SelectedCountryIndex];
            RegionOptions = country.regions?
                .Select(r => r.region_name_local)
                .ToList() ?? new List<string>();

            SelectedRegionIndex = RegionOptions.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// Gets the ISO code of the selected country
        /// </summary>
        public string GetSelectedCountryIso()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countriesData.Count)
                return null;
            return _countriesData[SelectedCountryIndex].country_iso;
        }

        /// <summary>
        /// Gets the ISO code of the selected region
        /// </summary>
        public string GetSelectedRegionIso()
        {
            if (SelectedCountryIndex < 0 || SelectedCountryIndex >= _countriesData.Count)
                return null;

            var country = _countriesData[SelectedCountryIndex];
            if (SelectedRegionIndex < 0 || SelectedRegionIndex >= country.regions?.Count)
                return null;

            return country.regions[SelectedRegionIndex].region_iso;
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
            else if (!string.IsNullOrEmpty(authState.userId))
            {
                NutriMessageDialog.Show(
                    message: "@UI:COMPLETE_WELCOME_MESSAGE",
                    actions: new[]
                    {
                        new FMDialogAction("@UI:BTN_CREATE_AVATAR", () => {}, isPrimary: true),
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
                    YearOfBirth,
                    country: !string.IsNullOrEmpty(countryIso) ? countryIso : null,
                    region: !string.IsNullOrEmpty(regionIso) ? regionIso : null,
                    zip: !string.IsNullOrEmpty(PostalCode) ? PostalCode : null
                );

                if (result.success)
                {
                    // Registration and auto-login successful - navigation handled by auth state change
                    Debug.Log($"[RegisterViewModel] Registration completed successfully for user: {result.userId}");
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

        public bool ValidateUsername()
        {
            if (string.IsNullOrEmpty(Username))
            {
                UsernameHelpTextValue = "@UI:ERROR_NO_EMPTY";
                UsernameHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            UsernameHelpTextValue = string.Empty;
            UsernameHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateEmail()
        {
            if (string.IsNullOrEmpty(Email))
            {
                EmailHelpTextValue = "@UI:ERROR_NO_EMPTY";
                EmailHelpTextVariant = HelpTextVariant.Destructive;
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

        public bool ValidatePassword()
        {
            if (string.IsNullOrEmpty(Password))
            {
                PasswordHelpTextValue = "@UI:ERROR_NO_EMPTY";
                PasswordHelpTextVariant = HelpTextVariant.Destructive;
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

        public bool ValidateYearOfBirth()
        {
            if (YearOfBirth != 0 && YearOfBirth < DateTime.Now.Year - 100)
            {
                YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_1";
                YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            if (YearOfBirth != 0 && YearOfBirth > DateTime.Now.Year - 18)
            {
                YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_2";
                YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            YearOfBirthHelpTextValue = string.Empty;
            YearOfBirthHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateCountry()
        {
            if (_selectedCountryIndex == -1)
            {
                CountryHelpTextValue = "@UI:ERROR_COUNTRY_SELECT";
                CountryHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            CountryHelpTextValue = string.Empty;
            CountryHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        public bool ValidateRegion()
        {
            if (_selectedRegionIndex == -1)
            {
                RegionHelpTextValue = "@UI:ERROR_REGION_SELECT";
                RegionHelpTextVariant = HelpTextVariant.Destructive;
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
        public bool ValidatePostalCode()
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

        public bool ValidateTerms()
        {
            if (HasAcceptedTerms != CheckboxState.Checked)
            {
                TermsHelpTextValue = "You must accept the Terms and Conditions";
                TermsHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            TermsHelpTextValue = string.Empty;
            TermsHelpTextVariant = HelpTextVariant.Default;
            return true;
        }
    }
}
