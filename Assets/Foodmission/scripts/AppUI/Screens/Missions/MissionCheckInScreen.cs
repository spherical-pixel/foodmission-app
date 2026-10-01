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
        private VisualElement _bubbleCard;
        private readonly Dictionary<int, VisualElement> _bodies = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, VisualElement> _cards = new Dictionary<int, VisualElement>();
        private SwipeView _swipeView;
        private const float StepPadding = 64f;

        private static string L(string key) => LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);

        private static string LF(string key, params object[] args) => LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, args);

        private static IFormatProvider Formatter => LocalizationSettings.SelectedLocale?.Formatter ?? CultureInfo.CurrentCulture;

        public MissionCheckInScreen()
        {
            // The page scrolls as a whole; the SwipeView grows to the current step when it doesn't fit (FitSwipeViewToStep)
            _swipeView = contentContainer.Q<SwipeView>("step-swipeview");
            _swipeView?.AddToClassList("fm-mission-report-swipeview");
        }

        /// <summary>A failed load leaves nothing to answer: OK goes back instead of leaving an empty screen.</summary>
        protected override Action OnApiErrorDismissed()
        {
            if (_viewModel == null || !_viewModel.LoadFailed)
            {
                return null;
            }
            NavController navController = _navController;
            return () => navController?.PopBackStack();
        }

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
            _cards.Clear();
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
            nutri.AddToClassList("fm-mission-report-nutri");
            container.Add(nutri);

            var card = new ExVisualElement();
            _bubbleCard = card;
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
            FitSwipeViewToStep(stepIndex);
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
            string message = _viewModel.KindOf(step) == CheckInStepKind.MealDay
                ? LF(_viewModel.NutriPromptKey, DayLabel(_viewModel.Plan.MealDays[_viewModel.ItemIndexOf(step)].Day))
                : L(_viewModel.NutriPromptKey);
            if (_bubbleText.text == message && _bubbleCard != null && _bubbleCard.ClassListContains("fm-step-flow__guide-card--visible"))
            {
                return;
            }

            // The guide card starts transparent (StepFlowScreen.uss): fade out, swap the text, fade in (as OnboardingSurveyScreen)
            _bubbleCard?.RemoveFromClassList("fm-step-flow__guide-card--visible");
            _bubbleCard?.AddToClassList("fm-step-flow__guide-card--exit");
            _bubbleText.schedule.Execute(() =>
            {
                _bubbleText.text = message;
                _bubbleCard?.RemoveFromClassList("fm-step-flow__guide-card--exit");
                _bubbleCard?.AddToClassList("fm-step-flow__guide-card--visible");
            }).StartingIn(150);
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            var root = new VisualElement();
            root.AddToClassList("fm-mission-report-step");
            // The card keeps the hint readable on any theme; RenderStep only clears the inner body
            var card = new VisualElement();
            card.AddToClassList("fm-mission-report-body");
            root.Add(card);
            _cards[stepIndex] = card;
            // SwipeView items share one fixed height: grow the SwipeView to the current step so the page scrolls instead
            card.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (_viewModel != null && _viewModel.CurrentStepIndex == stepIndex)
                {
                    FitSwipeViewToStep(stepIndex);
                }
            });
            if (stepIndex == 0)
            {
                var hint = new Text { text = L("MISSION_REPORT_INTRO") };
                hint.AddToClassList("fm-mission-report-hint");
                card.Add(hint);
            }

            var body = new VisualElement();
            card.Add(body);
            _bodies[stepIndex] = body;

            if (_viewModel != null && stepIndex < _viewModel.SummaryStepIndex)
            {
                RenderStep(stepIndex);
            }
            return root;
        }

        // ── Step rendering ────────────────────────────────────

        private void FitSwipeViewToStep(int stepIndex)
        {
            if (_swipeView == null || !_cards.TryGetValue(stepIndex, out VisualElement card) || float.IsNaN(card.layout.height))
            {
                return;
            }
            // Only grow when the step doesn't fit: a fixed minimum left empty scrollable space under short steps
            _swipeView.style.minHeight = card.layout.height + StepPadding;
        }

        /// <summary>Re-renders on the next frame: rebuilding the step inside a chip's click handler would destroy the clicked button.</summary>
        private void RenderStepLater(int stepIndex)
        {
            schedule.Execute(() => RenderStep(stepIndex));
        }

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
                    RenderStepLater(stepIndex);
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
                        RenderStepLater(stepIndex);
                    }));
                }
                body.Add(questions);

                if (!CheckInBuilder.CanSendMeal(day, mealType, checkedEvents))
                {
                    var emptyHint = new Text { text = L("MISSION_CHECKIN_MEAL_EMPTY_HINT") };
                    emptyHint.AddToClassList("fm-mission-report-meal-hint");
                    body.Add(emptyHint);
                }
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
                    RenderStepLater(stepIndex);
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
                    row.Add(Chip(L("MISSION_REPORT_YES"), answer.Confirmed, () => { _viewModel.SetConfirmed(stepIndex, true); RenderStepLater(stepIndex); }));
                    row.Add(Chip(L("MISSION_REPORT_NO"), !answer.Confirmed, () => { _viewModel.SetConfirmed(stepIndex, false); RenderStepLater(stepIndex); }));
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
            // The UI font has no "✓" glyph: mark selection with App UI's check icon
            var chip = new FMButton
            {
                title = title,
                leadingIcon = selected ? "check" : string.Empty,
                variant = selected ? ButtonVariant.Accent : ButtonVariant.Default,
                size = Size.S
            };
            chip.AddToClassList("fm-mission-report-chip");
            chip.EnableInClassList("fm-mission-report-chip--selected", selected);
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
