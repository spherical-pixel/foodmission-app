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

        private bool _isStarting;
        /// <summary>The progress couldn't be read: the mission may be running, so don't offer Start (nor hide its actions).</summary>
        private bool _progressUnknown;

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

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

        public DateTime? StartLocal => MissionProgress?.startedAt?.ToLocalTime();
        public DateTime? DeadlineLocal => StartLocal.HasValue ? MissionDeadlines.DeadlineLocal(StartLocal.Value) : (DateTime?)null;
        /// <summary>Start, end and current day of the 7-day window: only while the user can still act on it.</summary>
        public bool ShowsSchedule => CanAct && StartLocal.HasValue;
        /// <summary>1–7, counted from the start time (not calendar days) so the window always has 7.</summary>
        public int ScheduleDay => StartLocal.HasValue
            ? Math.Clamp((int)Math.Floor((Clock() - StartLocal.Value).TotalDays) + 1, 1, MissionDeadlines.WindowDays)
            : 0;
        public bool IsLastDay => ScheduleDay == MissionDeadlines.WindowDays;

        public bool IsFailed => MissionProgressState.IsFailed(MissionProgress);
        public int ProgressPercent => IsFailed ? 0 : (int)Math.Clamp(MissionProgress?.progress ?? 0f, 0f, 100f);
        public bool IsCompleted => MissionProgressState.IsCompleted(MissionProgress);
        public bool IsPendingRule => Mission != null && Interaction.Status == MissionInteractionStatus.PendingRule;
        /// <summary>Since backend #424 choosing the quest starts nothing: the user starts each mission.</summary>
        public bool IsStarted => _progressUnknown || MissionProgressState.IsStarted(MissionProgress);
        public bool CanStart => Mission != null && IsCurrentQuestMission && !IsPendingRule && !IsStarted;
        public bool IsStarting => _isStarting;
        public bool CanAct => Mission != null && IsStarted && !IsCompleted && !IsFailed && !IsPendingRule && IsCurrentQuestMission;
        public bool ShowsNotCurrentQuest => Mission != null && !IsCompleted && !IsFailed && !IsPendingRule && !IsCurrentQuestMission;
        /// <summary>Rules only run for the current quest, so a failed mission is restarted only from there.</summary>
        public bool CanRestartFailed => Mission != null && IsFailed && IsCurrentQuestMission;
        /// <summary>Only when the backend reports a status (pr-402+): older backends have no restart endpoint.</summary>
        public bool CanRestartActive => CanAct && !string.IsNullOrEmpty(MissionProgress?.status);
        public IReadOnlyList<MissionModuleLink> AutoModules => CanAct ? Interaction.AutoModules : NoModules;
        /// <summary>Buttons under the mission: auto modules first, then helpers (only auto modules get the "advances by itself" hint).</summary>
        public IReadOnlyList<MissionModuleLink> ModuleButtons => CanAct ? Interaction.AutoModules.Concat(Interaction.HelperModules).ToList() : NoModules;
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

                var (progress, progressError) = await _missionService.GetMissionProgressAsync(mission?.code ?? codeOrId);
                _progressUnknown = progressError != null;
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

        /// <summary>
        /// Starts the mission; M.B1.3 sends the meals with meat of last week (0 = meat-free week). On error the progress
        /// is reloaded, since the quest may have changed meanwhile. A second call while one runs is ignored.
        /// </summary>
        public async Task<bool> StartMissionAsync(int? previousMeatMeals)
        {
            string code = Mission?.code;
            if (string.IsNullOrEmpty(code) || !CanStart || _isStarting)
            {
                return false;
            }

            _isStarting = true;
            OnPropertyChanged(nameof(IsStarting));
            ErrorDetail = null;
            try
            {
                var (started, error) = await _missionService.StartMissionProgressAsync(code, MissionInputs.For(code, previousMeatMeals));
                if (error != null)
                {
                    var (reloaded, _) = await _missionService.GetMissionProgressAsync(code);
                    if (reloaded != null)
                    {
                        MissionProgress = reloaded;
                    }
                    ErrorDetail = error;
                    return false;
                }

                if (started != null)
                {
                    MissionProgress = started;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionDetailViewModel] StartMissionAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
                return false;
            }
            finally
            {
                _isStarting = false;
                OnPropertyChanged(nameof(IsStarting));
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// Restarts the failed mission, or gives up and restarts the active one; M.B1.3 sends the meals with meat of
        /// last week for the new attempt. On error the progress is reloaded.
        /// </summary>
        public async Task<bool> RestartMissionAsync(int? previousMeatMeals = null)
        {
            string code = Mission?.code;
            if (_failureService == null || string.IsNullOrEmpty(code) || (!CanRestartFailed && !CanRestartActive))
            {
                return false;
            }

            IsLoading = true;
            ErrorDetail = null;
            try
            {
                var (result, error) = CanRestartFailed
                    ? await _failureService.RestartAsync(MissionProgress, MissionInputs.For(code, previousMeatMeals))
                    : await _failureService.GiveUpAndRestartAsync(code, MissionInputs.For(code, previousMeatMeals));

                if (error != null)
                {
                    // Giving up may have succeeded before the restart failed: show the real state (Restart stays available)
                    var (reloaded, _) = await _missionService.GetMissionProgressAsync(code);
                    if (reloaded != null)
                    {
                        MissionProgress = reloaded;
                    }
                    ErrorDetail = error;
                    return false;
                }

                MissionProgress = result;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionDetailViewModel] RestartMissionAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
                return false;
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
            OnPropertyChanged(nameof(IsStarted));
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanAct));
            OnPropertyChanged(nameof(ShowsNotCurrentQuest));
            OnPropertyChanged(nameof(AutoModules));
            OnPropertyChanged(nameof(ModuleButtons));
            OnPropertyChanged(nameof(LevelKey));
            OnPropertyChanged(nameof(ShowsSchedule));
        }
    }
}
