using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>The Home prompts, in order (the coordinator groups them by kind, keeping this order inside each kind).</summary>
    public static class HomePrompts
    {
        public static IReadOnlyList<IHomePrompt> Create(HomeScreenViewModel viewModel, HomePromptRunState run, IWhatsNewService whatsNew)
        {
            return new IHomePrompt[]
            {
                new LegalConsentPrompt(viewModel),
                new PilotConsentPrompt(viewModel),
                new RewardCelebrationsPrompt(viewModel, run),
                new FailedMissionsPrompt(viewModel),
                new WhatsNewPrompt(whatsNew),
                new UnlockedQuestPrompt(viewModel, run),
                new OnboardingReminderPrompt(viewModel),
                new NotificationsPrompt(viewModel),
                new PilotSurveyPrompt(viewModel),
                new MissionNudgePrompt(viewModel),
                new DailyFoodFactPrompt(viewModel)
            };
        }
    }
}
