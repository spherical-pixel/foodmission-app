using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    public partial class OnboardingGoalsViewModel : StepFlowViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IDimensionService _dimensionService;

        private bool m_IsSubmitting;
        public bool IsSubmitting
        {
            get => m_IsSubmitting;
            set => SetProperty(ref m_IsSubmitting, value);
        }

        private bool m_FromEditProfile;
        public bool FromEditProfile
        {
            get => m_FromEditProfile;
            set => SetProperty(ref m_FromEditProfile, value);
        }

        private bool m_FromHome;
        public bool FromHome
        {
            get => m_FromHome;
            set => SetProperty(ref m_FromHome, value);
        }

        public HashSet<string> SelectedTopicCodes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public event Action OnSelectionChanged;

        public IDimensionService DimensionService => _dimensionService;

        public event Action OnDimensionsLoaded;

        public OnboardingGoalsViewModel(
            IStoreService storeService,
            IAuthService authService = null,
            IDimensionService dimensionService = null) : base(storeService)
        {
            _authService = authService;
            _dimensionService = dimensionService;
        }

        public override void Initialize()
        {
            base.Initialize();
            LoadInitialGoals();
            _ = EnsureDimensionsLoadedAsync();
        }

        public async Task EnsureDimensionsLoadedAsync()
        {
            if (_dimensionService != null && !_dimensionService.IsLoaded)
            {
                var (result, error) = await _dimensionService.PreloadAsync();
                if (result != null && error == null)
                {
                    OnDimensionsLoaded?.Invoke();
                }
            }
        }

        public string GetTopicDisplayName(string topicCode)
        {
            if (string.IsNullOrEmpty(topicCode)) return string.Empty;

            // 1. Topic name from backend service (already localized in active language)
            if (_dimensionService != null)
            {
                var topic = _dimensionService.GetTopic(topicCode);
                if (topic != null && !string.IsNullOrWhiteSpace(topic.name))
                {
                    return topic.name;
                }
            }

            // 2. Fallback: raw code while backend has not loaded yet
            return topicCode;
        }

        public void LoadInitialGoals()
        {
            AppState state = _storeService?.GetAppState();
            SelectedTopicCodes.Clear();

            if (state?.userGoals != null && state.userGoals.Length > 0)
            {
                foreach (var g in state.userGoals)
                {
                    if (!string.IsNullOrEmpty(g))
                    {
                        SelectedTopicCodes.Add(g);
                    }
                }
            }
            else
            {
                // Default: All 26 topics selected on fresh onboarding
                foreach (var code in DimensionGoalsCatalog.AllTopicCodes)
                {
                    SelectedTopicCodes.Add(code);
                }
            }

            OnSelectionChanged?.Invoke();
            InvalidateValidation();
        }

        public bool IsTopicSelected(string topicCode)
        {
            if (string.IsNullOrEmpty(topicCode)) return false;
            return SelectedTopicCodes.Contains(topicCode);
        }

        public void SetTopicSelected(string topicCode, bool selected)
        {
            if (string.IsNullOrEmpty(topicCode)) return;

            bool changed = selected ? SelectedTopicCodes.Add(topicCode) : SelectedTopicCodes.Remove(topicCode);
            if (changed)
            {
                OnSelectionChanged?.Invoke();
                InvalidateValidation();
            }
        }

        public void ToggleTopic(string topicCode)
        {
            if (string.IsNullOrEmpty(topicCode)) return;
            SetTopicSelected(topicCode, !IsTopicSelected(topicCode));
        }

        protected override int GetStepCount() => 7;

        protected override string GetStepTitle(int stepIndex)
        {
            return "";
        }

        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;
        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;

        protected override bool ValidateStep(int stepIndex)
        {
            // Steps 0 to 5: Any individual dimension can have 0 selections without blocking
            if (stepIndex < 6)
            {
                return true;
            }

            // Step 6 (final step): Must have at least one topic selected overall
            return SelectedTopicCodes.Count > 0;
        }

        protected override async Task OnFlowCompletedAsync()
        {
            if (IsSubmitting) return;

            if (SelectedTopicCodes.Count == 0)
            {
                ErrorDetail = new ApiErrorResponse
                {
                    statusCode = 400,
                    error = "GOALS_EMPTY",
                    message = "GOALS_VALIDATION_ERROR"
                };
                return;
            }

            IsSubmitting = true;
            ErrorDetail = null;

            try
            {
                string[] goalsArray = SelectedTopicCodes.ToArray();

                // 1. Dispatch goals to Redux store
                _storeService?.store?.Dispatch(AppActions.setUserGoals.Invoke(goalsArray));

                // 2. Persist to backend inside preferences via PATCH /api/v1/users/me
                if (_authService != null)
                {
                    AppState state = _storeService.GetAppState();
                    var request = new ProfileUpdateRequest
                    {
                        preferences = new ProfileUpdatePreferences
                        {
                            shoppingResponsibility = !string.IsNullOrEmpty(state.userShoppingResponsibility) ? state.userShoppingResponsibility : null,
                            dietaryPreference = state.userDietaryPreference != null && state.userDietaryPreference.Length > 0 ? state.userDietaryPreference : null,
                            onboardingSurvey = state.userOnboardingSurvey != null && state.userOnboardingSurvey.HasAnswers() ? state.userOnboardingSurvey : null,
                            autoAddToPantry = state.userAutoAddToPantry,
                            goals = goalsArray
                        }
                    };

                    var (success, error) = await _authService.UpdateProfileAsync(request);
                    if (!success)
                    {
                        Debug.LogWarning("[OnboardingGoalsViewModel] Failed to sync goals with server via PATCH");
                        ErrorDetail = error ?? new ApiErrorResponse
                        {
                            statusCode = 500,
                            error = "COULD_NOT_SAVE_GOALS",
                            message = "Could not sync goals with server."
                        };
                        return;
                    }
                }

                // 3. Navigate
                if (FromHome)
                {
                    RaiseNavigationRequested(Actions.go_to_home);
                }
                else if (FromEditProfile)
                {
                    RaiseNavigationRequested(Actions.go_to_editprofile);
                }
                else
                {
                    RaiseNavigationRequested(Actions.onboardinggoals_to_onboardingsurvey);
                }
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
