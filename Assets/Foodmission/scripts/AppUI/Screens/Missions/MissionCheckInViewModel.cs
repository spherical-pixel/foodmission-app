using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Nutri check-in (spec §3.9): meal days, day events, mission steps, summary.</summary>
    [ObservableObject]
    public partial class MissionCheckInViewModel : StepFlowViewModelBase
    {
        private const string MealDayPromptKey = "MISSION_CHECKIN_Q_MEALS";
        private const string SummaryPromptKey = "MISSION_REPORT_SUMMARY_TITLE";

        private readonly ICheckInService _checkInService;
        private readonly IMissionEventEmitter _emitter;
        private readonly IMissionService _missionService;

        private List<PendingReportItem> _pendingItems;
        private string _reportId = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSubmitting;

        [ObservableProperty]
        private bool _isUpToDate;

        [ObservableProperty]
        private string _nutriPromptKey;

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
        public string OnlyMissionCode { get; private set; }
        public CheckInPlan Plan { get; private set; } = CheckInPlan.Empty;
        public CheckInAnswers Answers { get; private set; } = CheckInAnswers.For(CheckInPlan.Empty);
        public int SummaryStepIndex => Plan.StepCount;
        public bool HasPartialSend => _pendingItems != null && _pendingItems.Any(i => i.Sent);

        public event Action<CheckInOutcome> CheckInCompleted;

        public MissionCheckInViewModel(
            IStoreService storeService,
            ICheckInService checkInService,
            IMissionEventEmitter emitter,
            IMissionService missionService) : base(storeService)
        {
            _checkInService = checkInService;
            _emitter = emitter;
            _missionService = missionService;
        }

        public async Task LoadAsync(string onlyMissionCode)
        {
            IsLoading = true;
            ErrorDetail = null;
            OnlyMissionCode = onlyMissionCode;
            try
            {
                var (plan, error) = await _checkInService.LoadPlanAsync(onlyMissionCode);
                if (error != null)
                {
                    ErrorDetail = error;
                    return;
                }

                Plan = plan ?? CheckInPlan.Empty;
                Answers = CheckInAnswers.For(Plan);
                IsUpToDate = Plan.IsEmpty;
                _reportId = Guid.NewGuid().ToString("N");
                _pendingItems = null;
                StepCount = GetStepCount();
                CurrentStepIndex = 0;
                NutriPromptKey = PromptKeyFor(0);
                RefreshStepState();
                RequestRebuildSteps();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionCheckInViewModel] LoadAsync error: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsLoading = false;
            }
        }

        public CheckInStepKind KindOf(int stepIndex)
        {
            if (stepIndex < Plan.MealDays.Count)
            {
                return CheckInStepKind.MealDay;
            }
            if (stepIndex < Plan.MealDays.Count + Plan.DayEvents.Count)
            {
                return CheckInStepKind.DayEvent;
            }
            return stepIndex < Plan.StepCount ? CheckInStepKind.MissionStep : CheckInStepKind.Summary;
        }

        public int ItemIndexOf(int stepIndex) => KindOf(stepIndex) switch
        {
            CheckInStepKind.MealDay => stepIndex,
            CheckInStepKind.DayEvent => stepIndex - Plan.MealDays.Count,
            CheckInStepKind.MissionStep => stepIndex - Plan.MealDays.Count - Plan.DayEvents.Count,
            _ => -1
        };

        public bool IsStepValid(int stepIndex) => ValidateStep(stepIndex);

        public IReadOnlyList<CheckInSummaryLine> BuildSummary()
        {
            var lines = new List<CheckInSummaryLine>();
            for (int step = 0; step < Plan.StepCount; step++)
            {
                int count = BuildFor(step).Count;
                if (count == 0)
                {
                    continue;
                }

                int index = ItemIndexOf(step);
                switch (KindOf(step))
                {
                    case CheckInStepKind.MealDay:
                        lines.Add(new CheckInSummaryLine(CheckInStepKind.MealDay, null, Plan.MealDays[index].Day, count));
                        break;
                    case CheckInStepKind.DayEvent:
                        lines.Add(new CheckInSummaryLine(CheckInStepKind.DayEvent, Plan.DayEvents[index].Step.PromptKey, null, count));
                        break;
                    case CheckInStepKind.MissionStep:
                        lines.Add(new CheckInSummaryLine(CheckInStepKind.MissionStep, Plan.MissionSteps[index].Step.PromptKey, null, count));
                        break;
                }
            }
            return lines;
        }

        // ── Mutators ──────────────────────────────────────────

        public void ToggleMeal(int stepIndex, string mealType)
        {
            if (!CanEdit(stepIndex, CheckInStepKind.MealDay) || !Plan.MealDays[stepIndex].OpenMealTypes.Contains(mealType))
            {
                return;
            }

            Dictionary<string, HashSet<string>> meals = Answers.MealDays[stepIndex].Meals;
            if (!meals.Remove(mealType))
            {
                meals[mealType] = new HashSet<string>();
            }
            Changed();
        }

        public void ToggleMealEvent(int stepIndex, string mealType, string eventType)
        {
            if (!CanEdit(stepIndex, CheckInStepKind.MealDay) ||
                !Answers.MealDays[stepIndex].Meals.TryGetValue(mealType, out HashSet<string> checkedEvents) ||
                !Plan.MealDays[stepIndex].QuestionsFor(mealType).Any(q => q.EventType == eventType))
            {
                return;
            }

            if (!checkedEvents.Remove(eventType))
            {
                checkedEvents.Add(eventType);
                if (eventType == ClientEventTypes.MealMeatFree)
                {
                    checkedEvents.Remove(ClientEventTypes.MealMeatConsumed);
                }
                else if (eventType == ClientEventTypes.MealMeatConsumed)
                {
                    checkedEvents.Remove(ClientEventTypes.MealMeatFree);
                }
            }
            Changed();
        }

        public void ToggleEventDay(int stepIndex, DateTime day)
        {
            if (!CanEdit(stepIndex, CheckInStepKind.DayEvent))
            {
                return;
            }

            int index = ItemIndexOf(stepIndex);
            CheckInDayEvent dayEvent = Plan.DayEvents[index];
            HashSet<DateTime> days = Answers.DayEvents[index];
            if (!dayEvent.OpenDays.Contains(day.Date))
            {
                return;
            }
            if (!days.Remove(day.Date) && days.Count < dayEvent.Step.MaxCount)
            {
                days.Add(day.Date);
            }
            Changed();
        }

        public void SetCount(int stepIndex, int value)
        {
            if (TryMissionStep(stepIndex, MissionStepType.Count, out MissionReportStep step, out MissionStepAnswer answer))
            {
                answer.Count = Math.Clamp(value, 0, step.MaxCount);
                Changed();
            }
        }

        public void SetConfirmed(int stepIndex, bool value)
        {
            if (TryMissionStep(stepIndex, MissionStepType.YesNo, out _, out MissionStepAnswer answer))
            {
                answer.Confirmed = value;
                Changed();
            }
        }

        public void SetOptionCount(int stepIndex, int optionIndex, int value)
        {
            if (!TryMissionStep(stepIndex, MissionStepType.OptionPicker, out MissionReportStep step, out MissionStepAnswer answer) ||
                optionIndex < 0 || optionIndex >= step.Options.Count)
            {
                return;
            }

            int clamped = Math.Clamp(value, 0, step.MaxCount);
            if (clamped == 0)
            {
                answer.OptionCounts.Remove(optionIndex);
            }
            else
            {
                answer.OptionCounts[optionIndex] = clamped;
            }
            Changed();
        }

        // ── StepFlow ──────────────────────────────────────────

        protected override int GetStepCount() => Plan.IsEmpty ? 0 : Plan.StepCount + 1;

        protected override bool ValidateStep(int stepIndex)
        {
            if (stepIndex == SummaryStepIndex)
            {
                return !IsSubmitting && CurrentItems().Count > 0;
            }
            return stepIndex >= 0 && stepIndex < Plan.StepCount;
        }

        protected override string GetStepTitle(int stepIndex) => string.Empty;

        protected override Task OnStepEnteredAsync(int stepIndex)
        {
            NutriPromptKey = PromptKeyFor(stepIndex);
            return Task.CompletedTask;
        }

        protected override Task OnStepExitingAsync(int stepIndex) => Task.CompletedTask;

        protected override async Task OnFlowCompletedAsync()
        {
            if (IsSubmitting)
            {
                return;
            }

            _pendingItems = CurrentItems();
            if (_pendingItems.Count == 0)
            {
                return;
            }

            IsSubmitting = true;
            ErrorDetail = null;
            try
            {
                MissionReportSendResult result = await _emitter.SendAsync(_pendingItems);
                if (!result.Success)
                {
                    ErrorDetail = result.Error;
                    return;
                }

                MissionProgress progress = null;
                if (!string.IsNullOrEmpty(OnlyMissionCode) && _missionService != null)
                {
                    (progress, _) = await _missionService.GetMissionProgressAsync(OnlyMissionCode);
                }
                CheckInCompleted?.Invoke(new CheckInOutcome(_pendingItems.Count, progress));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionCheckInViewModel] Send failed: {ex.Message}");
                ErrorDetail = new ApiErrorResponse { message = ex.Message };
            }
            finally
            {
                IsSubmitting = false;
                RefreshStepState();
            }
        }

        // ── Helpers ───────────────────────────────────────────

        private List<PendingReportItem> CurrentItems() =>
            _pendingItems ?? CheckInBuilder.Build(Plan, Answers, _reportId, Clock());

        /// <summary>Items one step would produce on its own (summary counts).</summary>
        private List<PendingReportItem> BuildFor(int stepIndex)
        {
            int index = ItemIndexOf(stepIndex);
            var onePlan = KindOf(stepIndex) switch
            {
                CheckInStepKind.MealDay => new CheckInPlan(new List<CheckInMealDay> { Plan.MealDays[index] }, new List<CheckInDayEvent>(), new List<CheckInMissionStep>()),
                CheckInStepKind.DayEvent => new CheckInPlan(new List<CheckInMealDay>(), new List<CheckInDayEvent> { Plan.DayEvents[index] }, new List<CheckInMissionStep>()),
                _ => new CheckInPlan(new List<CheckInMealDay>(), new List<CheckInDayEvent>(), new List<CheckInMissionStep> { Plan.MissionSteps[index] })
            };
            var oneAnswers = CheckInAnswers.For(onePlan);
            switch (KindOf(stepIndex))
            {
                case CheckInStepKind.MealDay:
                    oneAnswers.MealDays[0] = Answers.MealDays[index];
                    break;
                case CheckInStepKind.DayEvent:
                    oneAnswers.DayEvents[0] = Answers.DayEvents[index];
                    break;
                default:
                    oneAnswers.MissionSteps[0] = Answers.MissionSteps[index];
                    break;
            }
            return CheckInBuilder.Build(onePlan, oneAnswers, _reportId, Clock());
        }

        private string PromptKeyFor(int stepIndex) => KindOf(stepIndex) switch
        {
            CheckInStepKind.MealDay => MealDayPromptKey,
            CheckInStepKind.DayEvent => Plan.DayEvents[ItemIndexOf(stepIndex)].Step.PromptKey,
            CheckInStepKind.MissionStep => Plan.MissionSteps[ItemIndexOf(stepIndex)].Step.PromptKey,
            _ => SummaryPromptKey
        };

        private bool CanEdit(int stepIndex, CheckInStepKind kind) =>
            !HasPartialSend && !IsSubmitting && stepIndex >= 0 && stepIndex < Plan.StepCount && KindOf(stepIndex) == kind;

        private bool TryMissionStep(int stepIndex, MissionStepType type, out MissionReportStep step, out MissionStepAnswer answer)
        {
            step = null;
            answer = null;
            if (!CanEdit(stepIndex, CheckInStepKind.MissionStep))
            {
                return false;
            }

            int index = ItemIndexOf(stepIndex);
            step = Plan.MissionSteps[index].Step;
            answer = Answers.MissionSteps[index];
            return step.Type == type;
        }

        private void Changed()
        {
            _pendingItems = null;
            InvalidateValidation();
            OnPropertyChanged(nameof(Answers));
        }
    }
}
