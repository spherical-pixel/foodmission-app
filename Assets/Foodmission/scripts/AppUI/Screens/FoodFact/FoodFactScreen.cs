using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using eu.foodmission.platform.Components;
using MainraGames;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class FoodFactScreen : NavigationScreenBase<FoodFactScreenViewModel>
    {
        protected override bool IsFixedContent => true;
        protected override bool ApplySafeAreaTop => true;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;

        private FMButton _btContinue;
        private IconButton _btShare;
        private AccessibilityNode _btContinueNode;
        private AccessibilityNode _btShareNode;
        private VisualElement _factCard;
        private Image _imageTopic;
        private Text _textBody;
        private FMSourceItemView _sourceItemView;

        private IAudioService _audioService;
        private IDimensionService _dimensionService;
        private IBannerService _bannerService;

        public FoodFactScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodFactScreen));

            _audioService = App.current?.services?.GetService<IAudioService>();
            _dimensionService = App.current?.services?.GetService<IDimensionService>();
            _bannerService = App.current?.services?.GetService<IBannerService>();

            CacheUIElements();
            RegisterManualEvents();
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            TrackLoadingOverlay(() => _viewModel.IsLoading, nameof(FoodFactScreenViewModel.IsLoading));

            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "code" || a.name == "id")
                    {
                        RequestLoadFoodFact(a.value);
                        break;
                    }
                }
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
            }



            //_audioService?.PlayMusic(MusicType.Quiz);
            ShowCard(500, () =>
            {
                _audioService?.PlaySfx(SfxType.OpenMessage);
            });
        }

        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
            }

            //_audioService?.StopMusic();

            base.OnExit(controller, destination, args);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.FoodFactData))
            {
                RebuildFoodFactCard();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
        }

        private void CacheUIElements()
        {
            _btContinue = contentContainer.Q<FMButton>("bt-continue");
            _btShare = contentContainer.Q<IconButton>("bt-share");
            _factCard = contentContainer.Q<VisualElement>("fact-card");
            _factCard?.AddToClassList("fm-foodfact-card--hidden");

            _imageTopic = contentContainer.Q<Image>("image-topic");
            _textBody = contentContainer.Q<Text>("text-body");

            _sourceItemView = contentContainer.Q<FMSourceItemView>("source-item-view");
            if (_sourceItemView != null)
            {
                _sourceItemView.ShowPrefix = false;
            }

        }

        private void RegisterManualEvents()
        {
            if (_btContinue != null)
            {
                _btContinue.clicked += OnContinueClicked;
            }

            if (_btShare != null)
            {
                _btShare.clicked += OnShareClicked;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _btShare.clickable.longPressDuration = 800;
                _btShare.clickable.longClicked += OnShareLongPressed;
#endif
            }
        }

        private void UnregisterManualEvents()
        {
            if (_btContinue != null)
            {
                _btContinue.clicked -= OnContinueClicked;
            }

            if (_btShare != null)
            {
                _btShare.clicked -= OnShareClicked;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _btShare.clickable.longClicked -= OnShareLongPressed;
#endif
            }
        }

        private async void OnShareClicked()
        {
            if (_viewModel == null)
            {
                return;
            }
            bool opened = await _viewModel.ShareAsync();
            if (!opened)
            {
                Debug.LogError("[FoodFactScreen] Share sheet did not open");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Dev only: shares the text with the topic banner to test the image path on device.
        private async void OnShareLongPressed()
        {
            if (_viewModel?.FoodFactData == null || _bannerService == null)
            {
                return;
            }
            Sprite banner = await _bannerService.LoadTopicBannerAsync(_viewModel.FoodFactData.topicId);
            bool opened = await _viewModel.ShareAsync(banner);
            Debug.Log($"[FoodFactScreen] Dev share with image -> {opened}");
        }
#endif

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null)
            {
                return;
            }
            _btContinueNode = CreateButtonNode(_accessibilityHierarchy, _btContinue, _btContinue?.title, OnContinueClicked);
            _btShareNode = CreateButtonNode(_accessibilityHierarchy, _btShare,
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHARE"), OnShareClicked);
        }

        protected override void TeardownAccessibilityNodes()
        {
            _btContinueNode = null;
            _btShareNode = null;
            base.TeardownAccessibilityNodes();
        }

        // Invokes the handler directly: App UI buttons do not react to NavigationSubmitEvent.
        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label, Action onInvoke)
        {
            if (button == null)
            {
                return null;
            }
            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;
            if (!button.enabledSelf)
            {
                node.state = AccessibilityState.Disabled;
            }
            node.frameGetter = () =>
            {
                if (button.panel == null)
                {
                    return Rect.zero;
                }
                var r = button.worldBound;
                var s = button.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
            node.invoked += () =>
            {
                if (!button.enabledInHierarchy)
                {
                    return false;
                }
                onInvoke?.Invoke();
                return true;
            };
            return node;
        }

        private async void OnContinueClicked()
        {
            _audioService?.PlaySfx(SfxType.PositiveButton);
            _btContinue?.SetEnabled(false);

            ContentReward reward = null;
            if (_viewModel != null)
            {
                reward = await _viewModel.MarkAsReadAsync();
            }

            void NavigateOut()
            {
                HideCard(() =>
                {
                    if (_navController != null)
                    {
                        _navController.PopBackStack();
                    }
                    else
                    {
                        OnNavigationRequested(Actions.go_to_home, null);
                    }
                });
            }

            if (reward != null)
            {
                RewardCelebrationDialog.Show(
                    reward,
                    contextTitle: "@UI:FOOD_FACT_REWARD_TITLE",
                    onDismiss: NavigateOut);
            }
            else
            {
                NavigateOut();
            }
        }

        private void ShowCard(long delay, Action onComplete = null)
        {
            if (_factCard == null) return;

            _factCard.schedule.Execute(() =>
            {
                _factCard.RemoveFromClassList("fm-foodfact-card--hidden");
            }).StartingIn(delay);

            if (onComplete != null)
            {
                _factCard.schedule.Execute(onComplete).StartingIn(400 + delay);
            }
        }

        private void HideCard(Action onComplete = null)
        {
            if (_factCard == null)
            {
                onComplete?.Invoke();
                return;
            }

            _factCard.AddToClassList("fm-foodfact-card--hidden");

            if (onComplete != null)
            {
                _factCard.schedule.Execute(onComplete).StartingIn(400);
            }
        }

        private async void RequestLoadFoodFact(string codeOrId)
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadFoodFactDataByCodeOrId(codeOrId);
            }
        }

        private void RebuildFoodFactCard()
        {
            var fact = _viewModel?.FoodFactData;
            if (fact == null) return;

            if (_textBody != null)
            {
                _textBody.text = fact.body ?? "";
            }

            if (_sourceItemView != null)
            {
                _sourceItemView.SetSource(fact.source);
            }

            if (_imageTopic != null)
            {
                _imageTopic.style.width = Length.Percent(100);
                _imageTopic.style.height = StyleKeyword.Auto;
                _ = _bannerService?.BindTopicBanner(_imageTopic, fact.topicId);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();
            base.OnViewModelUnbinding();
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel?.ErrorDetail != null)
            {
                FMDialog.ShowApiError(
                    this,
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"),
                    _viewModel.ErrorDetail,
                    onOk: () =>
                    {
                        if (_navController != null)
                        {
                            _navController.PopBackStack();
                        }
                        else
                        {
                            OnNavigationRequested(Actions.go_to_home, null);
                        }
                    }
                );
                _viewModel.ErrorDetail = null;
            }
        }
    }
}
