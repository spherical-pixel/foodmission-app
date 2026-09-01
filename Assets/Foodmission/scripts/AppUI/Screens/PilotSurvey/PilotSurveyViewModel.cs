using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class PilotSurveyViewModel : StepFlowViewModelBase
    {
        private readonly ISurveyService _surveyService;
        private readonly IPilotSurveyService _pilotSurveyService;

        [ObservableProperty] private bool m_IsLoading;
        [ObservableProperty] private bool m_IsSubmitting;
        [ObservableProperty] private string m_SurveySlug;
        [ObservableProperty] private string m_SurveyId;
        [ObservableProperty] private string m_SurveyTitle;
        [ObservableProperty] private string m_SurveyDescription;
        [ObservableProperty] private string m_NutriMessage;

        public SurveyDto CurrentSurvey { get; private set; }
        public QuestionDto[] Questions { get; private set; } = Array.Empty<QuestionDto>();
        public int[] SelectedAnswers { get; private set; } = Array.Empty<int>();

        public PilotSurveyViewModel(
            IStoreService storeService,
            ISurveyService surveyService,
            IPilotSurveyService pilotSurveyService) : base(storeService)
        {
            _surveyService = surveyService;
            _pilotSurveyService = pilotSurveyService;
        }

        public async Task LoadSurveyAsync(string slugOrId, string lang = null)
        {
            if (string.IsNullOrEmpty(slugOrId))
                return;

            IsLoading = true;
            SurveyDto survey = null;

            // Try loading by slug first
            var (slugResult, _) = await _surveyService.GetSurveyBySlugAsync(slugOrId, lang);
            if (slugResult != null)
            {
                survey = slugResult;
            }
            else
            {
                // Fallback to load by ID
                var (idResult, _) = await _surveyService.GetSurveyByIdAsync(slugOrId, lang);
                survey = idResult;
            }

            if (survey != null)
            {
                SetSurvey(survey);
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Survey '{slugOrId}' could not be loaded.");
            }

            IsLoading = false;
        }

        public void SetSurvey(SurveyDto survey)
        {
            CurrentSurvey = survey;
            SurveyId = survey?.id ?? "";
            SurveySlug = survey?.slug ?? "";
            SurveyTitle = survey?.title ?? "";
            SurveyDescription = survey?.description ?? "";

            if (survey?.questions != null && survey.questions.Length > 0)
            {
                Questions = survey.questions.OrderBy(q => q.order).ToArray();
                SelectedAnswers = new int[Questions.Length];
                for (int i = 0; i < SelectedAnswers.Length; i++)
                {
                    SelectedAnswers[i] = -1; // -1 means unselected
                }
            }
            else
            {
                Questions = Array.Empty<QuestionDto>();
                SelectedAnswers = Array.Empty<int>();
            }

            StepCount = GetStepCount();
            CurrentStepIndex = 0;
            RefreshStepState();
            RequestRebuildSteps();
        }

        public void SetAnswer(int questionIndex, int value)
        {
            if (questionIndex >= 0 && questionIndex < SelectedAnswers.Length)
            {
                SelectedAnswers[questionIndex] = value;
                InvalidateValidation();
            }
        }

        public int GetAnswer(int questionIndex)
        {
            if (questionIndex >= 0 && questionIndex < SelectedAnswers.Length)
            {
                return SelectedAnswers[questionIndex];
            }
            return -1;
        }

        public string GetStepNutriMessage(int stepIndex)
        {
            if (Questions == null || Questions.Length == 0 || stepIndex < 0 || stepIndex >= Questions.Length)
                return "";

            var question = Questions[stepIndex];
            return question?.text ?? "";
        }

        // ── StepFlowViewModelBase Overrides ─────────────────────────

        protected override int GetStepCount() => Questions?.Length ?? 0;

        protected override bool ValidateStep(int stepIndex)
        {
            return ValidateStep(stepIndex, showError: false);
        }

        protected override bool ValidateStep(int stepIndex, bool showError)
        {
            // Steps are non-mandatory: users can proceed without answering any step
            return true;
        }

        protected override string GetStepTitle(int stepIndex)
        {
            if (Questions != null && stepIndex >= 0 && stepIndex < Questions.Length)
            {
                return Questions[stepIndex]?.text ?? "";
            }
            return "";
        }

        protected override Task OnStepEnteredAsync(int stepIndex)
        {
            NutriMessage = GetStepNutriMessage(stepIndex);
            return Task.CompletedTask;
        }

        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;

        protected override async Task OnFlowCompletedAsync()
        {
            Debug.Log($"[PilotSurveyViewModel] OnFlowCompletedAsync triggered. SurveyId: '{SurveyId}', Slug: '{SurveySlug}', Questions: {Questions?.Length}, IsSubmitting: {IsSubmitting}");

            if (IsSubmitting) return;

            if (Questions == null || Questions.Length == 0)
            {
                Debug.LogWarning("[PilotSurveyViewModel] No questions found. Popping backstack immediately.");
                RaiseNavigationRequested("popBackStack", Array.Empty<Argument>());
                return;
            }

            IsSubmitting = true;

            var responseList = new System.Collections.Generic.List<SubmitQuestionResponseDto>();
            for (int i = 0; i < Questions.Length; i++)
            {
                if (SelectedAnswers[i] >= 1 && SelectedAnswers[i] <= 5)
                {
                    responseList.Add(new SubmitQuestionResponseDto
                    {
                        questionId = Questions[i].id,
                        value = SelectedAnswers[i]
                    });
                }
            }

            Debug.Log($"[PilotSurveyViewModel] Submitting {responseList.Count} responses to backend for survey '{SurveyId}'...");

            var requestDto = new SubmitSurveyResponseDto
            {
                responses = responseList.ToArray()
            };

            var (res, error) = await _surveyService.SubmitSurveyResponseAsync(SurveyId, requestDto);

            if (error == null)
            {
                Debug.Log($"[PilotSurveyViewModel] Survey responses submitted successfully! Result ID: {res?.id}. Marking survey '{SurveySlug}' completed...");
                await _pilotSurveyService.MarkSurveyCompletedAsync(SurveySlug, SurveyId);
                IsSubmitting = false;
                Debug.Log("[PilotSurveyViewModel] Navigating back (popBackStack)...");
                RaiseNavigationRequested("popBackStack", Array.Empty<Argument>());
            }
            else
            {
                Debug.LogError($"[PilotSurveyViewModel] Failed to submit survey responses. Error: {error?.message} ({error?.statusCode})");
                IsSubmitting = false;
                ErrorDetail = error;
            }
        }

        public void PostponeFlow()
        {
            _pilotSurveyService.PostponeSurvey(SurveySlug);
            RaiseNavigationRequested("popBackStack", Array.Empty<Argument>());
        }

        public void SkipFlow()
        {
            _pilotSurveyService.SkipSurvey(SurveySlug);
            RaiseNavigationRequested("popBackStack", Array.Empty<Argument>());
        }
    }
}
