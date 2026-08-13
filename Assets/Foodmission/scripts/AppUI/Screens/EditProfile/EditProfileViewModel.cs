using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class EditProfileViewModel : ViewModelBase
    {
        private readonly ICatalogService _catalogService;
        private readonly IAuthService _authService;

        // Catalog data (source of truth for code lookup)
        private CatalogData _catalogData;

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

        // Dropdown source items (display labels)
        [ObservableProperty]
        private IList<string> _genderOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _activityLevelOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _educationLevelOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _annualIncomeOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _shoppingResponsibilityOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _dietaryPreferenceOptions = new List<string>();


        // Selected indices for dropdowns (-1 = no selection)
        [ObservableProperty]
        private int _selectedGenderIndex = -1;

        [ObservableProperty]
        private int _selectedActivityLevelIndex = -1;
        
        [ObservableProperty]
        private int[] _selectedDietaryPreferenceIndices = new int[] { };

        [ObservableProperty]
        private int _selectedEducationLevelIndex = -1;

        [ObservableProperty]
        private int _selectedAnnualIncomeIndex = -1;

        [ObservableProperty]
        private int _selectedShoppingResponsibilityIndex = -1;

        // UI state
        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isSubmitting = false;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private string _loadingText = "";

        /// <summary>
        /// Country dropdown options list (format: "🇦🇹 Austria")
        /// </summary>
        public List<string> CountryOptions { get; private set; } = new();

        /// <summary>
        /// Region options list for the selected country
        /// </summary>
        public List<string> RegionOptions { get; private set; } = new();

        // Country/region data loaded from backend catalog (with JSON fallback)
        private List<CatalogItem> _countries = new();
        private List<CatalogItem> _regions = new();

        /// <summary>
        /// Whether all required fields have a value.
        /// Required: gender, activityLevel, educationLevel, annualIncome, shoppingResponsibility
        /// </summary>
        public bool IsFormValid =>
            _selectedGenderIndex >= 0 ||
            _selectedActivityLevelIndex >= 0 ||
            _selectedEducationLevelIndex >= 0 ||
            _selectedAnnualIncomeIndex >= 0 ||
            _selectedShoppingResponsibilityIndex >= 0 || 
            (_selectedDietaryPreferenceIndices != null);

        [ObservableProperty]
        private ApiErrorResponse m_ErrorDetail;

        /// <summary>
        /// Event to show an error toast.
        /// </summary>
        public event System.Action<string> ShowErrorRequest;

        public EditProfileViewModel(IStoreService storeService, ICatalogService catalogService, IAuthService authService): base(storeService)
        {
            _catalogService = catalogService;
            _authService = authService;

            BuildYearOfBirthOptions();
        }

        private void BuildYearOfBirthOptions()
        {
            var maxYear = DateTime.Now.Year - 18;
            var minYear = DateTime.Now.Year - 100;
            YearOfBirthOptions = Enumerable.Range(0, maxYear - minYear + 1)
                .Select(i => (maxYear - i).ToString())
                .ToList();
        }

        public bool ValidateYearOfBirth()
        {
            if (SelectedYearOfBirthIndex < 0)
            {
                YearOfBirthHelpTextValue = string.Empty;
                YearOfBirthHelpTextVariant = HelpTextVariant.Default;
                return true;
            }

            if (SelectedYearOfBirthIndex >= YearOfBirthOptions.Count)
            {
                YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_1";
                YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            int year = int.Parse(YearOfBirthOptions[SelectedYearOfBirthIndex]);

            if (year < DateTime.Now.Year - 100)
            {
                YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_1";
                YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            if (year > DateTime.Now.Year - 18)
            {
                YearOfBirthHelpTextValue = "@UI:ERROR_BIRTH_2";
                YearOfBirthHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            YearOfBirthHelpTextValue = string.Empty;
            YearOfBirthHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        /// <summary>
        /// Loads country data from the backend catalog (with local JSON fallback).
        /// Call this from the Screen's OnEnter before PrePopulateFromState.
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

        /// <summary>
        /// Loads catalog data and populates dropdown options.
        /// Call this from the Screen's OnEnter after ViewModel is bound.
        /// </summary>
        public async Task LoadCatalogDataAsync()
        {
            IsLoading = true;
            LoadingText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOADING_DATA");

            try
            {
                AppState state = _storeService.GetAppState();
                string lang = state.lang ?? "en";

                var (data, _) = await _catalogService.LoadStartupAsync(lang);

                if (data == null)
                {
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_LOAD_DATA");
                    ShowErrorRequest?.Invoke(ErrorMessage);
                    IsLoading = false;
                    return;
                }

                _catalogData = data;

                // Populate dropdown options from catalog labels
                GenderOptions = data.genders?.Select(g => g.label).ToList() ?? new List<string>();
                ActivityLevelOptions = data.activityLevels?.Select(a => a.label).ToList() ?? new List<string>();
                EducationLevelOptions = data.educationLevels?.Select(e => e.label).ToList() ?? new List<string>();
                AnnualIncomeOptions = data.annualIncomeLevels?.Select(i => i.label).ToList() ?? new List<string>();
                ShoppingResponsibilityOptions = data.shoppingResponsibilities?.Select(s => s.label).ToList() ?? new List<string>();
                DietaryPreferenceOptions = data.dietaryPreferences?.Select(d => d.label).ToList() ?? new List<string>();

                
                Debug.Log($"[EditProfileViewModel] Catalog loaded: {GenderOptions.Count} genders, {ActivityLevelOptions.Count} activity levels");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditProfileViewModel] LoadCatalogDataAsync exception: {ex.Message}");
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_LOADING_DATA");
                ShowErrorRequest?.Invoke(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Pre-populates dropdown indices from the current AppState profile data.
        /// Call this after LoadCatalogDataAsync() and LoadCountriesAsync() so data is available.
        /// </summary>
        public async Task PrePopulateFromState()
        {
            if (_catalogData == null) return;

            AppState state = _storeService.GetAppState();

            if (state.userYearOfBirth > 0)
            {
                SelectedYearOfBirthIndex = YearOfBirthOptions.IndexOf(state.userYearOfBirth.ToString());
            }
            SelectedCountryIndex = _countries.FindIndex(c => c.code == state.userCountry);
            await UpdateRegionsForSelectedCountryAsync();
            if (SelectedCountryIndex >= 0 && SelectedRegionIndex >= 0)
            {
                SelectedRegionIndex = _regions.FindIndex(r => r.code == state.userRegion);
            }

            PostalCode = state.userZip;

            SelectedGenderIndex = FindCatalogIndex(_catalogData.genders, state.userGender);
            SelectedActivityLevelIndex = FindCatalogIndex(_catalogData.activityLevels, state.userActivityLevel);
            SelectedEducationLevelIndex = FindCatalogIndex(_catalogData.educationLevels, state.userEducationLevel);
            SelectedAnnualIncomeIndex = FindCatalogIndex(_catalogData.annualIncomeLevels, state.userAnnualIncome);
            SelectedShoppingResponsibilityIndex = FindCatalogIndex(_catalogData.shoppingResponsibilities, state.userShoppingResponsibility);

            var dietaryIndices = new List<int>();
            if (state.userDietaryPreference != null)
            {
                foreach (string code in state.userDietaryPreference)
                {
                    int idx = FindCatalogIndex(_catalogData.dietaryPreferences, code);
                    if (idx >= 0) dietaryIndices.Add(idx);
                }
            }
            SelectedDietaryPreferenceIndices = dietaryIndices.ToArray();
        }

        private static int FindCatalogIndex(CatalogItem[] items, string code)
        {
            if (items == null || string.IsNullOrEmpty(code)) return -1;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].code == code) return i;
            }
            return -1;
        }


        
        /// <summary>
        /// Submits the profile data to the backend.
        /// </summary>
        public async Task SubmitAsync()
        {
            if (!IsFormValid)
            {
                return;
            }

            IsSubmitting = true;
            ErrorMessage = "";

            try
            {
                string shoppingResponsibilityCode = _selectedShoppingResponsibilityIndex >= 0
                    ? _catalogData.shoppingResponsibilities[_selectedShoppingResponsibilityIndex].code
                    : null;

                // Build array of all selected dietary preference codes
                string[] dietaryCodes = null;
                if (_selectedDietaryPreferenceIndices != null && _selectedDietaryPreferenceIndices.Length > 0
                    && _catalogData.dietaryPreferences != null)
                {
                    var codes = new List<string>();
                    foreach (int idx in _selectedDietaryPreferenceIndices)
                    {
                        if (idx >= 0 && idx < _catalogData.dietaryPreferences.Length)
                        {
                            codes.Add(_catalogData.dietaryPreferences[idx].code);
                        }
                    }
                    if (codes.Count > 0) dietaryCodes = codes.ToArray();
                }

                AppState state = _storeService.GetAppState();

                bool hasShopping = shoppingResponsibilityCode != null || !string.IsNullOrEmpty(state.userShoppingResponsibility);
                bool hasDietary = (dietaryCodes != null && dietaryCodes.Length > 0)
                    || (state.userDietaryPreference != null && state.userDietaryPreference.Length > 0);
                bool hasSurvey = state.userOnboardingSurvey != null && state.userOnboardingSurvey.HasAnswers();

                var request = new ProfileUpdateRequest
                {
                    gender = _selectedGenderIndex >= 0 ? _catalogData.genders[_selectedGenderIndex].code : null,
                    activityLevel = _selectedActivityLevelIndex >= 0 ? _catalogData.activityLevels[_selectedActivityLevelIndex].code : null,
                    educationLevel = _selectedEducationLevelIndex >= 0 ? _catalogData.educationLevels[_selectedEducationLevelIndex].code : null,
                    annualIncome = _selectedAnnualIncomeIndex >= 0 ? _catalogData.annualIncomeLevels[_selectedAnnualIncomeIndex].code : null,
                    yearOfBirth = SelectedYearOfBirthIndex >= 0 ? (int?)int.Parse(YearOfBirthOptions[SelectedYearOfBirthIndex]) : null,
                    
                    preferences = (hasShopping || hasDietary || hasSurvey)
                        ? new ProfileUpdatePreferences
                        {
                            shoppingResponsibility = shoppingResponsibilityCode ?? state.userShoppingResponsibility,
                            dietaryPreference = dietaryCodes ?? state.userDietaryPreference,
                            onboardingSurvey = hasSurvey ? state.userOnboardingSurvey : null,
                            autoAddToPantry = state.userAutoAddToPantry
                        }
                        : null
                };
                

                if( SelectedGenderIndex >= 0 && _catalogData.genders[SelectedGenderIndex].code != state.userGender)
                {
                    request.gender = _catalogData.genders[SelectedGenderIndex].code;
                }

                if(SelectedActivityLevelIndex >= 0 && _catalogData.activityLevels[SelectedActivityLevelIndex].code != state.userActivityLevel)
                {
                    request.activityLevel = _catalogData.activityLevels[SelectedActivityLevelIndex].code;
                }

                if(SelectedEducationLevelIndex >= 0 && _catalogData.educationLevels[SelectedEducationLevelIndex].code != state.userEducationLevel)
                {
                    request.educationLevel = _catalogData.educationLevels[SelectedEducationLevelIndex].code;
                }

                if(SelectedAnnualIncomeIndex >= 0 && _catalogData.annualIncomeLevels[SelectedAnnualIncomeIndex].code != state.userAnnualIncome)
                {
                    request.annualIncome = _catalogData.annualIncomeLevels[SelectedAnnualIncomeIndex].code;
                }

                int currentYear = SelectedYearOfBirthIndex >= 0 ? int.Parse(YearOfBirthOptions[SelectedYearOfBirthIndex]) : 0;
                if (currentYear > 0 && currentYear != state.userYearOfBirth)
                {
                    request.yearOfBirth = currentYear;
                }

                if( SelectedCountryIndex >= 0 && _countries[SelectedCountryIndex].code != state.userCountry)
                {
                    request.country = _countries[SelectedCountryIndex].code;
                }

                if(SelectedRegionIndex >= 0 && SelectedRegionIndex < _regions.Count && _regions[SelectedRegionIndex].code != state.userRegion)
                {
                    request.region = _regions[SelectedRegionIndex].code;
                }

                if( !string.IsNullOrEmpty(PostalCode) && PostalCode != state.userZip)
                {
                    request.zip = PostalCode;
                }

                Debug.Log($"[EditProfileViewModel] Submitting profile update: {request.ToJson()}");

                var (success, error) = await _authService.UpdateProfileAsync(request);

                if (success)
                {
                    // Mark extended profile as completed in Redux
                    //_storeService.store.Dispatch(AppActions.setExtendedProfile.Invoke());
                    //RaiseNavigationRequested(Actions.onboardingprofile_to_onboardingavatar);
                    //RaiseNavigationRequested(Actions.go_to_home);
                    ErrorDetail = null;
                }
                else
                {
                    ErrorDetail = error ?? new ApiErrorResponse { statusCode = 500, error = "COULD_NOT_SAVE_PROFILE", message = "Could not save profile" };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditProfileViewModel] SubmitAsync exception: {ex.Message}");
                ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNEXPECTED_ERROR");
                ShowErrorRequest?.Invoke(ErrorMessage);
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}