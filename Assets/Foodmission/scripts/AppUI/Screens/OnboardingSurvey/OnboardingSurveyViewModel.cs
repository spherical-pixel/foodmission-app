using System;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class OnboardingSurveyViewModel : StepFlowViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private bool m_IsSubmitting;

        public static readonly LocalizedOption[] MeatMealsOptions = new LocalizedOption[]
        {
            new LocalizedOption("UI","txtMealsPerWeek", "0–4"),
            new LocalizedOption("UI","txtMealsPerWeek", "5–9"),
            new LocalizedOption("UI","txtMealsPerWeek", "10–14"),
            new LocalizedOption("UI","txtMealsPerWeek", "15+")
        };

        public static readonly string[] MeatMealsCodes = new string[]
        {
            "MEALS_0_4",
            "MEALS_5_9",
            "MEALS_10_14",
            "MEALS_15_PLUS"
        };

        public static readonly LocalizedOption[] BeefFrequencyOptions = new LocalizedOption[]
        {
            new LocalizedOption("UI","txtNever"),
            new LocalizedOption("UI","txtLessOnceWeek"),
            new LocalizedOption("UI","txtTimesPerWeek", "1–2"),
            new LocalizedOption("UI","txtTimesPerWeek", "3+")
        };

        public static readonly string[] BeefFrequencyCodes = new string[]
        {
            "NEVER",
            "LESS_THAN_ONCE",
            "TIMES_1_2",
            "TIMES_3_PLUS"
        };

        public static readonly LocalizedOption[] FoodWasteFrequencyOptions = new LocalizedOption[]
        {
            new LocalizedOption("UI","txtNever"),
            new LocalizedOption("UI","txtTimesPerWeek", "1–2"),
            new LocalizedOption("UI","txtTimesPerWeek", "3–4"),
            new LocalizedOption("UI","txtTimesPerWeek", "5+")
        };

        public static readonly string[] FoodWasteFrequencyCodes = new string[]
        {
            "NEVER",
            "TIMES_1_2",
            "TIMES_3_4",
            "TIMES_5_PLUS"
        };

        public static readonly LocalizedOption[] UltraProcessedFrequencyOptions = new LocalizedOption[]
        {
            new LocalizedOption("UI","txtTimesPerWeek", "0–3"),
            new LocalizedOption("UI","txtTimesPerWeek", "4–9"),
            new LocalizedOption("UI","txtTimesPerWeek", "10–14"),
            new LocalizedOption("UI","txtTimesPerWeek", "15+")
        };

        public static readonly string[] UltraProcessedFrequencyCodes = new string[]
        {
            "TIMES_0_3",
            "TIMES_4_9",
            "TIMES_10_14",
            "TIMES_15_PLUS"
        };

        public static readonly LocalizedOption[] ReusableContainersFrequencyOptions = new LocalizedOption[]
        {
            new LocalizedOption("UI","txtActionsPerWeek", "0–2"),
            new LocalizedOption("UI","txtActionsPerWeek", "3–6"),
            new LocalizedOption("UI","txtActionsPerWeek", "7–9"),
            new LocalizedOption("UI","txtActionsPerWeek", "10+")
        };

        public static readonly string[] ReusableContainersFrequencyCodes = new string[]
        {
            "ACTIONS_0_2",
            "ACTIONS_3_6",
            "ACTIONS_7_9",
            "ACTIONS_10_PLUS"
        };

        // Step States (-1 means no option selected)
        [ObservableProperty] private int m_MeatMealsIndex = -1;
        [ObservableProperty] private int m_BeefFrequencyIndex = -1;
        [ObservableProperty] private int m_FoodWasteFrequencyIndex = -1;
        [ObservableProperty] private int m_UltraProcessedFrequencyIndex = -1;
        [ObservableProperty] private int m_ReusableContainersFrequencyIndex = -1;

        public OnboardingSurveyViewModel(IStoreService storeService, IAuthService authService = null) : base(storeService)
        {
            _authService = authService;
        }

        protected override int GetStepCount() => 6;

        protected override string GetStepTitle(int stepIndex)
        {
            return "";
        }

        protected override bool ValidateStep(int stepIndex)
        {
            return stepIndex switch
            {
                0 => true,
                1 => MeatMealsIndex >= 0 && MeatMealsIndex < MeatMealsOptions.Length,
                2 => BeefFrequencyIndex >= 0 && BeefFrequencyIndex < BeefFrequencyOptions.Length,
                3 => FoodWasteFrequencyIndex >= 0 && FoodWasteFrequencyIndex < FoodWasteFrequencyOptions.Length,
                4 => UltraProcessedFrequencyIndex >= 0 && UltraProcessedFrequencyIndex < UltraProcessedFrequencyOptions.Length,
                5 => ReusableContainersFrequencyIndex >= 0 && ReusableContainersFrequencyIndex < ReusableContainersFrequencyOptions.Length,
                _ => false
            };
        }

        protected override Task OnStepEnteredAsync(int stepIndex)
        {
            Debug.Log($"[OnboardingSurvey] Entered Step {stepIndex}");
            return Task.CompletedTask;
        }

        protected override Task OnStepExitingAsync(int stepIndex)
        {
            Debug.Log($"[OnboardingSurvey] Exiting Step {stepIndex}");
            return Task.CompletedTask;
        }

        protected override async Task OnFlowCompletedAsync()
        {
            if (IsSubmitting) return;
            IsSubmitting = true;

            try
            {
                Debug.Log($"[OnboardingSurvey] Survey Completed! MeatMeals: {MeatMealsIndex}, BeefFreq: {BeefFrequencyIndex}, FoodWaste: {FoodWasteFrequencyIndex}, UltraProcessed: {UltraProcessedFrequencyIndex}, Reusable: {ReusableContainersFrequencyIndex}");

                var surveyData = new OnboardingSurveyData
                {
                    meatMeals = MeatMealsIndex >= 0 && MeatMealsIndex < MeatMealsCodes.Length ? MeatMealsCodes[MeatMealsIndex] : null,
                    beefFrequency = BeefFrequencyIndex >= 0 && BeefFrequencyIndex < BeefFrequencyCodes.Length ? BeefFrequencyCodes[BeefFrequencyIndex] : null,
                    foodWasteFrequency = FoodWasteFrequencyIndex >= 0 && FoodWasteFrequencyIndex < FoodWasteFrequencyCodes.Length ? FoodWasteFrequencyCodes[FoodWasteFrequencyIndex] : null,
                    ultraProcessedFrequency = UltraProcessedFrequencyIndex >= 0 && UltraProcessedFrequencyIndex < UltraProcessedFrequencyCodes.Length ? UltraProcessedFrequencyCodes[UltraProcessedFrequencyIndex] : null,
                    reusableContainersFrequency = ReusableContainersFrequencyIndex >= 0 && ReusableContainersFrequencyIndex < ReusableContainersFrequencyCodes.Length ? ReusableContainersFrequencyCodes[ReusableContainersFrequencyIndex] : null
                };

                // 1. Dispatch survey answers to Redux store (persisted in PlayerPrefs/LocalStorage)
                _storeService.store.Dispatch(AppActions.setOnboardingSurvey.Invoke(surveyData));

                // 2. Sync survey data inside preferences via PATCH /api/v1/users/me
                if (_authService != null)
                {
                    AppState state = _storeService.GetAppState();

                    var request = new ProfileUpdateRequest
                    {
                        preferences = new ProfileUpdatePreferences
                        {
                            shoppingResponsibility = !string.IsNullOrEmpty(state.userShoppingResponsibility) ? state.userShoppingResponsibility : null,
                            dietaryPreference = state.userDietaryPreference != null && state.userDietaryPreference.Length > 0 ? state.userDietaryPreference : null,
                            onboardingSurvey = surveyData
                        }
                    };

                    var (success, error) = await _authService.UpdateProfileAsync(request);
                    if (!success)
                    {
                        Debug.LogWarning("[OnboardingSurveyViewModel] Failed to sync survey data with server via PATCH");
                        ErrorDetail = error ?? new ApiErrorResponse { statusCode = 500, error = "COULD_NOT_SAVE_SURVEY", message = "Could not sync survey responses with server." };
                        return;
                    }
                    ErrorDetail = null;
                }

                // 3. Complete survey flow & navigate to OnboardingAvatar screen
                RaiseNavigationRequested(Actions.onboardingprofile_to_onboardingavatar, new Unity.AppUI.Navigation.Argument("fromOnboarding", "true"));
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
