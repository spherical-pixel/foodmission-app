using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>
    /// A mission already has what it needs and only waits for its window to end: instead of asking for data, Nutri says
    /// when it will be completed. Once per mission attempt.
    /// </summary>
    public sealed class MissionSecuredPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public MissionSecuredPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            MissionDeadlines.ActiveMission mission = await _viewModel.CheckMissionSecuredAsync();
            return mission?.StartLocal == null ? null : new Instance(_viewModel, mission);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly MissionDeadlines.ActiveMission _mission;

            public Instance(HomeScreenViewModel viewModel, MissionDeadlines.ActiveMission mission)
            {
                _viewModel = viewModel;
                _mission = mission;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                _viewModel.MarkMissionSecuredAnnounced(_mission);
                string message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSION_SECURED_MESSAGE",
                    new object[] { _mission.Title, MissionCheckInScreen.CompletionDay(_mission.StartLocal.Value) });
                await HomePromptDialogs.ChooseNutriAsync(message, new PromptChoice("@UI:TXT_OK", ButtonVariant.Accent));
                return HomePromptResult.Dismissed;
            }
        }
    }
}
