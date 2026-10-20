using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Nutri's welcome sequence (what the app is, quests, progress, how to move around), once per user.</summary>
    public sealed class HomeTutorialPrompt : IHomePrompt
    {
        private readonly ITutorialService _tutorials;

        public HomeTutorialPrompt(ITutorialService tutorials)
        {
            _tutorials = tutorials;
        }

        public HomePromptKind Kind => HomePromptKind.Info;

        public Task<IHomePromptInstance> CheckAsync()
        {
            if (_tutorials == null || _tutorials.HasSeen(SectionTutorials.Home))
            {
                return Task.FromResult<IHomePromptInstance>(null);
            }
            return Task.FromResult<IHomePromptInstance>(new Instance(_tutorials));
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly ITutorialService _tutorials;

            public Instance(ITutorialService tutorials)
            {
                _tutorials = tutorials;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                // The service marks it seen once shown; the queue goes on (e.g. the quest start reminder)
                await _tutorials.ShowAsync(SectionTutorials.Home);
                return HomePromptResult.Dismissed;
            }
        }
    }
}
