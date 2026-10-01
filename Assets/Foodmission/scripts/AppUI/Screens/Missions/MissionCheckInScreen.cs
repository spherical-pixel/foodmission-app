using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    [Preserve]
    public class MissionCheckInScreen : StepFlowScreenBase<MissionCheckInViewModel>
    {
        protected override int StepCount => _viewModel != null && _viewModel.StepCount > 0 ? _viewModel.StepCount : 1;
        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:MISSION_REPORT_SEND";

        private Text _bubbleText;
        private readonly Dictionary<int, VisualElement> _bodies = new Dictionary<int, VisualElement>();

        private static string L(string key) => LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);

        private static string LF(string key, params object[] args) => LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, args);

        private static IFormatProvider Formatter => LocalizationSettings.SelectedLocale?.Formatter ?? CultureInfo.CurrentCulture;

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            MissionCheckInViewModel viewModel = _viewModel;
            if (viewModel == null)
            {
                return;
            }

            string code = args?.FirstOrDefault(a => a.name == "code")?.value;
            await viewModel.LoadAsync(string.IsNullOrEmpty(code) ? null : code);
            // The user may have gone back while the plan was loading
            if (_viewModel != viewModel || panel == null)
            {
                return;
            }
            if (viewModel.IsUpToDate)
            {
                NavController navController = _navController;
                NutriMessageDialog.Show(L("MISSION_CHECKIN_UP_TO_DATE"),
                    new FMDialogAction("@UI:MISSION_REPORT_CLOSE", () => navController?.PopBackStack(), ButtonVariant.Accent));
                return;
            }
            OnStepChanged(_viewModel.CurrentStepIndex);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            TrackLoadingOverlay(() => _viewModel.IsLoading || _viewModel.IsSubmitting,
                nameof(MissionCheckInViewModel.IsLoading), nameof(MissionCheckInViewModel.IsSubmitting));
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.CheckInCompleted += OnCheckInCompleted;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.CheckInCompleted -= OnCheckInCompleted;
            }
            _bodies.Clear();
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MissionCheckInViewModel.NutriPromptKey))
            {
                UpdateBubble();
            }
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            // Same guide layout as OnboardingSurveyScreen
            var container = new VisualElement();
            var nutri = new FMNutriView();
            nutri.AddToClassList("fm-step-flow__guide-nutri");
            container.Add(nutri);

            var card = new ExVisualElement();
            card.AddToClassList("box-background");
            card.AddToClassList("fm-shadow-wrapper");
            card.AddToClassList("fm-step-flow__guide-card");
            _bubbleText = new Text { text = string.Empty, primary = false };
            _bubbleText.AddToClassList("fm-mission-report-bubble");
            card.Add(_bubbleText);
            container.Add(card);
            slot.Add(container);
        }

        protected override void OnStepChanged(int stepIndex)
        {
            base.OnStepChanged(stepIndex);
            UpdateBubble();
            if (_viewModel != null && _viewModel.KindOf(stepIndex) == CheckInStepKind.Summary)
            {
                RenderSummary(stepIndex);
            }
            TriggerNutriSpeech();
        }

        private void UpdateBubble()
        {
            if (_bubbleText == null || _viewModel == null || string.IsNullOrEmpty(_viewModel.NutriPromptKey))
            {
                return;
            }

            int step = _viewModel.CurrentStepIndex;
            _bubbleText.text = _viewModel.KindOf(step) == CheckInStepKind.MealDay
                ? LF(_viewModel.NutriPromptKey, DayLabel(_viewModel.Plan.MealDays[_viewModel.ItemIndexOf(step)].Day))
                : L(_viewModel.NutriPromptKey);
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            var root = new VisualElement();
            root.AddToClassList("fm-mission-report-step");
            if (stepIndex == 0)
            {
                var hint = new Text { text = L("MISSION_REPORT_INTRO") };
                hint.AddToClassList("fm-mission-report-hint");
                root.Add(hint);
            }

            var body = new VisualElement();
            body.AddToClassList("fm-mission-report-body");
            root.Add(body);
            _bodies[stepIndex] = body;

            if (_viewModel != null && stepIndex < _viewModel.SummaryStepIndex)
            {
                RenderStep(stepIndex);
            }
            return root;
        }

        // ── Step rendering ────────────────────────────────────

        private void RenderStep(int stepIndex)
        {
            if (!_bodies.TryGetValue(stepIndex, out VisualElement body) || _viewModel == null)
            {
                return;
            }

            body.Clear();
            int index = _viewModel.ItemIndexOf(stepIndex);
            switch (_viewModel.KindOf(stepIndex))
            {
                case CheckInStepKind.MealDay:
                    RenderMealDay(stepIndex, _viewModel.Plan.MealDays[index], _viewModel.Answers.MealDays[index], body);
                    break;
                case CheckInStepKind.DayEvent:
                    RenderDayEvent(stepIndex, _viewModel.Plan.DayEvents[index], _viewModel.Answers.DayEvents[index], body);
                    break;
                case CheckInStepKind.MissionStep:
                    RenderMissionStep(stepIndex, _viewModel.Plan.MissionSteps[index], _viewModel.Answers.MissionSteps[index], body);
                    break;
            }
        }

        private void RenderMealDay(int stepIndex, CheckInMealDay day, CheckInMealDayAnswer answer, VisualElement body)
        {
            var mealRow = new VisualElement();
            mealRow.AddToClassList("fm-mission-report-row");
            foreach (string mealType in day.OpenMealTypes)
            {
                string captured = mealType;
                mealRow.Add(Chip(L("TYPE_" + mealType), answer.Meals.ContainsKey(mealType), () =>
                {
                    _viewModel.ToggleMeal(stepIndex, captured);
                    RenderStep(stepIndex);
                }));
            }
            body.Add(mealRow);

            foreach (string mealType in CheckInMealTypes.All)
            {
                if (!answer.Meals.TryGetValue(mealType, out HashSet<string> checkedEvents))
                {
                    continue;
                }

                var title = new Text { text = L("TYPE_" + mealType) };
                title.AddToClassList("fm-mission-report-meal-title");
                body.Add(title);

                var questions = new VisualElement();
                questions.AddToClassList("fm-mission-report-row");
                foreach (CheckInMealQuestion question in day.QuestionsFor(mealType))
                {
                    string mealCaptured = mealType;
                    string eventCaptured = question.EventType;
                    questions.Add(Chip(L(question.LabelKey), checkedEvents.Contains(question.EventType), () =>
                    {
                        _viewModel.ToggleMealEvent(stepIndex, mealCaptured, eventCaptured);
                        RenderStep(stepIndex);
                    }));
                }
                body.Add(questions);
            }
        }

        private void RenderDayEvent(int stepIndex, CheckInDayEvent dayEvent, HashSet<DateTime> selected, VisualElement body)
        {
            var row = new VisualElement();
            row.AddToClassList("fm-mission-report-row");
            foreach (DateTime day in dayEvent.OpenDays)
            {
                DateTime captured = day;
                row.Add(Chip(DayLabel(day), selected.Contains(day), () =>
                {
                    _viewModel.ToggleEventDay(stepIndex, captured);
                    RenderStep(stepIndex);
                }));
            }
            body.Add(row);
        }

        private void RenderMissionStep(int stepIndex, CheckInMissionStep missionStep, MissionStepAnswer answer, VisualElement body)
        {
            var missionTitle = new Text { text = missionStep.MissionTitle };
            missionTitle.AddToClassList("fm-mission-report-meal-title");
            body.Add(missionTitle);

            MissionReportStep step = missionStep.Step;
            switch (step.Type)
            {
                case MissionStepType.Count:
                    body.Add(Stepper(answer.Count, step.MaxCount, v => _viewModel.SetCount(stepIndex, v)));
                    break;
                case MissionStepType.YesNo:
                    var row = new VisualElement();
                    row.AddToClassList("fm-mission-report-row");
                    row.Add(Chip(L("MISSION_REPORT_YES"), answer.Confirmed, () => { _viewModel.SetConfirmed(stepIndex, true); RenderStep(stepIndex); }));
                    row.Add(Chip(L("MISSION_REPORT_NO"), !answer.Confirmed, () => { _viewModel.SetConfirmed(stepIndex, false); RenderStep(stepIndex); }));
                    body.Add(row);
                    break;
                case MissionStepType.OptionPicker:
                    for (int o = 0; o < step.Options.Count; o++)
                    {
                        int optionIndex = o;
                        var optionRow = new VisualElement();
                        optionRow.AddToClassList("fm-mission-report-row");
                        var label = new Text { text = L(step.Options[o].LabelKey) };
                        label.AddToClassList("fm-mission-report-label");
                        optionRow.Add(label);
                        answer.OptionCounts.TryGetValue(o, out int current);
                        optionRow.Add(Stepper(current, step.MaxCount, v => _viewModel.SetOptionCount(stepIndex, optionIndex, v)));
                        body.Add(optionRow);
                    }
                    break;
            }
        }

        private void RenderSummary(int stepIndex)
        {
            if (!_bodies.TryGetValue(stepIndex, out VisualElement body))
            {
                return;
            }

            body.Clear();
            foreach (CheckInSummaryLine line in _viewModel.BuildSummary())
            {
                string text = line.Kind == CheckInStepKind.MealDay
                    ? LF("MISSION_CHECKIN_SUMMARY_MEALS", DayLabel(line.Day.Value), line.Count)
                    : LF("MISSION_REPORT_SUMMARY_LINE", L(line.PromptKey), line.Count);
                var row = new Text { text = text };
                row.AddToClassList("fm-mission-report-label");
                body.Add(row);
            }
        }

        // ── Controls ──────────────────────────────────────────

        private static FMButton Chip(string title, bool selected, Action onClick)
        {
            var chip = new FMButton { title = title, variant = selected ? ButtonVariant.Accent : ButtonVariant.Default, size = Size.S };
            chip.AddToClassList("fm-mission-report-chip");
            chip.clicked += onClick;
            return chip;
        }

        private static FMArrowStepper Stepper(int value, int max, Action<int> onChanged)
        {
            var stepper = new FMArrowStepper { Choices = Enumerable.Range(0, max + 1).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray() };
            stepper.SelectedIndex = Math.Clamp(value, 0, max);
            stepper.valueChanged += (_, e) => onChanged(e.newValue);
            return stepper;
        }

        private string DayLabel(DateTime day)
        {
            DateTime today = _viewModel.Clock().Date;
            return day.Date == today ? L("MISSION_REPORT_DAY_TODAY") : day.ToString("dddd d", Formatter);
        }

        // ── Completion ────────────────────────────────────────

        private void OnCheckInCompleted(CheckInOutcome outcome)
        {
            ResetNutriToIdle();
            NavController navController = _navController;
            MissionProgress progress = outcome.SingleMissionProgress;
            string message = progress == null
                ? L("MISSION_CHECKIN_RESULT")
                : progress.completed
                    ? L("MISSION_REPORT_RESULT_COMPLETED")
                    : LF("MISSION_REPORT_RESULT_PROGRESS", Mathf.RoundToInt(progress.progress));

            NutriMessageDialog.Show(message,
                new FMDialogAction("@UI:MISSION_REPORT_CLOSE", () => navController?.PopBackStack(), ButtonVariant.Accent));
        }
    }
}
