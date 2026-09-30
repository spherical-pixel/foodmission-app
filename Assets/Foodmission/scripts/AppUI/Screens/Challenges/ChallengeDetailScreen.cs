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
    public class ChallengeDetailScreen : NavigationScreenBase<ChallengeDetailViewModel>
    {
        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private Unity.AppUI.UI.Text _levelBadge;

        private Unity.AppUI.UI.Text _challengeTitle;
        private Image _imageDimensionBanner;
        private Unity.AppUI.UI.Text _challengeGoal;
        private Unity.AppUI.UI.Text _challengeWhyItMatters;
        private Unity.AppUI.UI.Text _progressLabel;
        private VisualElement _progressFill;

        private VisualElement _challengeActionBox;
        private VisualElement _challengeCompletedBox;
        private Unity.AppUI.UI.Text _nativeModuleHint;
        private FMButton _btnGoModule;

        private IBannerService _bannerService;
        private IAudioService _audioService;

        public ChallengeDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.ChallengeDetailScreen));

            _bannerService = App.current?.services?.GetService<IBannerService>();
            _audioService = App.current?.services?.GetService<IAudioService>();

            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _levelBadge = contentContainer.Q<Unity.AppUI.UI.Text>("challenge-level-badge");
            _challengeTitle = contentContainer.Q<Unity.AppUI.UI.Text>("challenge-title");
            _imageDimensionBanner = contentContainer.Q<Image>("image-dimension-banner");
            _challengeGoal = contentContainer.Q<Unity.AppUI.UI.Text>("challenge-goal");
            _challengeWhyItMatters = contentContainer.Q<Unity.AppUI.UI.Text>("challenge-why-it-matters");
            _progressLabel = contentContainer.Q<Unity.AppUI.UI.Text>("challenge-progress-label");
            _progressFill = contentContainer.Q<VisualElement>("challenge-progress-fill");

            _challengeActionBox = contentContainer.Q<VisualElement>("challenge-action-box");
            _challengeCompletedBox = contentContainer.Q<VisualElement>("challenge-completed-box");
            _nativeModuleHint = contentContainer.Q<Unity.AppUI.UI.Text>("native-module-hint");
            _btnGoModule = contentContainer.Q<FMButton>("btn-go-module");

            if (_btnGoModule != null)
            {
                _btnGoModule.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.NavigateToNativeModule();
                };
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string challengeCodeOrId = null;
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "code" || a.name == "id")
                    {
                        challengeCodeOrId = a.value;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(challengeCodeOrId))
            {
                _ = _viewModel?.LoadChallengeAsync(challengeCodeOrId);
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
            if (e.PropertyName == nameof(_viewModel.Challenge) ||
                e.PropertyName == nameof(_viewModel.ChallengeProgress) ||
                e.PropertyName == nameof(_viewModel.IsCompleted) ||
                e.PropertyName == nameof(_viewModel.Dimension) ||
                e.PropertyName == nameof(_viewModel.Mapping))
            {
                UpdateView();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
        }

        private void UpdateView()
        {
            if (_viewModel == null) return;

            var challenge = _viewModel.Challenge;
            if (challenge != null)
            {
                if (_challengeTitle != null) _challengeTitle.text = challenge.title ?? string.Empty;
                if (_challengeGoal != null) _challengeGoal.text = challenge.task ?? challenge.title ?? string.Empty;
                if (_challengeWhyItMatters != null) _challengeWhyItMatters.text = challenge.whyItMatters ?? string.Empty;

                if (_levelBadge != null)
                {
                    _levelBadge.text = challenge.level;
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
            }

            var progress = _viewModel.ChallengeProgress;
            bool isCompleted = _viewModel.IsCompleted;
            int pct = isCompleted ? 100 : (progress != null ? (int)Mathf.Clamp(progress.progress, 0, 100) : 0);

            if (_progressLabel != null) _progressLabel.text = $"{pct}%";
            if (_progressFill != null) _progressFill.style.width = Length.Percent(pct);

            if (_challengeActionBox != null)
            {
                _challengeActionBox.style.display = isCompleted ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_challengeCompletedBox != null)
            {
                _challengeCompletedBox.style.display = isCompleted ? DisplayStyle.Flex : DisplayStyle.None;
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
