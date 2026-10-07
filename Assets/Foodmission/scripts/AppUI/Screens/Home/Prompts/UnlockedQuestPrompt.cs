using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>After a completed quest is celebrated, opens the next quest's detail (it has the "Start" button).</summary>
    public sealed class UnlockedQuestPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;
        private readonly HomePromptRunState _run;

        public UnlockedQuestPrompt(HomeScreenViewModel viewModel, HomePromptRunState run)
        {
            _viewModel = viewModel;
            _run = run;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public Task<IHomePromptInstance> CheckAsync()
        {
            return Task.FromResult<IHomePromptInstance>(_run.QuestToOffer != null ? new Instance(_viewModel, _run) : null);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly HomePromptRunState _run;

            public Instance(HomeScreenViewModel viewModel, HomePromptRunState run)
            {
                _viewModel = viewModel;
                _run = run;
            }

            public Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                Quest quest = _run.QuestToOffer;
                _run.QuestToOffer = null;
                _viewModel.OpenQuest(quest);
                return Task.FromResult(HomePromptResult.Navigated);
            }
        }
    }
}
