using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class DimensionLevelRow
    {
        public string DimensionCode;
        public string DimensionName;
        public string Level;
        public string ProposedLevel;
    }

    /// <summary>One-step flow: proposal of per-dimension levels after the survey, or editing them later.</summary>
    public partial class DimensionLevelsViewModel : StepFlowViewModelBase
    {
        private bool m_IsSubmitting;
        public bool IsSubmitting
        {
            get => m_IsSubmitting;
            set => SetProperty(ref m_IsSubmitting, value);
        }

        private readonly IAuthService _authService;
        private readonly IDimensionService _dimensionService;
        private readonly List<DimensionLevelRow> _rows = new List<DimensionLevelRow>();

        public DimensionLevelsMode Mode { get; set; } = DimensionLevelsMode.Edit;
        public bool FromHome { get; set; }

        public IReadOnlyList<DimensionLevelRow> Rows => _rows;

        public bool IsDifferentFromProposal => _rows.Any(r => r.Level != r.ProposedLevel);

        public event Action RowsChanged;

        public DimensionLevelsViewModel(
            IStoreService storeService,
            IAuthService authService = null,
            IDimensionService dimensionService = null) : base(storeService)
        {
            _authService = authService;
            _dimensionService = dimensionService;
        }

        public override void Initialize()
        {
            base.Initialize();
            Reload();
        }

        /// <summary>Rebuilds the rows for the current Mode (the screen sets Mode after Initialize).</summary>
        public void Reload()
        {
            AppState state = _storeService.GetAppState();
            DimensionLevelEntry[] proposal = DimensionLevels.Propose(state?.userSegment);
            DimensionLevelEntry[] current = Mode == DimensionLevelsMode.Proposal ? proposal : DimensionLevels.Resolve(state);

            _rows.Clear();
            for (int i = 0; i < DimensionCode.All.Length; i++)
            {
                string code = DimensionCode.All[i];
                _rows.Add(new DimensionLevelRow
                {
                    DimensionCode = code,
                    DimensionName = _dimensionService?.GetDimension(code)?.name ?? code,
                    Level = current[i].level,
                    ProposedLevel = proposal[i].level
                });
            }

            // StepFlowViewModelBase validated the step in Initialize, before the rows existed
            RefreshStepState();
            RowsChanged?.Invoke();
        }

        /// <summary>Loads the dimension catalogue if needed (new users reach this screen early) and replaces code fallbacks with the localized names.</summary>
        public async Task EnsureDimensionNamesAsync()
        {
            if (_dimensionService == null)
            {
                return;
            }

            if (!_dimensionService.IsLoaded)
            {
                try
                {
                    await _dimensionService.PreloadAsync(_storeService.GetAppState()?.lang);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DimensionLevelsViewModel] EnsureDimensionNamesAsync: {ex.Message}");
                    return;
                }
            }

            bool changed = false;
            foreach (DimensionLevelRow row in _rows)
            {
                string name = _dimensionService.GetDimension(row.DimensionCode)?.name;
                if (!string.IsNullOrEmpty(name) && name != row.DimensionName)
                {
                    row.DimensionName = name;
                    changed = true;
                }
            }

            if (changed)
            {
                RowsChanged?.Invoke();
            }
        }

        public void SetLevel(string dimensionCode, string level)
        {
            string normalized = ContentLevel.Normalize(level);
            DimensionLevelRow row = _rows.FirstOrDefault(r => string.Equals(r.DimensionCode, dimensionCode, StringComparison.OrdinalIgnoreCase));
            if (row == null || normalized == null || row.Level == normalized)
            {
                return;
            }

            row.Level = normalized;
            RowsChanged?.Invoke();
        }

        public void ResetToProposal()
        {
            foreach (DimensionLevelRow row in _rows)
            {
                row.Level = row.ProposedLevel;
            }
            RowsChanged?.Invoke();
        }

        public async Task SaveAsync()
        {
            if (IsSubmitting)
            {
                return;
            }

            IsSubmitting = true;
            ErrorDetail = null;
            try
            {
                DimensionLevelEntry[] entries = _rows.Select(r => new DimensionLevelEntry(r.DimensionCode, r.Level)).ToArray();
                AppState state = _storeService.GetAppState();

                if (_authService != null)
                {
                    var request = new ProfileUpdateRequest
                    {
                        preferences = new ProfileUpdatePreferences
                        {
                            dimensionLevels = DimensionLevels.ToMap(entries),
                            // Non-nullable bool: always serialized, so send the user's value back
                            autoAddToPantry = state.userAutoAddToPantry
                        }
                    };

                    var (success, error) = await _authService.UpdateProfileAsync(request);
                    if (!success)
                    {
                        ErrorDetail = error ?? new ApiErrorResponse { statusCode = 500, error = "COULD_NOT_SAVE_LEVELS", message = "Could not save levels." };
                        return;
                    }
                }

                _storeService.store.Dispatch(AppActions.setDimensionLevels.Invoke(entries));
                NavigateAfterSave();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DimensionLevelsViewModel] SaveAsync exception: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { statusCode = 500, error = "COULD_NOT_SAVE_LEVELS", message = ex.Message };
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private void NavigateAfterSave()
        {
            if (Mode == DimensionLevelsMode.Edit)
            {
                RaiseNavigationRequested("popBackStack");
            }
            else if (FromHome)
            {
                RaiseNavigationRequested(Actions.go_to_home);
            }
            else
            {
                RaiseNavigationRequested(Actions.onboardingprofile_to_onboardingavatar, new Unity.AppUI.Navigation.Argument("fromOnboarding", "true"));
            }
        }

        /// <summary>
        /// ✕: Edit goes back; the proposal leaves for Home without saving (unsaved levels resolve to the
        /// survey's segment, which is exactly the proposal).
        /// </summary>
        public void Close()
        {
            RaiseNavigationRequested(Mode == DimensionLevelsMode.Edit ? "popBackStack" : Actions.go_to_home);
        }

        // ── StepFlow: a single step whose completion saves ──

        protected override int GetStepCount() => 1;
        protected override bool ValidateStep(int stepIndex) => _rows.Count == DimensionCode.All.Length;
        protected override string GetStepTitle(int stepIndex) => "";
        protected override Task OnStepEnteredAsync(int stepIndex) => Task.CompletedTask;
        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;
        protected override Task OnFlowCompletedAsync() => SaveAsync();
    }
}
