using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>Reminds the next pending onboarding step (Profile → Goals → Survey).</summary>
    public sealed class OnboardingReminderPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public OnboardingReminderPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public Task<IHomePromptInstance> CheckAsync()
        {
            if (HomePromptSession.OnboardingDeferred)
            {
                return Task.FromResult<IHomePromptInstance>(null);
            }

            PendingOnboardingType pending = _viewModel.GetPendingOnboardingType();
            return Task.FromResult<IHomePromptInstance>(pending == PendingOnboardingType.None ? null : new Instance(_viewModel, pending));
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly PendingOnboardingType _pending;

            public Instance(HomeScreenViewModel viewModel, PendingOnboardingType pending)
            {
                _viewModel = viewModel;
                _pending = pending;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                string messageKey = _pending switch
                {
                    PendingOnboardingType.Survey => "ONBOARDING_REMINDER_SURVEY_MSG",
                    PendingOnboardingType.Goals => "ONBOARDING_REMINDER_GOALS_MSG",
                    _ => "ONBOARDING_REMINDER_PROFILE_MSG"
                };
                string actionKey = _pending switch
                {
                    PendingOnboardingType.Survey => "ONBOARDING_REMINDER_BTN_COMPLETE_SURVEY",
                    PendingOnboardingType.Goals => "ONBOARDING_REMINDER_BTN_COMPLETE_GOALS",
                    _ => "ONBOARDING_REMINDER_BTN_COMPLETE_PROFILE"
                };

                int choice = await HomePromptDialogs.ChooseNutriAsync(
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", messageKey),
                    new PromptChoice(LocalizationSettings.StringDatabase.GetLocalizedString("UI", actionKey), ButtonVariant.Accent),
                    new PromptChoice(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LATER"), ButtonVariant.Default));

                if (choice != 0)
                {
                    // "Later" or closed: not again this session
                    HomePromptSession.OnboardingDeferred = true;
                    return HomePromptResult.Dismissed;
                }

                if (_pending == PendingOnboardingType.Profile)
                {
                    _viewModel.NavigateToOnboardingProfile();
                }
                else if (_pending == PendingOnboardingType.Survey)
                {
                    _viewModel.NavigateToOnboardingSurvey();
                }
                else
                {
                    _viewModel.NavigateToOnboardingGoals();
                }
                return HomePromptResult.Navigated;
            }
        }
    }
}
