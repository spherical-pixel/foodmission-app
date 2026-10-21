using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
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
    /// Abstract screen base class for multi-step flows.
    /// Manages the layout containing SwipeView, FMStepProgressBar, companion slot, and navigation buttons.
    /// </summary>
    /// <typeparam name="TViewModel">The StepFlowViewModelBase subclass associated with this screen</typeparam>
    [Preserve]
    public abstract class StepFlowScreenBase<TViewModel> : NavigationScreenBase<TViewModel>
        where TViewModel : StepFlowViewModelBase
    {
        protected override bool IsFixedContent => true;

        // ── UI Elements ─────────────────────────────────────
        private SwipeView _swipeView;
        private FMStepProgressBar _progressBar;
        private VisualElement _companionSlot;
        private FMButton _btnPrevious;
        private FMButton _btnNext;
        private ScrollView _bodyScroll;
        private IconButton _btnClose;

        // ── Customization Settings ──────────────────────────
        protected virtual bool AllowSwipeGestures => false;
        protected virtual Direction FlowDirection => Direction.Horizontal;
        protected virtual string NextButtonLabel => "@UI:TXT_NEXT";
        protected virtual string PreviousButtonLabel => "@UI:TXT_BACK";
        protected virtual string CompleteButtonLabel => "@UI:TXT_DONE";
        protected virtual string[] StepLabels => Array.Empty<string>();

        /// <summary>
        /// SwipeView items are absolutely positioned at the SwipeView's own height (overflow hidden), so a step taller
        /// than the body is clipped instead of scrolling. When true, the SwipeView grows to the current step's content
        /// height and step-body-scroll scrolls it. Step content must keep its natural height (flex-shrink: 0).
        /// </summary>
        protected virtual bool GrowWithStepContent => false;

        private readonly Dictionary<int, VisualElement> _stepContents = new Dictionary<int, VisualElement>();

        private AccessibilityNode _previousNode;
        private AccessibilityNode _nextNode;
        private AccessibilityNode _closeNode;

        // ── Abstract Methods ────────────────────────────────
        protected abstract int StepCount { get; }
        protected abstract VisualElement CreateStepContent(int stepIndex);

        // ── Companion Slot Configuration ────────────────────
        protected virtual void SetupCompanionSlot(VisualElement slot) { }

        /// <summary>Action for the API error dialog's OK button (null = just close it).</summary>
        protected virtual Action OnApiErrorDismissed() => null;

        /// <summary>Non-null turns the API error dialog into Retry (this action) / Cancel (OnApiErrorDismissed).</summary>
        protected virtual Action OnApiErrorRetry() => null;

        /// <summary>Shows a ✕ (top right) that leaves the flow from any step. Off by default; each flow opts in.</summary>
        protected virtual bool ShowCloseButton => false;

        /// <summary>
        /// Shows a Skip button in place of the (hidden) Back button on the first step. Read every time the buttons
        /// refresh, so it may depend on navigation arguments. Off by default.
        /// </summary>
        protected virtual bool ShowSkipOnFirstStep => false;

        protected virtual string SkipButtonLabel => "@UI:ONBOARDING_SKIP";

        /// <summary>Called by the first-step Skip button.</summary>
        protected virtual void OnSkipRequested() { }

        /// <summary>Called by the ✕. Default: go back; override to confirm first.</summary>
        protected virtual void OnCloseRequested()
        {
            _navController?.PopBackStack();
        }

        protected StepFlowScreenBase()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.StepFlow));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _swipeView = contentContainer.Q<SwipeView>("step-swipeview");
            _progressBar = contentContainer.Q<FMStepProgressBar>("step-progress");
            _companionSlot = contentContainer.Q<VisualElement>("companion-slot");
            _btnPrevious = contentContainer.Q<FMButton>("btn-previous");
            _btnNext = contentContainer.Q<FMButton>("btn-next");
            _bodyScroll = contentContainer.Q<ScrollView>("step-body-scroll");
            SetupCloseButton();
        }

        private void SetupCloseButton()
        {
            if (!ShowCloseButton)
            {
                return;
            }
            VisualElement root = contentContainer.Q<VisualElement>("root");
            if (root == null)
            {
                return;
            }
            root.AddToClassList("fm-step-flow--closable");
            _btnClose = new IconButton { name = "btn-close", icon = "x", quiet = true, size = Size.L };
            _btnClose.AddToClassList("fm-step-flow__close");
            _btnClose.AddToClassList("fm-icon-button--on-main-bg");
            _btnClose.clicked += OnCloseClicked;
            root.Add(_btnClose);
        }

        private void OnCloseClicked()
        {
            _audioService ??= App.current?.services?.GetService<IAudioService>();
            _audioService?.PlaySfx(SfxType.NegativeButton);
            OnCloseRequested();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_bodyScroll != null)
            {
                _bodyScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                _bodyScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                _bodyScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            if (_swipeView != null)
            {
                _swipeView.swipeable = AllowSwipeGestures;
                _swipeView.direction = FlowDirection;
                _swipeView.RegisterValueChangedCallback(OnSwipeViewValueChanged);
            }

            if (_progressBar != null)
            {
                _progressBar.Labels = StepLabels;
                _progressBar.Mode = StepLabels.Length > 0 ? StepProgressMode.Detailed : StepProgressMode.Compact;
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (_btnNext != null)
            {
                _btnNext.clicked += OnNextClicked;
            }

            if (_btnPrevious != null)
            {
                _btnPrevious.clicked += OnPreviousClicked;
            }

            SetupCompanionSlotInternal();
            BuildSteps();

            _viewModel.Initialize();

            UpdateStepIndex(_viewModel.CurrentStepIndex);
            UpdateNavigationControls();
        }

        private INutriService _nutriService;
        private IAudioService _audioService;
        private IVisualElementScheduledItem _nutriSpeechSchedule;
        private static NutriSfxType s_LastTalkSfx = NutriSfxType.None;

        /// <summary>
        /// Triggers Nutri talking animation and plays a Nutri talking sound effect for the given duration,
        /// then automatically switches Nutri back to Idle.
        /// </summary>
        /// <param name="durationSeconds">Duration in seconds (default 1.5s)</param>
        /// <param name="sfxType">Nutri SFX to play (default NutriSfxType.Talk1)</param>
        protected void TriggerNutriSpeech(float durationSeconds = 1.5f, NutriSfxType sfxType = NutriSfxType.Talk1)
        {
            ResetNutriToIdle();

            _nutriService ??= App.current?.services?.GetService<INutriService>();
            _audioService ??= App.current?.services?.GetService<IAudioService>();

            if (_nutriService == null) return;

            if (sfxType == NutriSfxType.Talk1)
            {
                NutriSfxType[] candidates = s_LastTalkSfx switch
                {
                    NutriSfxType.Talk1 => new[] { NutriSfxType.Talk2, NutriSfxType.Talk3 },
                    NutriSfxType.Talk2 => new[] { NutriSfxType.Talk1, NutriSfxType.Talk3 },
                    NutriSfxType.Talk3 => new[] { NutriSfxType.Talk1, NutriSfxType.Talk2 },
                    _ => new[] { NutriSfxType.Talk1, NutriSfxType.Talk2, NutriSfxType.Talk3 }
                };

                sfxType = candidates[UnityEngine.Random.Range(0, candidates.Length)];
                s_LastTalkSfx = sfxType;
            }

            _nutriService.SetAction(NutriAction.Talking);
            _audioService?.PlayNutriSfx(sfxType, 0.5f);

            _nutriSpeechSchedule = schedule.Execute(() =>
            {
                _nutriService?.SetAction(NutriAction.Idle);
                _nutriSpeechSchedule = null;
            }).StartingIn((long)(durationSeconds * 1000));
        }

        /// <summary>
        /// Cancels any scheduled return to Idle and immediately forces Nutri back to Idle.
        /// </summary>
        protected void ResetNutriToIdle()
        {
            if (_nutriSpeechSchedule != null)
            {
                _nutriSpeechSchedule.Pause();
                _nutriSpeechSchedule = null;
            }

            _nutriService ??= App.current?.services?.GetService<INutriService>();
            _nutriService?.SetAction(NutriAction.Idle);
        }

        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            ResetNutriToIdle();
            base.OnExit(controller, destination, args);
        }

        protected override void OnViewModelUnbinding()
        {
            ResetNutriToIdle();

            if (_swipeView != null)
            {
                _swipeView.UnregisterValueChangedCallback(OnSwipeViewValueChanged);
            }

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            if (_btnNext != null)
            {
                _btnNext.clicked -= OnNextClicked;
            }

            if (_btnPrevious != null)
            {
                _btnPrevious.clicked -= OnPreviousClicked;
            }

            base.OnViewModelUnbinding();
        }

        private void BuildSteps()
        {
            if (_swipeView == null) return;

            int count = _viewModel.StepCount > 0 ? _viewModel.StepCount : StepCount;

            // Use SwipeView's source-items API: it (re)creates the items with their index set. Adding children
            // directly only works once (SwipeView polls its hierarchy a single time), so steps rebuilt later
            // (e.g. after async data loads) all kept index 0 and were stacked at x = 0.
            _swipeView.bindItem = (item, index) =>
            {
                item.Clear();
                var content = CreateStepContent(index);
                if (content != null)
                {
                    item.Add(content);
                    if (GrowWithStepContent)
                    {
                        int stepIndex = index;
                        _stepContents[stepIndex] = content;
                        content.RegisterCallback<GeometryChangedEvent>(_ => FitSwipeViewToStep(stepIndex));
                    }
                }
            };
            var indexes = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                indexes.Add(i);
            }
            _swipeView.sourceItems = indexes;

            if (_viewModel.CurrentStepIndex > 0 && _viewModel.CurrentStepIndex < count)
            {
                _swipeView.SetValueWithoutNotify(_viewModel.CurrentStepIndex);
            }
        }

        private void SetupCompanionSlotInternal()
        {
            if (_companionSlot == null) return;

            _companionSlot.Clear();
            SetupCompanionSlot(_companionSlot);

            if (_companionSlot.childCount > 0)
            {
                _companionSlot.style.display = DisplayStyle.Flex;
            }
            else
            {
                _companionSlot.style.display = DisplayStyle.None;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.CurrentStepIndex):
                    UpdateStepIndex(_viewModel.CurrentStepIndex);
                    break;
                case nameof(_viewModel.StepCount):
                    BuildSteps();
                    UpdateNavigationControls();
                    break;
                case nameof(_viewModel.CanGoNext):
                case nameof(_viewModel.CanGoPrevious):
                case nameof(_viewModel.IsLastStep):
                case nameof(_viewModel.IsFirstStep):
                    UpdateNavigationControls();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    if (_viewModel.ErrorDetail != null)
                    {
                        string errorTitle = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE");
                        Action retry = OnApiErrorRetry();
                        if (retry != null)
                        {
                            FMDialog.ShowApiError(this, errorTitle, _viewModel.ErrorDetail, okLabel: "@UI:TXT_RETRY", onOk: retry, koLabel: "@UI:TXT_CANCEL", onKo: OnApiErrorDismissed() ?? (() => { }));
                        }
                        else
                        {
                            FMDialog.ShowApiError(this, errorTitle, _viewModel.ErrorDetail, onOk: OnApiErrorDismissed());
                        }
                        _viewModel.ErrorDetail = null;
                    }
                    break;
            }
        }

        private void UpdateStepIndex(int index)
        {
            if (_swipeView != null && _swipeView.value != index)
            {
                _swipeView.GoTo(index);
            }

            if (_progressBar != null)
            {
                _progressBar.CurrentStep = index;
            }

            FitSwipeViewToStep(index);
            OnStepChanged(index);
        }

        private void FitSwipeViewToStep(int stepIndex)
        {
            if (!GrowWithStepContent || _swipeView == null || _viewModel == null || stepIndex != _viewModel.CurrentStepIndex)
            {
                return;
            }
            if (!_stepContents.TryGetValue(stepIndex, out VisualElement content) || float.IsNaN(content.layout.height))
            {
                return;
            }

            float height = content.layout.height;
            if (!Mathf.Approximately(_swipeView.resolvedStyle.minHeight.value, height))
            {
                _swipeView.style.minHeight = height;
            }
        }

        /// <summary>
        /// Called whenever the current step index changes.
        /// </summary>
        /// <param name="stepIndex">The new active step index.</param>
        protected virtual void OnStepChanged(int stepIndex) { }

        private void UpdateNavigationControls()
        {
            if (_btnPrevious != null)
            {
                if (IsSkipShown())
                {
                    _btnPrevious.title = SkipButtonLabel;
                    _btnPrevious.SetEnabled(true);
                    _btnPrevious.style.visibility = Visibility.Visible;
                }
                else
                {
                    _btnPrevious.title = PreviousButtonLabel;
                    _btnPrevious.SetEnabled(_viewModel.CanGoPrevious);
                    _btnPrevious.style.visibility = _viewModel.IsFirstStep ? Visibility.Hidden : Visibility.Visible;
                }
            }

            if (_btnNext != null)
            {
                _btnNext.title = _viewModel.IsLastStep ? CompleteButtonLabel : NextButtonLabel;
                _btnNext.SetEnabled(_viewModel.CanGoNext);
            }

            RefreshButtonNodes();

            if (_progressBar != null)
            {
                _progressBar.StepCount = _viewModel.StepCount;
            }
        }

        private void OnSwipeViewValueChanged(ChangeEvent<int> evt)
        {
            if (_viewModel.CurrentStepIndex != evt.newValue)
            {
                ResetNutriToIdle();
                ExecuteGoToStep(evt.newValue);
            }
        }

        private async void ExecuteGoToStep(int index)
        {
            try
            {
                await _viewModel.GoToStepAsync(index);
                UpdateStepIndex(_viewModel.CurrentStepIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error navigating to step {index}: {ex.Message}");
                UpdateStepIndex(_viewModel.CurrentStepIndex);
            }
        }

        private async void OnNextClicked()
        {
            ResetNutriToIdle();
            try
            {
                await _viewModel.GoNextAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error going to next step: {ex.Message}");
            }
        }

        // ── Accessibility ──

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null)
            {
                return;
            }

            _previousNode = CreateButtonNode(_btnPrevious);
            _nextNode = CreateButtonNode(_btnNext);
            _closeNode = CreateButtonNode(_btnClose);
            if (_closeNode != null)
            {
                _closeNode.label = LocalizedLabel("@UI:MISSION_CHECKIN_CLOSE");
            }
            RefreshButtonNodes();
        }

        protected override void TeardownAccessibilityNodes()
        {
            if (_accessibilityHierarchy != null)
            {
                foreach (AccessibilityNode node in new[] { _previousNode, _nextNode, _closeNode })
                {
                    if (node != null)
                    {
                        _accessibilityHierarchy.RemoveNode(node);
                    }
                }
            }
            _previousNode = null;
            _nextNode = null;
            _closeNode = null;
            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(VisualElement button)
        {
            if (button == null)
            {
                return null;
            }
            AccessibilityNode node = _accessibilityHierarchy.AddNode(string.Empty);
            node.role = AccessibilityRole.Button;
            node.frameGetter = () =>
            {
                if (button.panel == null || button.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return Rect.zero;
                }
                Rect rect = button.worldBound;
                float scale = button.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
            node.invoked += () =>
            {
                if (!button.enabledInHierarchy || button.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
                using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
                {
                    evt.target = button;
                    button.SendEvent(evt);
                }
                return true;
            };
            return node;
        }

        /// <summary>Button labels and states follow the buttons (Back becomes Skip on the first step, Next becomes Done).</summary>
        private void RefreshButtonNodes()
        {
            if (_previousNode != null && _btnPrevious != null)
            {
                _previousNode.label = LocalizedLabel(_btnPrevious.title);
                _previousNode.state = _btnPrevious.enabledSelf ? AccessibilityState.None : AccessibilityState.Disabled;
            }
            if (_nextNode != null && _btnNext != null)
            {
                _nextNode.label = LocalizedLabel(_btnNext.title);
                _nextNode.state = _btnNext.enabledSelf ? AccessibilityState.None : AccessibilityState.Disabled;
            }
        }

        private static string LocalizedLabel(string title)
        {
            if (string.IsNullOrEmpty(title) || !title.StartsWith("@UI:"))
            {
                return title ?? string.Empty;
            }
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", title.Substring(4));
        }

        private bool IsSkipShown() => _viewModel != null && _viewModel.IsFirstStep && ShowSkipOnFirstStep;

        /// <summary>Re-evaluates the first-step Skip (e.g. after reading navigation arguments).</summary>
        protected void RefreshNavigationButtons()
        {
            if (_viewModel != null)
            {
                UpdateNavigationControls();
            }
        }

        private async void OnPreviousClicked()
        {
            ResetNutriToIdle();
            if (IsSkipShown())
            {
                OnSkipRequested();
                return;
            }
            try
            {
                await _viewModel.GoPreviousAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error going to previous step: {ex.Message}");
            }
        }
    }
}
