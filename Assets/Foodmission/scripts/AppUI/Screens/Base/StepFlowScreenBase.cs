using System;
using System.ComponentModel;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
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

        // ── Customization Settings ──────────────────────────
        protected virtual bool AllowSwipeGestures => false;
        protected virtual Direction FlowDirection => Direction.Horizontal;
        protected virtual string NextButtonLabel => "@UI:TXT_NEXT";
        protected virtual string PreviousButtonLabel => "@UI:TXT_BACK";
        protected virtual string CompleteButtonLabel => "@UI:TXT_DONE";
        protected virtual string[] StepLabels => Array.Empty<string>();

        // ── Abstract Methods ────────────────────────────────
        protected abstract int StepCount { get; }
        protected abstract VisualElement CreateStepContent(int stepIndex);

        // ── Companion Slot Configuration ────────────────────
        protected virtual void SetupCompanionSlot(VisualElement slot) { }

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

        protected override void OnViewModelUnbinding()
        {
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

            _swipeView.Clear();
            int count = _viewModel.StepCount > 0 ? _viewModel.StepCount : StepCount;
            for (int i = 0; i < count; i++)
            {
                var stepItem = new SwipeViewItem();
                var content = CreateStepContent(i);
                if (content != null)
                {
                    stepItem.Add(content);
                }
                _swipeView.Add(stepItem);
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
                        FMDialog.ShowApiError(this, "Error", _viewModel.ErrorDetail);
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

            OnStepChanged(index);
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
                _btnPrevious.title = PreviousButtonLabel;
                _btnPrevious.SetEnabled(_viewModel.CanGoPrevious);
                _btnPrevious.style.visibility = _viewModel.IsFirstStep ? Visibility.Hidden : Visibility.Visible;
            }

            if (_btnNext != null)
            {
                _btnNext.title = _viewModel.IsLastStep ? CompleteButtonLabel : NextButtonLabel;
                _btnNext.SetEnabled(_viewModel.CanGoNext);
            }

            if (_progressBar != null)
            {
                _progressBar.StepCount = _viewModel.StepCount;
            }
        }

        private void OnSwipeViewValueChanged(ChangeEvent<int> evt)
        {
            if (_viewModel.CurrentStepIndex != evt.newValue)
            {
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
            try
            {
                await _viewModel.GoNextAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error going to next step: {ex.Message}");
            }
        }

        private async void OnPreviousClicked()
        {
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
