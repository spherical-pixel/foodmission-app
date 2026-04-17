using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using UnityEngine;

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

        // Dietary preference checkbox states (parallel to _dietaryPreferenceOptions)
        private List<bool> _dietaryPreferenceChecked = new List<bool>();

        // Selected indices for dropdowns (-1 = no selection)
        [ObservableProperty]
        private int _selectedGenderIndex = -1;

        [ObservableProperty]
        private int _selectedActivityLevelIndex = -1;

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
            _selectedShoppingResponsibilityIndex >= 0;

        
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
            LoadingText = "Cargando datos...";

            try
            {
                AppState state = _storeService.GetAppState();
                string lang = state.lang ?? "es";

                CatalogData data = await _catalogService.LoadStartupAsync(lang);

                if (data == null)
                {
                    ErrorMessage = "No se pudieron cargar los datos. Intenta de nuevo.";
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

                // Initialize checkbox states for dietary preferences
                _dietaryPreferenceChecked = new List<bool>(new bool[DietaryPreferenceOptions.Count]);

                Debug.Log($"[OnboardingProfileViewModel] Catalog loaded: {GenderOptions.Count} genders, {ActivityLevelOptions.Count} activity levels");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingProfileViewModel] LoadCatalogDataAsync exception: {ex.Message}");
                ErrorMessage = "Error al cargar los datos.";
                ShowErrorRequest?.Invoke(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Sets the checked state of a dietary preference at the given index.
        /// </summary>
        public void SetDietaryPreference(int index, bool isChecked)
        {
            if (index >= 0 && index < _dietaryPreferenceChecked.Count)
            {
                _dietaryPreferenceChecked[index] = isChecked;
            }
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
                // Map selected indices to catalog codes
                string genderCode =  _selectedGenderIndex >= 0 ? _catalogData.genders[_selectedGenderIndex].code : null;
                string activityLevelCode = _selectedActivityLevelIndex >= 0 ? _catalogData.activityLevels[_selectedActivityLevelIndex].code : null;
                string educationLevelCode = _selectedEducationLevelIndex >= 0 ? _catalogData.educationLevels[_selectedEducationLevelIndex].code : null;
                string annualIncomeCode = _selectedAnnualIncomeIndex >= 0 ? _catalogData.annualIncomeLevels[_selectedAnnualIncomeIndex].code : null;
                string shoppingResponsibilityCode = _selectedShoppingResponsibilityIndex >= 0 ? _catalogData.shoppingResponsibilities[_selectedShoppingResponsibilityIndex].code : null;

                // Build dietary preference — pick first selected preference
                string dietaryPreferenceCode = "";
                for (int i = 0; i < _dietaryPreferenceChecked.Count; i++)
                {
                    if (_dietaryPreferenceChecked[i])
                    {
                        dietaryPreferenceCode = _catalogData.dietaryPreferences[i].code;
                        break;
                    }
                }

                var request = new ProfileUpdateRequest();
                if( genderCode != null)
                {
                    request.gender = genderCode;
                }
                if( activityLevelCode != null)
                {
                    request.activityLevel = activityLevelCode;
                }
                if( educationLevelCode != null)                {
                    request.educationLevel = educationLevelCode;
                }
                if( annualIncomeCode != null)                {
                    request.annualIncome = annualIncomeCode;
                }
                if( shoppingResponsibilityCode != null)                {
                    if( request.preferences == null)
                    {
                        request.preferences = new ProfileUpdatePreferences();
                    }
                    request.preferences.shoppingResponsibility = shoppingResponsibilityCode;
                }
                
                if( dietaryPreferenceCode != null)                {
                    if( request.preferences == null)
                    {
                        request.preferences = new ProfileUpdatePreferences();
                    }
                    request.preferences.dietaryPreference = dietaryPreferenceCode;
                }
                
                /*{
                    
                    
                    
                    
                    preferences = new ProfileUpdatePreferences
                    {
                        dietaryPreference = dietaryPreferenceCode,
                        shoppingResponsibility = shoppingResponsibilityCode
                    }
                };*/

                bool success = await _authService.UpdateProfileAsync(request);

                if (success)
                {
                    // Mark extended profile as completed in Redux
                    _storeService.store.Dispatch(AppActions.setExtendedProfile.Invoke());
                    RaiseNavigationRequested(Actions.onboardingprofile_to_onboardingavatar);
                }
                else
                {
                    ErrorMessage = "No se pudo guardar el perfil. Intenta de nuevo.";
                    ShowErrorRequest?.Invoke(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingProfileViewModel] SubmitAsync exception: {ex.Message}");
                ErrorMessage = "Error inesperado. Intenta de nuevo.";
                ShowErrorRequest?.Invoke(ErrorMessage);
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}