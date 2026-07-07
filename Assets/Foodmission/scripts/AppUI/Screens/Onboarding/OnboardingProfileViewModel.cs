using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class OnboardingProfileViewModel : ViewModelBase
    {
        private readonly ICatalogService _catalogService;
        private readonly IAuthService _authService;

        // Catalog data (source of truth for code lookup)
        private CatalogData _catalogData;

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

        
        /// <summary>
        /// Event to show an error toast.
        /// </summary>
        public event System.Action<string> ShowErrorRequest;

        public OnboardingProfileViewModel(IStoreService storeService, ICatalogService catalogService, IAuthService authService)
            : base(storeService)
        {
            _catalogService = catalogService;
            _authService = authService;
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
                string lang = state.lang ?? "es";

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

                
                Debug.Log($"[OnboardingProfileViewModel] Catalog loaded: {GenderOptions.Count} genders, {ActivityLevelOptions.Count} activity levels");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingProfileViewModel] LoadCatalogDataAsync exception: {ex.Message}");
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
        /// Call this after LoadCatalogDataAsync() so _catalogData is available.
        /// </summary>
        public void PrePopulateFromState()
        {
            if (_catalogData == null) return;

            AppState state = _storeService.GetAppState();

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

        public void Skip()
        {
            RaiseNavigationRequested(Actions.go_to_home);
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

                var request = new ProfileUpdateRequest
                {
                    gender = _selectedGenderIndex >= 0 ? _catalogData.genders[_selectedGenderIndex].code : null,
                    activityLevel = _selectedActivityLevelIndex >= 0 ? _catalogData.activityLevels[_selectedActivityLevelIndex].code : null,
                    educationLevel = _selectedEducationLevelIndex >= 0 ? _catalogData.educationLevels[_selectedEducationLevelIndex].code : null,
                    annualIncome = _selectedAnnualIncomeIndex >= 0 ? _catalogData.annualIncomeLevels[_selectedAnnualIncomeIndex].code : null,
                    
                    preferences = (shoppingResponsibilityCode != null || dietaryCodes != null)
                        ? new ProfileUpdatePreferences
                        {
                            shoppingResponsibility = shoppingResponsibilityCode,
                            dietaryPreference = dietaryCodes
                        }
                        : null
                };

                Debug.Log($"[OnboardingProfileViewModel] Submitting profile update: {request.ToJson()}");

                bool success = await _authService.UpdateProfileAsync(request);

                if (success)
                {
                    // Mark extended profile as completed in Redux
                    _storeService.store.Dispatch(AppActions.setExtendedProfile.Invoke());
                    // When ready, uncomment to navigate to avatar editor with onboarding flag:
                    //RaiseNavigationRequested(Actions.go_to_avatar_editor, new Unity.AppUI.Navigation.Argument("fromOnboarding", "true"));
                    RaiseNavigationRequested(Actions.go_to_home);
                }
                else
                {
                    ErrorMessage = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SAVE_PROFILE");
                    ShowErrorRequest?.Invoke(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingProfileViewModel] SubmitAsync exception: {ex.Message}");
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