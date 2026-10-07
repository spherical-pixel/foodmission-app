using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>Days without reporting or a stalled mission: Nutri proposes the check-in.</summary>
    public sealed class MissionNudgePrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;

        public MissionNudgePrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            if (_viewModel.IsMissionLastDayShownToday)
            {
                // Nutri already asked for today's report (last day): one mission reminder per day
                return null;
            }
            MissionNudge nudge = await _viewModel.CheckMissionNudgeAsync();
            return nudge == null ? null : new Instance(_viewModel, nudge);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly MissionNudge _nudge;

            public Instance(HomeScreenViewModel viewModel, MissionNudge nudge)
            {
                _viewModel = viewModel;
                _nudge = nudge;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                _viewModel.MarkNudgeShown(_nudge);

                var choices = new List<PromptChoice>();
                var actions = new List<System.Action>();
                string message;
                if (_nudge.Kind == MissionNudgeKind.MissingDays)
                {
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSION_NUDGE_MISSING_DAYS", new object[] { _nudge.MissingDays });
                    choices.Add(new PromptChoice("@UI:MISSION_BTN_TELL_NUTRI", ButtonVariant.Accent));
                    actions.Add(() => _viewModel.OpenCheckIn());
                }
                else
                {
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSION_NUDGE_MESSAGE", new object[] { _nudge.MissionTitle });
                    if (_nudge.AutoModule != null)
                    {
                        choices.Add(new PromptChoice("@UI:" + _nudge.AutoModule.ButtonKey, ButtonVariant.Accent));
                        actions.Add(() => _viewModel.OpenMissionModule(_nudge.AutoModule));
                    }
                    choices.Add(new PromptChoice("@UI:MISSION_BTN_TELL_NUTRI", _nudge.AutoModule != null ? ButtonVariant.Default : ButtonVariant.Accent));
                    actions.Add(() => _viewModel.OpenCheckIn(_nudge.MissionCode));
                }
                choices.Add(new PromptChoice("@UI:MISSION_NUDGE_NOT_NOW", ButtonVariant.Default));

                int choice = await HomePromptDialogs.ChooseNutriAsync(message, choices.ToArray());
                if (choice >= 0 && choice < actions.Count)
                {
                    actions[choice]();
                    return HomePromptResult.Navigated;
                }
                return HomePromptResult.Dismissed;
            }
        }
    }
}
