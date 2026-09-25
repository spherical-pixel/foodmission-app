using System;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ChallengeDetailViewModel : ViewModelBase
    {
        private readonly IChallengeService _challengeService;
        private readonly IDimensionService _dimensionService;
        private readonly IEventService _eventService;
        private readonly IActivityEventMapper _activityEventMapper;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Challenge _challenge;

        [ObservableProperty]
        private ChallengeProgress _challengeProgress;

        [ObservableProperty]
        private ActivityMapping _mapping;

        [ObservableProperty]
        private Dimension _dimension;

        [ObservableProperty]
        private int _selectedTabIndex; // 0: Native App Module, 1: Direct Report

        [ObservableProperty]
        private int _selectedCount = 1;

        [ObservableProperty]
        private string _selectedSwapOption = "";

        [ObservableProperty]
        private bool _isReportingDirect;

        [ObservableProperty]
        private bool _reportSuccess;

        [ObservableProperty]
        private string _successMessage = "";

        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        public ChallengeDetailViewModel(
            IStoreService storeService,
            IChallengeService challengeService,
            IDimensionService dimensionService,
            IEventService eventService,
            IActivityEventMapper activityEventMapper) : base(storeService)
        {
            _challengeService = challengeService;
            _dimensionService = dimensionService;
            _eventService = eventService;
            _activityEventMapper = activityEventMapper;
        }

        public async Task LoadChallengeAsync(string codeOrId, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(codeOrId)) return;

            IsLoading = true;
            ErrorDetail = null;
            ReportSuccess = false;
            SuccessMessage = "";

            try
            {
                if (_dimensionService != null && (!_dimensionService.IsLoaded || forceRefresh))
                {
                    await _dimensionService.PreloadAsync(force: forceRefresh);
                }

                var (challenge, challengeErr) = await _challengeService.GetChallengeAsync(codeOrId);
                if (challengeErr != null)
                {
                    ErrorDetail = challengeErr;
                    return;
                }
                Challenge = challenge;

                var (progress, _) = await _challengeService.GetChallengeProgressAsync(codeOrId);
                ChallengeProgress = progress;

                if (_activityEventMapper != null && Challenge != null)
                {
                    Mapping = _activityEventMapper.GetChallengeMapping(Challenge.code);
                    SelectedCount = Mapping?.DefaultCount ?? 1;
                    if (Mapping?.SwapOptions != null && Mapping.SwapOptions.Length > 0)
                    {
                        SelectedSwapOption = Mapping.SwapOptions[0];
                    }
                }

                if (_dimensionService != null && Challenge != null && !string.IsNullOrEmpty(Challenge.dimensionId))
                {
                    Dimension = _dimensionService.GetDimension(Challenge.dimensionId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadChallengeAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void SetTabIndex(int index)
        {
            SelectedTabIndex = Math.Clamp(index, 0, 1);
        }

        public void IncrementCount()
        {
            int max = Mapping?.MaxCount ?? 7;
            SelectedCount = Math.Min(SelectedCount + 1, max);
        }

        public void DecrementCount()
        {
            SelectedCount = Math.Max(SelectedCount - 1, 0);
        }

        public void SelectSwapOption(string swap)
        {
            SelectedSwapOption = swap ?? "";
        }

        public void NavigateToNativeModule()
        {
            if (Mapping == null || string.IsNullOrEmpty(Mapping.NativeModuleAction)) return;
            RaiseNavigationRequested(Mapping.NativeModuleAction);
        }

        public async Task<bool> SubmitDirectReportAsync()
        {
            if (Challenge == null || Mapping == null) return false;
            if (_eventService == null) return false;

            IsReportingDirect = true;
            ReportSuccess = false;
            ErrorDetail = null;

            try
            {
                string targetEventType = ClientEventTypes.MealLogged;
                if (Mapping.QuestionType == DirectQuestionType.SwapSelector && !string.IsNullOrEmpty(SelectedSwapOption))
                {
                    targetEventType = SelectedSwapOption;
                }
                else if (Mapping.TargetEventTypes != null && Mapping.TargetEventTypes.Length > 0)
                {
                    targetEventType = Mapping.TargetEventTypes[0];
                }

                var request = new CreateClientEventRequest
                {
                    eventType = targetEventType,
                    metadata = new
                    {
                        challengeCode = Challenge.code,
                        challengeId = Challenge.id,
                        count = SelectedCount,
                        sessionId = _eventService.CurrentSessionId,
                        reportedVia = "direct_nutri_dialog"
                    }
                };

                var (result, err) = await _eventService.RecordClientEventAsync(request);
                if (err != null)
                {
                    ErrorDetail = err;
                    return false;
                }

                ReportSuccess = true;
                SuccessMessage = "¡Reto completado con éxito!";

                // Reload challenge progress to reflect backend evaluator update
                if (!string.IsNullOrEmpty(Challenge.code))
                {
                    var (updatedProgress, _) = await _challengeService.GetChallengeProgressAsync(Challenge.code);
                    if (updatedProgress != null)
                    {
                        ChallengeProgress = updatedProgress;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SubmitDirectReportAsync error: {ex.Message}");
                return false;
            }
            finally
            {
                IsReportingDirect = false;
            }
        }
    }
}
