using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class OnboardingProfileViewModel : StepFlowViewModelBase
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

        [ObservableProperty]
        private IList<string> _motivationOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _dailyTimeCommitmentOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _segmentOptions = new List<string>();

        [ObservableProperty]
        private IList<string> _pushNotificationsOptions = new List<string>
        {
            "@UI:ONBOARDING_PROFILE.NOTIFICATIONS_OPT_YES",
            "@UI:ONBOARDING_PROFILE.NOTIFICATIONS_OPT_NO"
        };

        [ObservableProperty]
        private IList<string> _reminderTimeOptions = new List<string>
        {
            "07:00", "08:00", "09:00", "10:00", "11:00", "12:00",
            "13:00", "14:00", "15:00", "16:00", "17:00", "18:00",
            "19:00", "20:00", "21:00", "22:00"
        };

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

        [ObservableProperty]
        private int _selectedMotivationIndex = -1;

        [ObservableProperty]
        private int _selectedDailyTimeCommitmentIndex = -1;

        [ObservableProperty]
        private int _selectedSegmentIndex = -1;

        [ObservableProperty]
        private int _selectedPushNotificationsIndex = 0;

        [ObservableProperty]
        private int _selectedReminderTimeIndex = 3;

        // UI state
        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isSubmitting = false;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private string _loadingText = "";

        private readonly INotificationService _notificationService;

        /// <summary>
        /// Segment selection is required for form validity.
        /// </summary>
        public bool IsFormValid => _selectedSegmentIndex >= 0;

        /// <summary>
        /// Event to show an error toast.
        /// </summary>
        public event System.Action<string> ShowErrorRequest;

        public OnboardingProfileViewModel(
            IStoreService storeService,
            ICatalogService catalogService,
            IAuthService authService,
            INotificationService notificationService = null)
            : base(storeService)
        {
            _catalogService = catalogService;
            _authService = authService;
            _notificationService = notificationService;

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SelectedSegmentIndex) ||
                    args.PropertyName == nameof(SelectedPushNotificationsIndex) ||
                    args.PropertyName == nameof(SelectedReminderTimeIndex))
                {
                    InvalidateValidation();
                }
            };
        }

        // ── StepFlow Implementation ───────────────────────────

        protected override int GetStepCount() => 8;

        protected override bool ValidateStep(int stepIndex)
        {
            return ValidateStep(stepIndex, false);
        }

        protected override bool ValidateStep(int stepIndex, bool showError)
        {
            return stepIndex switch
            {
                5 => _selectedSegmentIndex >= 0,
                6 => _selectedPushNotificationsIndex >= 0,
                7 => _selectedReminderTimeIndex >= 0,
                _ => true
            };
        }

        protected override int GetNextStepIndex(int currentIndex)
        {
            if (currentIndex == 6 && _selectedPushNotificationsIndex != 0)
            {
                // Skip Step 7 (reminder time) if Push Notifications is No
                return 8;
            }
            return currentIndex + 1;
        }

        protected override int GetPreviousStepIndex(int currentIndex)
        {
            return currentIndex - 1;
        }

        protected override bool CheckIsLastStep(int currentIndex)
        {
            if (currentIndex == 6 && _selectedPushNotificationsIndex != 0)
            {
                return true;
            }
            return currentIndex >= GetStepCount() - 1;
        }

        protected override string GetStepTitle(int stepIndex)
        {
            return "";
        }


        protected override Task OnStepEnteredAsync(int stepIndex)
        {
            return Task.CompletedTask;
        }

        protected override Task OnStepExitingAsync(int stepIndex)
        {
            return Task.CompletedTask;
        }

        protected override async Task OnFlowCompletedAsync()
        {
            await SubmitAsync();
        }

        // ── Catalog Data Loading ──────────────────────────────

        public async Task LoadCatalogDataAsync()
        {
            IsLoading = true;
            LoadingText = GetLocalized("LOADING_DATA");

            try
            {
                AppState state = _storeService.GetAppState();
                string lang = state.lang ?? "en";

                var (data, _) = await _catalogService.LoadStartupAsync(lang);

                if (data == null)
                {
                    ErrorMessage = GetLocalized("COULD_NOT_LOAD_DATA");
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

                if (data.onboarding?.motivations != null && data.onboarding.motivations.Length > 0)
                {
                    MotivationOptions = data.onboarding.motivations.Select(m => m.label).ToList();
                }


                if (data.onboarding?.userSegments != null && data.onboarding.userSegments.Length > 0)
                {
                    SegmentOptions = data.onboarding.userSegments.Select(s => s.label).ToList();
                }


                DailyTimeCommitmentOptions = new List<string>
                {
                    "5 min",
                    "10 min",
                    "15 min",
                    "20+ min"
                };

                Debug.Log($"[OnboardingProfileViewModel] Catalog loaded: {GenderOptions.Count} genders, {MotivationOptions.Count} motivations");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingProfileViewModel] LoadCatalogDataAsync exception: {ex.Message}");
                ErrorMessage = GetLocalized("ERROR_LOADING_DATA");
                ShowErrorRequest?.Invoke(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void PrePopulateFromState()
        {
            if (_catalogData == null) return;

            AppState state = _storeService.GetAppState();

            SelectedGenderIndex = FindCatalogIndex(_catalogData.genders, state.userGender);
            SelectedActivityLevelIndex = FindCatalogIndex(_catalogData.activityLevels, state.userActivityLevel);
            SelectedEducationLevelIndex = FindCatalogIndex(_catalogData.educationLevels, state.userEducationLevel);
            SelectedAnnualIncomeIndex = FindCatalogIndex(_catalogData.annualIncomeLevels, state.userAnnualIncome);
            SelectedShoppingResponsibilityIndex = FindCatalogIndex(_catalogData.shoppingResponsibilities, state.userShoppingResponsibility);

            if (_catalogData.onboarding?.motivations != null)
            {
                SelectedMotivationIndex = FindCatalogIndex(_catalogData.onboarding.motivations, state.userMotivation);
            }
            if (_catalogData.onboarding?.userSegments != null)
            {
                SelectedSegmentIndex = FindCatalogIndex(_catalogData.onboarding.userSegments, state.userSegment);
            }

            if (state.userDailyTimeCommitmentMinutes <= 0) SelectedDailyTimeCommitmentIndex = -1;
            else if (state.userDailyTimeCommitmentMinutes <= 5) SelectedDailyTimeCommitmentIndex = 0;
            else if (state.userDailyTimeCommitmentMinutes <= 10) SelectedDailyTimeCommitmentIndex = 1;
            else if (state.userDailyTimeCommitmentMinutes <= 15) SelectedDailyTimeCommitmentIndex = 2;
            else SelectedDailyTimeCommitmentIndex = 3;

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

            SelectedPushNotificationsIndex = state.pushNotificationsEnabled ? 0 : 1;
            if (!string.IsNullOrEmpty(state.notificationPreferredTime))
            {
                int timeIdx = ReminderTimeOptions.IndexOf(state.notificationPreferredTime);
                SelectedReminderTimeIndex = timeIdx >= 0 ? timeIdx : 3;
            }
            else
            {
                SelectedReminderTimeIndex = 3;
            }
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

        // ── Skip & Submit Actions ─────────────────────────────

        public async void Skip()
        {
            await SkipAsync();
        }

        public async Task SkipAsync()
        {
            IsSubmitting = true;
            try
            {
                var request = new ProfileUpdateRequest
                {
                    preferences = new ProfileUpdatePreferences
                    {
                        onboardingProfileSkippedAt = DateTime.UtcNow.ToString("o")
                    }
                };

                await _authService.UpdateProfileAsync(request);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnboardingProfileViewModel] SkipAsync profile update error: {ex.Message}");
            }
            finally
            {
                _storeService.store.Dispatch(AppActions.setSkippedExtendedProfile.Invoke());
                IsSubmitting = false;
                NavigateNextScreen();
            }
        }

        public async Task SubmitAsync()
        {
            IsSubmitting = true;
            ErrorMessage = "";

            try
            {
                string shoppingResponsibilityCode = _selectedShoppingResponsibilityIndex >= 0 && _catalogData?.shoppingResponsibilities != null
                    ? _catalogData.shoppingResponsibilities[_selectedShoppingResponsibilityIndex].code
                    : null;

                string motivationCode = _selectedMotivationIndex >= 0 && _catalogData?.onboarding?.motivations != null
                    && _selectedMotivationIndex < _catalogData.onboarding.motivations.Length
                    ? _catalogData.onboarding.motivations[_selectedMotivationIndex].code
                    : null;

                string segmentCode = _selectedSegmentIndex >= 0 && _catalogData?.onboarding?.userSegments != null
                    && _selectedSegmentIndex < _catalogData.onboarding.userSegments.Length
                    ? _catalogData.onboarding.userSegments[_selectedSegmentIndex].code
                    : null;

                int dailyTimeMinutes = 0;
                if (_selectedDailyTimeCommitmentIndex == 0) dailyTimeMinutes = 5;
                else if (_selectedDailyTimeCommitmentIndex == 1) dailyTimeMinutes = 10;
                else if (_selectedDailyTimeCommitmentIndex == 2) dailyTimeMinutes = 15;
                else if (_selectedDailyTimeCommitmentIndex == 3) dailyTimeMinutes = 20;

                string[] dietaryCodes = null;
                if (_selectedDietaryPreferenceIndices != null && _selectedDietaryPreferenceIndices.Length > 0
                    && _catalogData?.dietaryPreferences != null)
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

                bool pushEnabled = _selectedPushNotificationsIndex == 0;
                string preferredTime = _selectedReminderTimeIndex >= 0 && _selectedReminderTimeIndex < _reminderTimeOptions.Count
                    ? _reminderTimeOptions[_selectedReminderTimeIndex]
                    : "10:00";

                AppState state = _storeService.GetAppState();

                var request = new ProfileUpdateRequest
                {
                    gender = _selectedGenderIndex >= 0 && _catalogData?.genders != null ? _catalogData.genders[_selectedGenderIndex].code : null,
                    activityLevel = _selectedActivityLevelIndex >= 0 && _catalogData?.activityLevels != null ? _catalogData.activityLevels[_selectedActivityLevelIndex].code : null,
                    educationLevel = _selectedEducationLevelIndex >= 0 && _catalogData?.educationLevels != null ? _catalogData.educationLevels[_selectedEducationLevelIndex].code : null,
                    annualIncome = _selectedAnnualIncomeIndex >= 0 && _catalogData?.annualIncomeLevels != null ? _catalogData.annualIncomeLevels[_selectedAnnualIncomeIndex].code : null,
                    segment = segmentCode ?? state.userSegment,

                    preferences = new ProfileUpdatePreferences
                    {
                        shoppingResponsibility = shoppingResponsibilityCode ?? state.userShoppingResponsibility,
                        dietaryPreference = dietaryCodes ?? state.userDietaryPreference,
                        motivation = motivationCode ?? state.userMotivation,
                        dailyTimeCommitmentMinutes = dailyTimeMinutes > 0 ? dailyTimeMinutes : state.userDailyTimeCommitmentMinutes,
                        onboardingProfileCompleted = true,
                        autoAddToPantry = state.userAutoAddToPantry
                    },

                    settings = new UserSettingsDto
                    {
                        pushNotificationsEnabled = pushEnabled,
                        notificationPreferredTime = pushEnabled ? preferredTime : "10:00"
                    }
                };

                Debug.Log($"[OnboardingProfileViewModel] Submitting profile update: {request.ToJson()}");

                var (success, error) = await _authService.UpdateProfileAsync(request);

                if (success)
                {
                    _storeService.store.Dispatch(AppActions.setExtendedProfile.Invoke());
                    _storeService.store.Dispatch(AppActions.setPushNotifications.Invoke(pushEnabled));
                    _storeService.store.Dispatch(AppActions.setNotificationPreferredTime.Invoke(preferredTime));

                    if (pushEnabled)
                    {
                        _notificationService?.SetNotificationsEnabled(true);
                        if (TimeSpan.TryParse(preferredTime, out var ts))
                        {
                            _notificationService?.ScheduleDailyMealReminder(ts);
                        }
                        _ = _notificationService?.RequestPermissionsAsync();
                    }
                    else
                    {
                        _notificationService?.SetNotificationsEnabled(false);
                    }

                    ErrorDetail = null;
                    NavigateNextScreen();
                }
                else
                {
                    ErrorDetail = error ?? new ApiErrorResponse { statusCode = 500, error = "COULD_NOT_SAVE_PROFILE", message = "Could not save profile" };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingProfileViewModel] SubmitAsync exception: {ex.Message}");
                ErrorMessage = GetLocalized("UNEXPECTED_ERROR");
                ShowErrorRequest?.Invoke(ErrorMessage);
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        public void OpenAvatarEditor()
        {
            RaiseNavigationRequested(Actions.go_to_avatar_editor, new Argument("fromOnboarding", "true"));
        }

        private void NavigateNextScreen()
        {
            RaiseNavigationRequested(Actions.onboardingprofile_to_onboarding_survey);
        }

        private string GetLocalized(string key)
        {
            return LocalizationSettings.StringDatabase?.GetLocalizedString("UI", key);
        }
    }
}