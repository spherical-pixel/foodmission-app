using System.Collections.Generic;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>Completed missions/quests and new badges, one celebration after another; each is marked when shown.</summary>
    public sealed class RewardCelebrationsPrompt : IHomePrompt
    {
        private const int GapMs = 250;

        private readonly HomeScreenViewModel _viewModel;
        private readonly HomePromptRunState _run;

        public RewardCelebrationsPrompt(HomeScreenViewModel viewModel, HomePromptRunState run)
        {
            _viewModel = viewModel;
            _run = run;
        }

        public HomePromptKind Kind => HomePromptKind.Info;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            List<PendingRewardCelebration> rewards = await _viewModel.CheckPendingGamificationRewardsAsync();
            if (rewards != null && rewards.Count > 0)
            {
                return new Instance(_viewModel, _run, rewards);
            }

            // Safety net: the active quest is already completed but its QUEST_COMPLETED was never celebrated
            string questBefore = _viewModel.GetCurrentQuestIdFromStore();
            PendingRewardCelebration completedQuest = await _viewModel.CheckCompletedActiveQuestAsync();
            if (completedQuest != null)
            {
                return new Instance(_viewModel, _run, new List<PendingRewardCelebration> { completedQuest });
            }

            bool questCleared = !string.IsNullOrEmpty(questBefore) && string.IsNullOrEmpty(_viewModel.GetCurrentQuestIdFromStore());
            // An already celebrated quest was cleared: only the "no active quest" widget needs refreshing
            return questCleared ? new RefreshOnly(_viewModel) : null;
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly HomePromptRunState _run;
            private readonly List<PendingRewardCelebration> _rewards;

            public Instance(HomeScreenViewModel viewModel, HomePromptRunState run, List<PendingRewardCelebration> rewards)
            {
                _viewModel = viewModel;
                _run = run;
                _rewards = rewards;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                for (int i = 0; i < _rewards.Count; i++)
                {
                    if (!host.IsActive)
                    {
                        // The rest stay unmarked and are celebrated on the next Home entry
                        return HomePromptResult.Navigated;
                    }

                    PendingRewardCelebration item = _rewards[i];
                    var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    RewardCelebrationDialog.Show(
                        item.Reward,
                        contextTitle: item.ContextTitle,
                        onDismiss: () => closed.TrySetResult(true),
                        extraItem: UnlockedQuestCard(item.UnlockedQuest));
                    await _viewModel.MarkCelebrationShownAsync(item);
                    if (item.UnlockedQuest != null)
                    {
                        _run.QuestToOffer = item.UnlockedQuest;
                    }
                    await closed.Task;

                    if (i < _rewards.Count - 1)
                    {
                        await Task.Delay(GapMs);
                    }
                }

                _ = _viewModel.LoadActiveQuestAsync();
                if (host.IsActive)
                {
                    host.RefreshActiveQuestWidget();
                }
                return HomePromptResult.Dismissed;
            }

            private static RewardPresentationItem UnlockedQuestCard(Quest quest)
            {
                if (quest == null)
                {
                    return null;
                }

                return new RewardPresentationItem
                {
                    Type = RewardType.QuestUnlocked,
                    Title = "@UI:QUEST_UNLOCKED_TITLE",
                    Subtitle = quest.GetDisplayName(),
                    IconEmoji = "🔓",
                    RawId = quest.code ?? quest.id
                };
            }
        }

        private sealed class RefreshOnly : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;

            public RefreshOnly(HomeScreenViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                _ = _viewModel.LoadActiveQuestAsync();
                host.RefreshActiveQuestWidget();
                return Task.FromResult(HomePromptResult.Dismissed);
            }
        }
    }
}
