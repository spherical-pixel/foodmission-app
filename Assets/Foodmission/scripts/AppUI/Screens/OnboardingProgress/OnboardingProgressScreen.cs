using System;
using System.Collections.Generic;
using System.ComponentModel;

using eu.foodmission.platform.Components;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Onboarding progress (issue #31): Nutri + the 4 onboarding steps with their state. Continue opens the current step,
    /// the first-step Skip skips it, the ✕ leaves the onboarding.
    /// </summary>
    [Preserve]
    public class OnboardingProgressScreen : StepFlowScreenBase<OnboardingProgressViewModel>
    {
        private const string k_StepClass = "fm-onboarding-progress__step";
        private const int k_StepCount = 4;

        private FMNutriView _nutriView;
        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private string _shownMessageKey;
        private readonly List<StepRow> _rows = new List<StepRow>();
        private readonly List<AccessibilityNode> _rowNodes = new List<AccessibilityNode>();

        private class StepRow
        {
            public VisualElement Root;
            public Unity.AppUI.UI.Text Number;
            public Icon Check;
            public Unity.AppUI.UI.Text Title;
            public Unity.AppUI.UI.Text Detail;
        }

        protected override int StepCount => 1;

        protected override bool GrowWithStepContent => true;

        protected override string CompleteButtonLabel => "@UI:TXT_CONTINUE";

        protected override bool ShowSkipOnFirstStep => true;

        protected override string SkipButtonLabel => "@UI:" + OnboardingProgressTexts.SkipStepKey;

        protected override bool ShowCloseButton => true;

        protected override void OnSkipRequested()
        {
            if (_viewModel != null)
            {
                _ = _viewModel.SkipCurrentAsync();
            }
        }

        protected override void OnCloseRequested()
        {
            if (_viewModel == null || _viewModel.IsSubmitting)
            {
                return;
            }
            OnboardingExitDialog.Show(this, OnboardingExitDialog.ProgressMessage, () =>
            {
                ResetNutriToIdle();
                _ = _viewModel.CloseAsync();
            });
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            _viewModel?.Refresh();
            ApplyStates();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnProgressPropertyChanged;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnProgressPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void OnProgressPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OnboardingProgressViewModel.StepStates)
                || e.PropertyName == nameof(OnboardingProgressViewModel.CurrentStep))
            {
                ApplyStates();
            }
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            _nutriView = new FMNutriView();
            _nutriView.AddToClassList("fm-step-flow__guide-nutri");
            slot.Add(_nutriView);

            _messageCard = new ExVisualElement();
            _messageCard.AddToClassList("box-background");
            _messageCard.AddToClassList("fm-shadow-wrapper");
            _messageCard.AddToClassList("fm-step-flow__guide-card");

            _messageText = new Unity.AppUI.UI.Text { text = "" };
            _messageText.style.whiteSpace = WhiteSpace.Normal;
            _messageText.primary = false;
            _messageCard.Add(_messageText);

            _messageCard.style.display = DisplayStyle.None;
            slot.Add(_messageCard);
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            var root = new VisualElement();
            root.AddToClassList("fm-onboarding-progress");

            _rows.Clear();
            for (int i = 0; i < k_StepCount; i++)
            {
                var row = new StepRow { Root = new ExVisualElement() };
                row.Root.AddToClassList("box-background");
                row.Root.AddToClassList("fm-shadow-wrapper");
                row.Root.AddToClassList(k_StepClass);

                var marker = new VisualElement();
                marker.AddToClassList("fm-onboarding-progress__marker");
                row.Number = new Unity.AppUI.UI.Text { text = (i + 1).ToString() };
                row.Number.size = TextSize.L;
                row.Number.AddToClassList("fm-onboarding-progress__number");
                marker.Add(row.Number);
                row.Check = new Icon { iconName = "check" };
                row.Check.AddToClassList("fm-onboarding-progress__check");
                marker.Add(row.Check);
                row.Root.Add(marker);

                var texts = new VisualElement();
                texts.AddToClassList("fm-onboarding-progress__texts");
                row.Title = new Unity.AppUI.UI.Text { text = "@UI:" + OnboardingProgressTexts.StepTitleKey((OnboardingStep)i) };
                row.Title.AddToClassList("fm-onboarding-progress__title");
                texts.Add(row.Title);
                row.Detail = new Unity.AppUI.UI.Text { text = "" };
                row.Detail.AddToClassList("fm-onboarding-progress__detail");
                texts.Add(row.Detail);
                row.Root.Add(texts);

                root.Add(row.Root);
                _rows.Add(row);
            }

            ApplyStates();
            return root;
        }

        private void ApplyStates()
        {
            if (_viewModel == null || _viewModel.StepStates.Count < k_StepCount)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                StepRow row = _rows[i];
                OnboardingStepState state = _viewModel.StepStates[i];
                foreach (OnboardingStepState s in Enum.GetValues(typeof(OnboardingStepState)))
                {
                    row.Root.EnableInClassList(StateClass(s), s == state);
                }
                bool done = state == OnboardingStepState.Done;
                row.Number.style.display = done ? DisplayStyle.None : DisplayStyle.Flex;
                row.Check.style.display = done ? DisplayStyle.Flex : DisplayStyle.None;

                string detail = DetailText((OnboardingStep)i, state);
                row.Detail.text = detail;
                row.Detail.style.display = string.IsNullOrEmpty(detail) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            UpdateNutriMessage();
            RefreshRowNodes();
        }

        private static string StateClass(OnboardingStepState state) => k_StepClass + "--" + state.ToString().ToLowerInvariant();

        private static string DetailText(OnboardingStep step, OnboardingStepState state)
        {
            if (state != OnboardingStepState.Current && state != OnboardingStepState.Pending)
            {
                return string.Empty;
            }
            int? minutes = OnboardingProgressTexts.Minutes(step);
            return minutes.HasValue
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", OnboardingProgressTexts.MinutesKey, new object[] { minutes.Value })
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", OnboardingProgressTexts.OptionalKey);
        }

        private void UpdateNutriMessage()
        {
            if (_messageText == null || _messageCard == null || _viewModel?.CurrentStep == null)
            {
                return;
            }

            string key = OnboardingProgressTexts.NutriMessageKey(_viewModel.CurrentStep.Value);
            if (key == _shownMessageKey)
            {
                return;
            }
            _shownMessageKey = key;
            _messageText.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
            _messageCard.style.display = DisplayStyle.Flex;
            _messageCard.AddToClassList("fm-step-flow__guide-card--visible");
            TriggerNutriSpeech(2.0f);
        }

        // ── Accessibility ──

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null)
            {
                return;
            }

            _rowNodes.Clear();
            for (int i = 0; i < k_StepCount; i++)
            {
                AccessibilityNode node = _accessibilityHierarchy.AddNode(
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", OnboardingProgressTexts.StepTitleKey((OnboardingStep)i)));
                node.role = AccessibilityRole.StaticText;
                int index = i;
                node.frameGetter = () => RowFrame(index);
                _rowNodes.Add(node);
            }
            RefreshRowNodes();
        }

        protected override void TeardownAccessibilityNodes()
        {
            if (_accessibilityHierarchy != null)
            {
                foreach (AccessibilityNode node in _rowNodes)
                {
                    _accessibilityHierarchy.RemoveNode(node);
                }
            }
            _rowNodes.Clear();
            base.TeardownAccessibilityNodes();
        }

        private void RefreshRowNodes()
        {
            if (_viewModel == null || _viewModel.StepStates.Count < k_StepCount)
            {
                return;
            }
            for (int i = 0; i < _rowNodes.Count; i++)
            {
                _rowNodes[i].value = LocalizationSettings.StringDatabase.GetLocalizedString("UI", OnboardingProgressTexts.StateKey(_viewModel.StepStates[i]));
            }
        }

        private Rect RowFrame(int index)
        {
            VisualElement element = index < _rows.Count ? _rows[index].Root : null;
            if (element == null || element.panel == null)
            {
                return Rect.zero;
            }
            Rect rect = element.worldBound;
            float scale = element.panel.scaledPixelsPerPoint;
            return new Rect(rect.position * scale, rect.size * scale);
        }
    }
}
