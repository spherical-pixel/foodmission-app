using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Onboarding survey: a welcome step plus one optional step per question served by the backend.</summary>
    [ObservableObject]
    public partial class OnboardingSurveyViewModel : StepFlowViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IGamificationService _gamificationService;
        private readonly IOnboardingRun _onboardingRun;
        private readonly OnboardingSurveyData _answers = new OnboardingSurveyData();
        private OnboardingSurveyQuestion[] _questions = new OnboardingSurveyQuestion[0];
        private int _loadRequest;

        [ObservableProperty]
        private bool m_IsSubmitting;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool m_FromHome = false;

        public IReadOnlyList<OnboardingSurveyQuestion> Questions => _questions;

        /// <summary>False until the questions load; the load error dialog then offers a retry.</summary>
        public bool HasQuestions => _questions.Length > 0;

        public OnboardingSurveyViewModel(IStoreService storeService, IGamificationService gamificationService, IAuthService authService = null, IOnboardingRun onboardingRun = null) : base(storeService)
        {
            _gamificationService = gamificationService;
            _authService = authService;
            _onboardingRun = onboardingRun;

            _storeSubscription = _store.Subscribe(
                state => state.lang,
                OnLanguageChanged
            );
        }

        private async void OnLanguageChanged(string newLang)
        {
            if (string.IsNullOrEmpty(newLang))
            {
                return;
            }
            await LoadQuestionsAsync();
        }

        public override void Initialize()
        {
            base.Initialize();
            _ = LoadQuestionsAsync();
        }

        /// <summary>Loads (or reloads, e.g. after a language change) the questions; answers are kept by field.</summary>
        public async Task LoadQuestionsAsync()
        {
            if (_gamificationService == null)
            {
                Debug.LogWarning("[OnboardingSurveyViewModel] _gamificationService is null!");
                return;
            }

            // A slower response (e.g. for the previous language) must not replace a newer one
            int request = ++_loadRequest;
            IsLoading = true;
            try
            {
                var (questions, error) = await _gamificationService.GetOnboardingSurveyAsync();
                if (request != _loadRequest)
                {
                    return;
                }
                if (error != null)
                {
                    ErrorDetail = error;
                    return;
                }

                _questions = questions ?? new OnboardingSurveyQuestion[0];
                StepCount = GetStepCount();
                CurrentStepIndex = Math.Min(CurrentStepIndex, StepCount - 1);
                RequestRebuildSteps();
                RefreshStepState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingSurveyViewModel] LoadQuestionsAsync exception: {ex.Message}");
            }
            finally
            {
                if (request == _loadRequest)
                {
                    IsLoading = false;
                }
            }
        }

        public OnboardingSurveyQuestion QuestionForStep(int stepIndex)
        {
            int index = stepIndex - 1;
            return index >= 0 && index < _questions.Length ? _questions[index] : null;
        }

        public string GetAnswer(string field) => _answers.Get(field);

        /// <summary>Null clears the answer (every question is optional).</summary>
        public void SetAnswer(string field, string value) => _answers.Set(field, value);

        protected override int GetStepCount() => 1 + _questions.Length;

        protected override string GetStepTitle(int stepIndex) => "";

        // Welcome needs the questions (never submit a survey the user never saw); questions can be skipped
        protected override bool ValidateStep(int stepIndex) => stepIndex == 0 ? _questions.Length > 0 : stepIndex < StepCount;

        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;

        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;

        protected override async Task OnFlowCompletedAsync()
        {
            if (IsSubmitting)
            {
                return;
            }
            IsSubmitting = true;

            try
            {
                var surveyData = OnboardingSurveyData.FromMap(_answers.ToMap());

                // 1. Persist the answers locally
                _storeService.store.Dispatch(AppActions.setOnboardingSurvey.Invoke(surveyData));

                // 2. First submission scores every dimension and sets the levels. 409 = already onboarded
                //    (retake from Home): the answers are only updated via PATCH below, levels are not rescored.
                bool needsPatch = true;
                if (_gamificationService != null)
                {
                    var (result, submitError) = await _gamificationService.SubmitOnboardingSurveyAsync(surveyData);
                    if (submitError == null)
                    {
                        _storeService.store.Dispatch(AppActions.setUserSegment.Invoke(result?.segment));
                        needsPatch = false;
                    }
                    else if (submitError.statusCode != 409)
                    {
                        ErrorDetail = submitError;
                        return;
                    }
                }

                // 3. Sync survey data inside preferences via PATCH /api/v1/users/me
                if (needsPatch && _authService != null)
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
                }

                // 4. The backend scored the levels: load them for the proposal screen
                await DimensionLevels.RefreshAsync(_gamificationService, _storeService);

                ErrorDetail = null;
                if (IsInOnboardingRun)
                {
                    _onboardingRun.MarkDone(OnboardingStep.Survey);
                }

                // 5. Show the proposed per-dimension levels; that screen continues to Home or the avatar step
                RaiseNavigationRequested(Actions.go_to_dimension_levels, DimensionLevelsNavigation.ProposalArguments(FromHome));
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        /// <summary>Inside the onboarding run (not Home's reminder): Skip is shown.</summary>
        public bool IsInOnboardingRun => _onboardingRun != null && _onboardingRun.IsActive && !FromHome;

        /// <summary>Skip (onboarding run only): skips the survey and the level proposal; back to the progress screen.</summary>
        public void Skip()
        {
            _onboardingRun?.MarkSkipped(OnboardingStep.Survey);
            RaiseNavigationRequested(Actions.go_to_onboarding_progress);
        }

        /// <summary>✕: leaves for Home without saving; the survey stays pending (Home reminds it next session).</summary>
        public void Close()
        {
            RaiseNavigationRequested(Actions.go_to_home);
        }
    }
}
