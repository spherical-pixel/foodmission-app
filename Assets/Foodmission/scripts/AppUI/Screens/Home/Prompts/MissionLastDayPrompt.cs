using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>Today is the last day to report a mission (past days can't be added once it fails): Nutri proposes the check-in.</summary>
    public sealed class MissionLastDayPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public MissionLastDayPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            MissionDeadlines.LastDay day = await _viewModel.CheckMissionLastDayAsync();
            return day == null || day.Missions.Count == 0 ? null : new Instance(_viewModel, day);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly MissionDeadlines.LastDay _day;

            public Instance(HomeScreenViewModel viewModel, MissionDeadlines.LastDay day)
            {
                _viewModel = viewModel;
                _day = day;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                _viewModel.MarkMissionLastDayShown();

                bool single = _day.Missions.Count == 1;
                string message = single
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSION_LAST_DAY_MESSAGE_ONE", new object[] { _day.Missions[0].Title })
                    : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSION_LAST_DAY_MESSAGE_MANY", new object[] { _day.Missions.Count });

                int choice = await HomePromptDialogs.ChooseNutriAsync(message,
                    new PromptChoice("@UI:MISSION_BTN_TELL_NUTRI", ButtonVariant.Accent),
                    new PromptChoice("@UI:MISSION_NUDGE_NOT_NOW", ButtonVariant.Default));
                if (choice == 0)
                {
                    _viewModel.OpenCheckIn(single ? _day.Missions[0].Code : null);
                    return HomePromptResult.Navigated;
                }
                return HomePromptResult.Dismissed;
            }
        }
    }
}
