using System.Threading.Tasks;

using Unity.AppUI.UI;

namespace eu.foodmission.platform
{
    /// <summary>The pilot survey due today. "Later" (or opening it without finishing) postpones it until tomorrow.</summary>
    public sealed class PilotSurveyPrompt : IHomePrompt
    {
        private const int AnswerNow = 0;
        private const int Decline = 2;

        private readonly HomeScreenViewModel _viewModel;

        public PilotSurveyPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            SurveyDto survey = await _viewModel.CheckPendingPilotSurveyAsync();
            return survey == null ? null : new Instance(_viewModel, survey);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly SurveyDto _survey;

            public Instance(HomeScreenViewModel viewModel, SurveyDto survey)
            {
                _viewModel = viewModel;
                _survey = survey;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                int choice = await HomePromptDialogs.ChooseNutriAsync("@UI:NEW_SURVEY_MESSAGE",
                    new PromptChoice("@UI:NEW_SURVEY_ANSWER_NOW", ButtonVariant.Accent),
                    new PromptChoice("@UI:NEW_SURVEY_LATER", ButtonVariant.Default),
                    new PromptChoice("@UI:NEW_SURVEY_DECLINE", ButtonVariant.Default));

                if (choice == Decline)
                {
                    _viewModel.SkipPilotSurvey(_survey.slug);
                    return HomePromptResult.Dismissed;
                }

                // Answer now, Later or closed: postponed until tomorrow. Finishing it marks it completed anyway.
                _viewModel.PostponePilotSurvey(_survey.slug);
                if (choice == AnswerNow)
                {
                    _viewModel.NavigateToPilotSurvey(_survey.slug ?? _survey.id);
                    return HomePromptResult.Navigated;
                }
                return HomePromptResult.Dismissed;
            }
        }
    }
}
