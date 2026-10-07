using System.Threading.Tasks;

using Unity.AppUI.UI;

namespace eu.foodmission.platform
{
    /// <summary>Asks for push notifications (system permission dialog on Yes).</summary>
    public sealed class NotificationsPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public NotificationsPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public Task<IHomePromptInstance> CheckAsync()
        {
            bool pending = !HomePromptSession.NotificationsDeferred && _viewModel.ShouldPromptForNotifications();
            return Task.FromResult<IHomePromptInstance>(pending ? new Instance(_viewModel) : null);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;

            public Instance(HomeScreenViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                int choice = await HomePromptDialogs.ChooseNutriAsync("@UI:ONBOARDING_PROFILE.NUTRI_STEP_6",
                    new PromptChoice("@UI:ONBOARDING_PROFILE.NOTIFICATIONS_OPT_YES", ButtonVariant.Accent),
                    new PromptChoice("@UI:ONBOARDING_PROFILE.NOTIFICATIONS_OPT_NO", ButtonVariant.Default));

                if (choice == 0)
                {
                    await _viewModel.AcceptNotificationsAsync();
                }
                else if (choice == 1)
                {
                    HomePromptSession.NotificationsDeferred = true;
                    _viewModel.DeclineNotifications();
                }
                else
                {
                    // Closed without answering: not again this session, nothing persisted
                    HomePromptSession.NotificationsDeferred = true;
                }
                return HomePromptResult.Dismissed;
            }
        }
    }
}
