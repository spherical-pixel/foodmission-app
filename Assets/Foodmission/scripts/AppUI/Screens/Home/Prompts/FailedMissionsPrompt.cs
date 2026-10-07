using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>Nutri tells each failed mission once and offers to restart it.</summary>
    public sealed class FailedMissionsPrompt : IHomePrompt
    {
        private const int Restart = 0;
        private const int GapMs = 250;

        private readonly HomeScreenViewModel _viewModel;

        public FailedMissionsPrompt(HomeScreenViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public HomePromptKind Kind => HomePromptKind.Info;

        public async Task<IHomePromptInstance> CheckAsync()
        {
            IReadOnlyList<MissionProgress> failures = await _viewModel.CheckFailedMissionsAsync();
            return failures == null || failures.Count == 0 ? null : new Instance(_viewModel, failures);
        }

        private sealed class Instance : IHomePromptInstance
        {
            private readonly HomeScreenViewModel _viewModel;
            private readonly IReadOnlyList<MissionProgress> _failures;

            public Instance(HomeScreenViewModel viewModel, IReadOnlyList<MissionProgress> failures)
            {
                _viewModel = viewModel;
                _failures = failures;
            }

            public async Task<HomePromptResult> ShowAsync(IHomePromptHost host)
            {
                for (int i = 0; i < _failures.Count; i++)
                {
                    if (!host.IsActive)
                    {
                        return HomePromptResult.Navigated;
                    }

                    MissionProgress failed = _failures[i];
                    string title = !string.IsNullOrEmpty(failed.missionTitle) ? failed.missionTitle : failed.missionCode;
                    int choice = await HomePromptDialogs.ChooseNutriAsync(
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MISSION_FAILED_NOTICE", new object[] { title }),
                        new PromptChoice("@UI:MISSION_BTN_RESTART", ButtonVariant.Accent),
                        new PromptChoice("@UI:CHALLENGE_BTN_LATER", ButtonVariant.Default));

                    if (choice == Restart)
                    {
                        await RestartAsync(host, failed);
                    }
                    else
                    {
                        // "Later" or closed: told once
                        _viewModel.AcknowledgeFailedMission(failed);
                    }

                    if (i < _failures.Count - 1)
                    {
                        await Task.Delay(GapMs);
                    }
                }
                return HomePromptResult.Dismissed;
            }

            private async Task RestartAsync(IHomePromptHost host, MissionProgress failed)
            {
                try
                {
                    ApiErrorResponse error = await _viewModel.RestartFailedMissionAsync(failed);
                    if (!host.IsActive)
                    {
                        return;
                    }

                    _ = _viewModel.LoadActiveQuestAsync();
                    host.RefreshActiveQuestWidget();
                    if (error != null)
                    {
                        await HomePromptDialogs.ShowApiErrorAsync(host.DialogAnchor, error);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FailedMissionsPrompt] Restart error: {ex.Message}");
                }
            }
        }
    }
}
