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
        private readonly ICatalogService _catalogService;

        [ObservableProperty]
        private bool m_IsSubmitting;



        public string[] MeatMealsOptions { get; private set; } = System.Array.Empty<string>();
        public string[] MeatMealsCodes { get; private set; } = System.Array.Empty<string>();

        public string[] BeefFrequencyOptions { get; private set; } = System.Array.Empty<string>();
        public string[] BeefFrequencyCodes { get; private set; } = System.Array.Empty<string>();

        public string[] FoodWasteFrequencyOptions { get; private set; } = System.Array.Empty<string>();
        public string[] FoodWasteFrequencyCodes { get; private set; } = System.Array.Empty<string>();

        public string[] UltraProcessedFrequencyOptions { get; private set; } = System.Array.Empty<string>();
        public string[] UltraProcessedFrequencyCodes { get; private set; } = System.Array.Empty<string>();

        public string[] ReusableContainersFrequencyOptions { get; private set; } = System.Array.Empty<string>();
        public string[] ReusableContainersFrequencyCodes { get; private set; } = System.Array.Empty<string>();

        // Step States (-1 means no option selected)
        [ObservableProperty] private int m_MeatMealsIndex = -1;
        [ObservableProperty] private int m_BeefFrequencyIndex = -1;
        [ObservableProperty] private int m_FoodWasteFrequencyIndex = -1;
        [ObservableProperty] private int m_UltraProcessedFrequencyIndex = -1;
        [ObservableProperty] private int m_ReusableContainersFrequencyIndex = -1;

        public OnboardingSurveyViewModel(IStoreService storeService, ICatalogService catalogService, IAuthService authService = null) : base(storeService)
        {
            _catalogService = catalogService;
            _authService = authService;

            _storeSubscription = _store.Subscribe(
                state => state.lang,
                OnLanguageChanged
            );
        }

        private async void OnLanguageChanged(string newLang)
        {
            if (string.IsNullOrEmpty(newLang)) return;
            await LoadCatalogOptionsAsync();
        }

        public override void Initialize()
        {
            base.Initialize();
            _ = LoadCatalogOptionsAsync();
        }

        public async Task LoadCatalogOptionsAsync()
        {
            if (_catalogService == null)
            {
                Debug.LogWarning("[OnboardingSurveyViewModel] _catalogService is null!");
                return;
            }

            try
            {
                string lang = _storeService.GetAppState().lang ?? "en";

                var meatTask = _catalogService.GetWeeklyMeatRangesAsync(lang);
                var beefTask = _catalogService.GetWeeklyBeefFrequenciesAsync(lang);
                var wasteTask = _catalogService.GetWeeklyFoodWasteRangesAsync(lang);
                var upfTask = _catalogService.GetWeeklyUpfRangesAsync(lang);
                var reusableTask = _catalogService.GetWeeklyReusableRangesAsync(lang);

                await Task.WhenAll(meatTask, beefTask, wasteTask, upfTask, reusableTask);

                var (meatItems, _) = meatTask.Result;
                if (meatItems != null && meatItems.Length > 0)
                {
                    MeatMealsCodes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(meatItems, x => x.code));
                    MeatMealsOptions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(meatItems, x => x.label));
                }

                var (beefItems, _) = beefTask.Result;
                if (beefItems != null && beefItems.Length > 0)
                {
                    BeefFrequencyCodes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(beefItems, x => x.code));
                    BeefFrequencyOptions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(beefItems, x => x.label));
                }

                var (wasteItems, _) = wasteTask.Result;
                if (wasteItems != null && wasteItems.Length > 0)
                {
                    FoodWasteFrequencyCodes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(wasteItems, x => x.code));
                    FoodWasteFrequencyOptions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(wasteItems, x => x.label));
                }

                var (upfItems, _) = upfTask.Result;
                if (upfItems != null && upfItems.Length > 0)
                {
                    UltraProcessedFrequencyCodes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(upfItems, x => x.code));
                    UltraProcessedFrequencyOptions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(upfItems, x => x.label));
                }

                var (reusableItems, _) = reusableTask.Result;
                if (reusableItems != null && reusableItems.Length > 0)
                {
                    ReusableContainersFrequencyCodes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(reusableItems, x => x.code));
                    ReusableContainersFrequencyOptions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(reusableItems, x => x.label));
                }

                Debug.Log($"[OnboardingSurveyViewModel] Catalog options loaded in parallel! Meat count: {MeatMealsOptions.Length}, Beef count: {BeefFrequencyOptions.Length}");
                OnPropertyChanged(nameof(MeatMealsOptions));
                InvalidateValidation();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnboardingSurveyViewModel] LoadCatalogOptionsAsync exception: {ex.Message}");
            }
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
                    weeklyMeatConsumption = MeatMealsIndex >= 0 && MeatMealsIndex < MeatMealsCodes.Length ? MeatMealsCodes[MeatMealsIndex] : null,
                    weeklyBeefConsumption = BeefFrequencyIndex >= 0 && BeefFrequencyIndex < BeefFrequencyCodes.Length ? BeefFrequencyCodes[BeefFrequencyIndex] : null,
                    weeklyFoodWaste = FoodWasteFrequencyIndex >= 0 && FoodWasteFrequencyIndex < FoodWasteFrequencyCodes.Length ? FoodWasteFrequencyCodes[FoodWasteFrequencyIndex] : null,
                    weeklyUpfConsumption = UltraProcessedFrequencyIndex >= 0 && UltraProcessedFrequencyIndex < UltraProcessedFrequencyCodes.Length ? UltraProcessedFrequencyCodes[UltraProcessedFrequencyIndex] : null,
                    weeklyReusableOrRefill = ReusableContainersFrequencyIndex >= 0 && ReusableContainersFrequencyIndex < ReusableContainersFrequencyCodes.Length ? ReusableContainersFrequencyCodes[ReusableContainersFrequencyIndex] : null
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
                            onboardingSurvey = surveyData,
                            autoAddToPantry = state.userAutoAddToPantry
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
