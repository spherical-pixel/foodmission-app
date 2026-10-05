using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class MissionDetailViewModel : ViewModelBase
    {
        private static readonly IReadOnlyList<MissionModuleLink> NoModules = Array.Empty<MissionModuleLink>();

        private readonly IMissionService _missionService;
        private readonly IDimensionService _dimensionService;
        private readonly IQuestService _questService;
        private readonly IMissionFailureService _failureService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Mission _mission;

        [ObservableProperty]
        private Dimension _dimension;

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        private MissionInteraction _interaction = MissionInteraction.Unknown;
        public MissionInteraction Interaction
        {
            get => _interaction;
            private set
            {
                if (SetProperty(ref _interaction, value ?? MissionInteraction.Unknown))
                {
                    NotifyStateChanged();
                }
            }
        }

        private MissionProgress _missionProgress;
        public MissionProgress MissionProgress
        {
            get => _missionProgress;
            private set
            {
                if (SetProperty(ref _missionProgress, value))
                {
                    NotifyStateChanged();
                }
            }
        }

        private bool _isCurrentQuestMission;
        public bool IsCurrentQuestMission
        {
            get => _isCurrentQuestMission;
            private set
            {
                if (SetProperty(ref _isCurrentQuestMission, value))
                {
                    NotifyStateChanged();
                }
            }
        }

        public bool IsFailed => MissionProgressState.IsFailed(MissionProgress);
        public int ProgressPercent => IsFailed ? 0 : (int)Math.Clamp(MissionProgress?.progress ?? 0f, 0f, 100f);
        public bool IsCompleted => MissionProgressState.IsCompleted(MissionProgress);
        public bool IsPendingRule => Mission != null && Interaction.Status == MissionInteractionStatus.PendingRule;
        public bool CanAct => Mission != null && !IsCompleted && !IsFailed && !IsPendingRule && IsCurrentQuestMission;
        public bool ShowsNotCurrentQuest => Mission != null && !IsCompleted && !IsFailed && !IsPendingRule && !IsCurrentQuestMission;
        /// <summary>Rules only run for the current quest, so a failed mission is restarted only from there.</summary>
        public bool CanRestartFailed => Mission != null && IsFailed && IsCurrentQuestMission;
        /// <summary>Only when the backend reports a status (pr-402+): older backends have no restart endpoint.</summary>
        public bool CanRestartActive => CanAct && !string.IsNullOrEmpty(MissionProgress?.status);
        public IReadOnlyList<MissionModuleLink> AutoModules => CanAct ? Interaction.AutoModules : NoModules;
        public IReadOnlyList<MissionModuleLink> HelperModules => CanAct ? Interaction.HelperModules : NoModules;
        /// <summary>UI.csv key for the level badge (shared with challenges); null when unknown.</summary>
        public string LevelKey => string.IsNullOrEmpty(Mission?.level) ? null : $"CHALLENGE_LEVEL_{Mission.level.Trim().ToUpperInvariant()}";

        public MissionDetailViewModel(
            IStoreService storeService,
            IMissionService missionService,
            IDimensionService dimensionService,
            IQuestService questService,
            IMissionFailureService failureService) : base(storeService)
        {
            _failureService = failureService;
            _missionService = missionService;
            _dimensionService = dimensionService;
            _questService = questService;
        }

        public async Task LoadMissionAsync(string codeOrId, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return;
            }

            IsLoading = true;
            ErrorDetail = null;
            try
            {
                if (_dimensionService != null && (!_dimensionService.IsLoaded || forceRefresh))
                {
                    await _dimensionService.PreloadAsync(force: forceRefresh);
                }

                var (mission, missionError) = await _missionService.GetMissionAsync(codeOrId);
                if (missionError != null)
                {
                    ErrorDetail = missionError;
                    return;
                }

                Mission = mission;
                Interaction = MissionInteractionCatalog.Get(mission?.code ?? codeOrId);

                var (progress, _) = await _missionService.GetMissionProgressAsync(mission?.code ?? codeOrId);
                MissionProgress = progress;

                IsCurrentQuestMission = await IsInCurrentQuestAsync(mission?.code ?? codeOrId);
                if (IsFailed)
                {
                    // Seeing the failed card here is being told: Home won't announce it again
                    _failureService?.Acknowledge(MissionProgress);
                }

                if (_dimensionService != null && !string.IsNullOrEmpty(mission?.dimensionId))
                {
                    Dimension = _dimensionService.GetDimension(mission.dimensionId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionDetailViewModel] LoadMissionAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public void OpenModule(MissionModuleLink module)
        {
            if (!CanAct || module == null || string.IsNullOrEmpty(module.Action))
            {
                return;
            }
            RaiseNavigationRequested(module.Action);
        }

        public void OpenCheckIn()
        {
            if (!CanAct || !Interaction.CanReport)
            {
                return;
            }
            RaiseNavigationRequested(Actions.open_mission_checkin, new Argument("code", Mission.code));
        }

        private async Task<bool> IsInCurrentQuestAsync(string missionCode)
        {
            string questId = _storeService?.GetAppState()?.userCurrentQuestId;
            if (string.IsNullOrEmpty(questId) || _questService == null || string.IsNullOrEmpty(missionCode))
            {
                return false;
            }

            var (quest, _) = await _questService.GetQuestAsync(questId);
            return quest?.items?.Any(i =>
                i != null &&
                string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.contentCode, missionCode, StringComparison.OrdinalIgnoreCase)) == true;
        }

        /// <summary>Restarts the failed mission, or gives up and restarts the active one. On error the progress is reloaded.</summary>
        public async Task RestartMissionAsync()
        {
            string code = Mission?.code;
            if (_failureService == null || string.IsNullOrEmpty(code) || (!CanRestartFailed && !CanRestartActive))
            {
                return;
            }

            IsLoading = true;
            ErrorDetail = null;
            try
            {
                var (result, error) = CanRestartFailed
                    ? await _failureService.RestartAsync(MissionProgress)
                    : await _failureService.GiveUpAndRestartAsync(code);

                if (error != null)
                {
                    // Giving up may have succeeded before the restart failed: show the real state (Restart stays available)
                    var (reloaded, _) = await _missionService.GetMissionProgressAsync(code);
                    if (reloaded != null)
                    {
                        MissionProgress = reloaded;
                    }
                    ErrorDetail = error;
                    return;
                }

                MissionProgress = result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionDetailViewModel] RestartMissionAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(CanRestartFailed));
            OnPropertyChanged(nameof(CanRestartActive));
            OnPropertyChanged(nameof(IsPendingRule));
            OnPropertyChanged(nameof(CanAct));
            OnPropertyChanged(nameof(ShowsNotCurrentQuest));
            OnPropertyChanged(nameof(AutoModules));
            OnPropertyChanged(nameof(HelperModules));
            OnPropertyChanged(nameof(LevelKey));
        }
    }
}
