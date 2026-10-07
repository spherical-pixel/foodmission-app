using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>With an active quest, opens its next unread food fact once a day.</summary>
    public sealed class DailyFoodFactPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public DailyFoodFactPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            string code = await _viewModel.CheckDailyFoodFactAsync();
            return string.IsNullOrEmpty(code) ? null : new Instance(_viewModel, code);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly string _code;

            public Instance(HomeScreenViewModel viewModel, string code)
            {
                _viewModel = viewModel;
                _code = code;
            }

            public Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                _viewModel.MarkDailyFoodFactShown(_code);
                _viewModel.OpenFoodFact(_code);
                return Task.FromResult(HomePromptResult.Navigated);
            }
        }
    }
}
