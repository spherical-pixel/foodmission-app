using System;
using System.ComponentModel;
using eu.foodmission.platform.Components;
using MainraGames;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class MissionDetailScreen : NavigationScreenBase<MissionDetailViewModel>
    {
        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private Unity.AppUI.UI.Text _levelBadge;
        private Unity.AppUI.UI.Text _durationBadge;
        private Unity.AppUI.UI.Text _missionTitle;
        private Image _imageDimensionBanner;
        private Unity.AppUI.UI.Text _missionGoal;
        private Unity.AppUI.UI.Text _missionWhyItMatters;
        private Unity.AppUI.UI.Text _progressLabel;
        private VisualElement _progressFill;

        private UnityEngine.UIElements.Button _tabBtnApp;
        private UnityEngine.UIElements.Button _tabBtnDirect;
        private VisualElement _tabContentApp;
        private VisualElement _tabContentDirect;

        private Unity.AppUI.UI.Text _nativeModuleHint;
        private FMButton _btnGoModule;

        private VisualElement _swapContainer;
        private VisualElement _swapOptionsList;
        private Unity.AppUI.UI.Text _directNutriPrompt;
        private UnityEngine.UIElements.Button _btnStepperMinus;
        private UnityEngine.UIElements.Button _btnStepperPlus;
        private Unity.AppUI.UI.Text _stepperCountLabel;
        private Unity.AppUI.UI.Text _directFeedbackLabel;
        private FMButton _btnSubmitDirect;

        private IBannerService _bannerService;
        private IAudioService _audioService;

        public MissionDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.MissionDetailScreen));

            _bannerService = App.current?.services?.GetService<IBannerService>();
            _audioService = App.current?.services?.GetService<IAudioService>();

            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _levelBadge = contentContainer.Q<Unity.AppUI.UI.Text>("mission-level-badge");
            _durationBadge = contentContainer.Q<Unity.AppUI.UI.Text>("mission-duration-badge");
            _missionTitle = contentContainer.Q<Unity.AppUI.UI.Text>("mission-title");
            _imageDimensionBanner = contentContainer.Q<Image>("image-dimension-banner");
            _missionGoal = contentContainer.Q<Unity.AppUI.UI.Text>("mission-goal");
            _missionWhyItMatters = contentContainer.Q<Unity.AppUI.UI.Text>("mission-why-it-matters");
            _progressLabel = contentContainer.Q<Unity.AppUI.UI.Text>("mission-progress-label");
            _progressFill = contentContainer.Q<VisualElement>("mission-progress-fill");

            _tabBtnApp = contentContainer.Q<UnityEngine.UIElements.Button>("tab-btn-app");
            _tabBtnDirect = contentContainer.Q<UnityEngine.UIElements.Button>("tab-btn-direct");
            _tabContentApp = contentContainer.Q<VisualElement>("tab-content-app");
            _tabContentDirect = contentContainer.Q<VisualElement>("tab-content-direct");

            _nativeModuleHint = contentContainer.Q<Unity.AppUI.UI.Text>("native-module-hint");
            _btnGoModule = contentContainer.Q<FMButton>("btn-go-module");

            _directNutriPrompt = contentContainer.Q<Unity.AppUI.UI.Text>("direct-nutri-prompt");
            _swapContainer = contentContainer.Q<VisualElement>("swap-container");
            _swapOptionsList = contentContainer.Q<VisualElement>("swap-options-list");
            _btnStepperMinus = contentContainer.Q<UnityEngine.UIElements.Button>("btn-stepper-minus");
            _btnStepperPlus = contentContainer.Q<UnityEngine.UIElements.Button>("btn-stepper-plus");
            _stepperCountLabel = contentContainer.Q<Unity.AppUI.UI.Text>("stepper-count-label");
            _directFeedbackLabel = contentContainer.Q<Unity.AppUI.UI.Text>("direct-feedback-label");
            _btnSubmitDirect = contentContainer.Q<FMButton>("btn-submit-direct");

            if (_tabBtnApp != null)
            {
                _tabBtnApp.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.SetTabIndex(0);
                };
            }

            if (_tabBtnDirect != null)
            {
                _tabBtnDirect.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.SetTabIndex(1);
                };
            }

            if (_btnStepperMinus != null)
            {
                _btnStepperMinus.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.DecrementCount();
                };
            }

            if (_btnStepperPlus != null)
            {
                _btnStepperPlus.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.IncrementCount();
                };
            }

            if (_btnGoModule != null)
            {
                _btnGoModule.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.NavigateToNativeModule();
                };
            }

            if (_btnSubmitDirect != null)
            {
                _btnSubmitDirect.clicked += async () =>
                {
                    if (_viewModel == null) return;
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    await _viewModel.SubmitDirectReportAsync();
                };
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string missionCodeOrId = null;
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "code" || a.name == "id")
                    {
                        missionCodeOrId = a.value;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(missionCodeOrId))
            {
                _ = _viewModel?.LoadMissionAsync(missionCodeOrId);
            }

            UpdateView();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            UpdateView();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Mission) ||
                e.PropertyName == nameof(_viewModel.MissionProgress) ||
                e.PropertyName == nameof(_viewModel.Dimension) ||
                e.PropertyName == nameof(_viewModel.Mapping))
            {
                UpdateView();
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedSwapOption))
            {
                RebuildSwapOptions();
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedTabIndex))
            {
                UpdateTabSelection();
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedCount))
            {
                UpdateStepper();
            }
            else if (e.PropertyName == nameof(_viewModel.ReportSuccess) ||
                     e.PropertyName == nameof(_viewModel.SuccessMessage) ||
                     e.PropertyName == nameof(_viewModel.IsReportingDirect))
            {
                UpdateDirectReportState();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
        }

        private void UpdateView()
        {
            if (_viewModel == null) return;

            var mission = _viewModel.Mission;
            if (mission != null)
            {
                if (_missionTitle != null) _missionTitle.text = mission.title ?? string.Empty;
                if (_missionGoal != null) _missionGoal.text = mission.goal ?? string.Empty;
                if (_missionWhyItMatters != null) _missionWhyItMatters.text = mission.whyItMatters ?? string.Empty;

                if (_levelBadge != null)
                {
                    _levelBadge.text = mission.level ?? "BEGINNER";
                }

                if (_durationBadge != null)
                {
                    _durationBadge.text = !string.IsNullOrEmpty(mission.duration)
                        ? $"⏱️ {mission.duration}"
                        : $"⏱️ ";
                }

                if (_imageDimensionBanner != null && _bannerService != null && _viewModel.Dimension != null)
                {
                    _ = _bannerService.BindDimensionBanner(_imageDimensionBanner, _viewModel.Dimension.code);
                }
            }

            var mapping = _viewModel.Mapping;
            if (mapping != null)
            {
                if (_nativeModuleHint != null)
                {
                    _nativeModuleHint.text = mapping.NativeModuleHint ?? string.Empty;
                }
                if (_btnGoModule != null)
                {
                    _btnGoModule.title = mapping.NativeModuleButtonTitle ?? "Ir al módulo";
                }
                if (_directNutriPrompt != null)
                {
                    _directNutriPrompt.text = mapping.DirectQuestionPrompt ?? "¿Has completado esta acción?";
                }
            }

            var progress = _viewModel.MissionProgress;
            int pct = 0;
            if (progress != null)
            {
                pct = (int)Mathf.Clamp(progress.progress, 0, 100);
            }
            if (_progressLabel != null) _progressLabel.text = $"{pct}%";
            if (_progressFill != null) _progressFill.style.width = Length.Percent(pct);

            UpdateTabSelection();
            RebuildSwapOptions();
            UpdateStepper();
            UpdateDirectReportState();
        }

        private void RebuildSwapOptions()
        {
            if (_swapContainer == null || _swapOptionsList == null || _viewModel == null) return;

            var mapping = _viewModel.Mapping;
            if (mapping != null && mapping.QuestionType == DirectQuestionType.SwapSelector &&
                mapping.SwapOptions != null && mapping.SwapOptions.Length > 0)
            {
                _swapContainer.style.display = DisplayStyle.Flex;
                _swapOptionsList.Clear();

                foreach (var swap in mapping.SwapOptions)
                {
                    if (string.IsNullOrEmpty(swap)) continue;

                    var row = new VisualElement();
                    row.AddToClassList("fm-activity-swap-item");
                    bool isSelected = swap == _viewModel.SelectedSwapOption;
                    if (isSelected) row.AddToClassList("fm-activity-swap-item--active");

                    var radio = new VisualElement();
                    radio.AddToClassList("fm-activity-swap-item-radio");
                    if (isSelected) radio.AddToClassList("fm-activity-swap-item-radio--active");
                    row.Add(radio);

                    var label = new Unity.AppUI.UI.Text();
                    label.AddToClassList("fm-activity-swap-item-text");
                    label.text = ActivityEventMapper.GetSwapDisplayName(swap);
                    row.Add(label);

                    string capturedSwap = swap;
                    row.RegisterCallback<ClickEvent>(_ =>
                    {
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        _viewModel?.SelectSwapOption(capturedSwap);
                    });

                    _swapOptionsList.Add(row);
                }
            }
            else
            {
                _swapContainer.style.display = DisplayStyle.None;
            }
        }

        private void UpdateTabSelection()
        {
            if (_viewModel == null) return;

            bool isAppTab = _viewModel.SelectedTabIndex == 0;

            if (_tabBtnApp != null)
            {
                if (isAppTab) _tabBtnApp.AddToClassList("fm-activity-tab-btn--active");
                else _tabBtnApp.RemoveFromClassList("fm-activity-tab-btn--active");
            }

            if (_tabBtnDirect != null)
            {
                if (!isAppTab) _tabBtnDirect.AddToClassList("fm-activity-tab-btn--active");
                else _tabBtnDirect.RemoveFromClassList("fm-activity-tab-btn--active");
            }

            if (_tabContentApp != null)
            {
                _tabContentApp.style.display = isAppTab ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_tabContentDirect != null)
            {
                _tabContentDirect.style.display = !isAppTab ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateStepper()
        {
            if (_viewModel == null || _stepperCountLabel == null) return;
            _stepperCountLabel.text = _viewModel.SelectedCount.ToString();
        }

        private void UpdateDirectReportState()
        {
            if (_viewModel == null) return;

            if (_directFeedbackLabel != null)
            {
                if (_viewModel.ReportSuccess)
                {
                    _directFeedbackLabel.style.display = DisplayStyle.Flex;
                    _directFeedbackLabel.text = _viewModel.SuccessMessage ?? "¡Registrado con éxito!";
                }
                else
                {
                    _directFeedbackLabel.style.display = DisplayStyle.None;
                }
            }

            if (_btnSubmitDirect != null)
            {
                _btnSubmitDirect.SetEnabled(!_viewModel.IsReportingDirect);
            }
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel?.ErrorDetail != null)
            {
                FMDialog.ShowApiError(
                    this,
                    LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "ERROR_TITLE") ?? "Error",
                    _viewModel.ErrorDetail,
                    onOk: () => { }
                );
                _viewModel.ErrorDetail = null;
            }
        }
    }
}
