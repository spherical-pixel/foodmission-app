using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    public enum QuestStartReminderKind
    {
        NoQuest,
        MissionsToStart
    }

    /// <summary>What Nutri reminds: choose a quest, or start one of the current quest's missions.</summary>
    public sealed class QuestStartReminder
    {
        public QuestStartReminderKind Kind { get; private set; }

        /// <summary>The current quest (MissionsToStart only).</summary>
        public CurrentQuest Quest { get; private set; }

        public static QuestStartReminder NoQuest() => new QuestStartReminder { Kind = QuestStartReminderKind.NoQuest };

        public static QuestStartReminder MissionsToStart(CurrentQuest quest) =>
            new QuestStartReminder { Kind = QuestStartReminderKind.MissionsToStart, Quest = quest };
    }

    /// <summary>
    /// No current quest: Nutri invites the user to choose one. A current quest with missions left (not started or
    /// failed) and none running: Nutri invites the user to start one. Each at most once per app session (marked when
    /// shown); the second waits for the next visit when a failure was already told in this one.
    /// </summary>
    public sealed class QuestStartReminderPrompt : IHomePrompt
    {
        private readonly HomeScreenViewModel _viewModel;
        private readonly HomePromptRunState _run;

        public QuestStartReminderPrompt(HomeScreenViewModel viewModel, HomePromptRunState run = null)
        {
            _viewModel = viewModel;
            _run = run;
        }

        public HomePromptKind Kind => HomePromptKind.Action;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            if (HomePromptSession.NoQuestReminded && HomePromptSession.MissionsToStartReminded)
            {
                return null;
            }

            QuestStartReminder reminder = await _viewModel.CheckQuestStartReminderAsync();
            if (reminder == null || IsRemindedThisSession(reminder.Kind))
            {
                return null;
            }
            if (reminder.Kind == QuestStartReminderKind.MissionsToStart && _run != null && _run.FailedMissionsNotified)
            {
                // Nutri just told a failure with its Restart button: not twice in one visit
                return null;
            }
            return new Instance(_viewModel, reminder);
        }

        private static bool IsRemindedThisSession(QuestStartReminderKind kind)
        {
            return kind == QuestStartReminderKind.NoQuest ? HomePromptSession.NoQuestReminded : HomePromptSession.MissionsToStartReminded;
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly QuestStartReminder _reminder;

            public Instance(HomeScreenViewModel viewModel, QuestStartReminder reminder)
            {
                _viewModel = viewModel;
                _reminder = reminder;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                string message;
                string actionKey;
                if (_reminder.Kind == QuestStartReminderKind.NoQuest)
                {
                    HomePromptSession.NoQuestReminded = true;
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "HOME_NUDGE_NO_QUEST");
                    actionKey = "HOME_NO_ACTIVE_QUEST_BUTTON";
                }
                else
                {
                    HomePromptSession.MissionsToStartReminded = true;
                    message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "HOME_NUDGE_MISSIONS_TO_START",
                        new object[] { _reminder.Quest.name ?? _reminder.Quest.questCode ?? "" });
                    actionKey = "HOME_NUDGE_BTN_SEE_MISSIONS";
                }

                int choice = await HomePromptDialogs.ChooseNutriAsync(message,
                    new PromptChoice("@UI:" + actionKey, ButtonVariant.Accent),
                    new PromptChoice("@UI:MISSION_NUDGE_NOT_NOW", ButtonVariant.Default));
                if (choice != 0)
                {
                    return HomePromptResult.Dismissed;
                }

                if (_reminder.Kind == QuestStartReminderKind.NoQuest)
                {
                    _viewModel.NavigateToQuests();
                }
                else
                {
                    // The quest detail has the ▶ start button on each mission
                    _viewModel.OpenQuest(CurrentQuestView.ToQuest(_reminder.Quest));
                }
                return HomePromptResult.Navigated;
            }
        }
    }
}
