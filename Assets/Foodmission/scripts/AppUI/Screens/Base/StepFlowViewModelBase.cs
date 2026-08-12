using System;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Abstract ViewModel base class that manages step state, validation, and navigation for multi-step flows.
    /// </summary>
    [ObservableObject]
    public abstract partial class StepFlowViewModelBase : ViewModelBase
    {
        // ── Observable State ──────────────────────────────────
        [ObservableProperty] private int m_CurrentStepIndex;
        [ObservableProperty] private int m_StepCount;
        [ObservableProperty] private bool m_CanGoNext;
        [ObservableProperty] private bool m_CanGoPrevious;
        [ObservableProperty] private bool m_IsLastStep;
        [ObservableProperty] private bool m_IsFirstStep;
        [ObservableProperty] private string m_StepTitle;
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;


        protected StepFlowViewModelBase(IStoreService storeService) : base(storeService)
        {
        }

        // ── Abstract & Virtual Methods (subclasses implement) ──
        protected abstract int GetStepCount();
        protected abstract bool ValidateStep(int stepIndex);
        protected virtual bool ValidateStep(int stepIndex, bool showError) => ValidateStep(stepIndex);
        protected abstract string GetStepTitle(int stepIndex);
        protected abstract Task OnStepEnteredAsync(int stepIndex);
        protected abstract Task OnStepExitingAsync(int stepIndex);
        protected abstract Task OnFlowCompletedAsync();
        protected virtual Task OnFlowCancelledAsync() => Task.CompletedTask;

        // ── Initialization ────────────────────────────────────
        public virtual void Initialize()
        {
            StepCount = GetStepCount();
            CurrentStepIndex = 0;
            RefreshStepState();
            
            // Fire enter step async safely
            SafeEnterStepAsync(0);
        }

        private async void SafeEnterStepAsync(int stepIndex)
        {
            try
            {
                await OnStepEnteredAsync(stepIndex);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[StepFlowViewModelBase] Error entering step {stepIndex}: {ex.Message}");
            }
        }

        public void RefreshStepState()
        {
            IsFirstStep = CurrentStepIndex == 0;
            IsLastStep = CurrentStepIndex == StepCount - 1;
            CanGoPrevious = !IsFirstStep;
            CanGoNext = ValidateStep(CurrentStepIndex, false);
            StepTitle = GetStepTitle(CurrentStepIndex);
        }

        public void InvalidateValidation()
        {
            RefreshStepState();
        }

        public void RequestRebuildSteps()
        {
            OnPropertyChanged(nameof(StepCount));
        }

        // ── Public Navigation Methods ─────────────────────────
        public async Task GoNextAsync()
        {
            if (!ValidateStep(CurrentStepIndex, true))
            {
                return;
            }

            await OnStepExitingAsync(CurrentStepIndex);

            if (IsLastStep)
            {
                await OnFlowCompletedAsync();
            }
            else
            {
                CurrentStepIndex++;
                RefreshStepState();
                await OnStepEnteredAsync(CurrentStepIndex);
            }
        }

        public async Task GoPreviousAsync()
        {
            if (IsFirstStep)
            {
                return;
            }

            await OnStepExitingAsync(CurrentStepIndex);

            CurrentStepIndex--;
            RefreshStepState();
            await OnStepEnteredAsync(CurrentStepIndex);
        }

        public async Task GoToStepAsync(int index)
        {
            if (index < 0 || index >= StepCount || index == CurrentStepIndex)
            {
                return;
            }

            if (index > CurrentStepIndex)
            {
                for (int i = CurrentStepIndex; i < index; i++)
                {
                    if (!ValidateStep(i, true))
                    {
                        return; // blocked by validation on intermediate steps
                    }
                }
            }

            await OnStepExitingAsync(CurrentStepIndex);
            CurrentStepIndex = index;
            RefreshStepState();
            await OnStepEnteredAsync(CurrentStepIndex);
        }

        public async void CancelFlow()
        {
            try
            {
                await OnFlowCancelledAsync();
                RaiseNavigationRequested("popBackStack", Array.Empty<Argument>());
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[StepFlowViewModelBase] Error cancelling flow: {ex.Message}");
            }
        }
    }
}
