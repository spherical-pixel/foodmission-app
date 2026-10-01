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

        public int ProgressPercent => (int)Math.Clamp(MissionProgress?.progress ?? 0f, 0f, 100f);
        public bool IsCompleted => MissionProgress?.completed == true || (MissionProgress?.progress ?? 0f) >= 100f;
        public bool IsPendingRule => Mission != null && Interaction.Status == MissionInteractionStatus.PendingRule;
        public bool CanAct => Mission != null && !IsCompleted && !IsPendingRule && IsCurrentQuestMission;
        public bool ShowsNotCurrentQuest => Mission != null && !IsCompleted && !IsPendingRule && !IsCurrentQuestMission;
        public IReadOnlyList<MissionModuleLink> AutoModules => CanAct ? Interaction.AutoModules : NoModules;
        public IReadOnlyList<MissionModuleLink> HelperModules => CanAct ? Interaction.HelperModules : NoModules;
        /// <summary>UI.csv key for the level badge (shared with challenges); null when unknown.</summary>
        public string LevelKey => string.IsNullOrEmpty(Mission?.level) ? null : $"CHALLENGE_LEVEL_{Mission.level.Trim().ToUpperInvariant()}";

        public MissionDetailViewModel(
            IStoreService storeService,
            IMissionService missionService,
            IDimensionService dimensionService,
            IQuestService questService) : base(storeService)
        {
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

        private void NotifyStateChanged()
        {
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsPendingRule));
            OnPropertyChanged(nameof(CanAct));
            OnPropertyChanged(nameof(ShowsNotCurrentQuest));
            OnPropertyChanged(nameof(AutoModules));
            OnPropertyChanged(nameof(HelperModules));
            OnPropertyChanged(nameof(LevelKey));
        }
    }
}
