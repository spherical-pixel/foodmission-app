using System.Threading.Tasks;

using Unity.AppUI.UI;

namespace eu.foodmission.platform
{
    /// <summary>Users in pilot countries who never accepted the pilot consent are asked before any survey.</summary>
    public sealed class PilotConsentPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public PilotConsentPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Blocking;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            if (!_viewModel.IsUserInPilotCountry() || await _viewModel.HasAcceptedPilotConsentAsync())
            {
                return null;
            }

            var (content, _) = await _viewModel.GetPilotConsentFormAsync();
            return string.IsNullOrEmpty(content) ? null : new Instance(_viewModel, content);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly string _content;

            public Instance(HomeScreenViewModel viewModel, string content)
            {
                _viewModel = viewModel;
                _content = content;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                while (host.IsActive)
                {
                    if (await HomePromptDialogs.ShowDocumentAsync(host.DialogAnchor, "@UI:PILOT_CONSENT_TITLE", _content))
                    {
                        await _viewModel.AcceptPilotConsentAsync();
                        return HomePromptResult.Dismissed;
                    }

                    int choice = await HomePromptDialogs.ChooseInfoAsync(host.DialogAnchor, "@UI:MESSAGE_TITLE_WARNING", "@UI:NOT_ACCP_LEGAL_WARNING",
                        new PromptChoice("@UI:TXT_REVIEW_DOCUMENT", ButtonVariant.Accent),
                        new PromptChoice("@UI:TXT_CANCEL", ButtonVariant.Default));
                    if (choice != 0)
                    {
                        // Declined for now: asked again on the next Home entry
                        return HomePromptResult.Dismissed;
                    }
                }
                return HomePromptResult.Navigated;
            }
        }
    }
}
