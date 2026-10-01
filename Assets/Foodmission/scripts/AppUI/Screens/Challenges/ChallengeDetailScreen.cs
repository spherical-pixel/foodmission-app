using System;
using System.ComponentModel;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

using eu.foodmission.platform.Components;
using MainraGames;

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

        private Text _levelBadge;
        private Heading _title;
        private Text _task;
        private Text _whyItMatters;
        private VisualElement _card;
        private FMNutriView _nutriView;
        private INutriService _nutriService;
        private IVisualElementScheduledItem _cardSchedule;
        private IVisualElementScheduledItem _nutriSpeechSchedule;
        private static NutriSfxType s_LastTalkSfx = NutriSfxType.Talk1;

        private Text _autoHint;
        private VisualElement _actions;
        private VisualElement _completedBox;
        private FMButton _btnHelper;
        private FMButton _btnDone;
        private FMButton _btnLater;

        private readonly IAudioService _audioService;
        private bool _isLeaving;

        public ChallengeDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.ChallengeDetailScreen));

            _audioService = App.current?.services?.GetService<IAudioService>();
            _nutriService = App.current?.services?.GetService<INutriService>();
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _card = contentContainer.Q<VisualElement>("challenge-card");
            _nutriView = contentContainer.Q<FMNutriView>("nutri-view") ?? contentContainer.Q<FMNutriView>();
            _card?.AddToClassList("fm-challenge-card--entering");

            _levelBadge = contentContainer.Q<Text>("challenge-level-badge");
            _title = contentContainer.Q<Heading>("challenge-title");
            _task = contentContainer.Q<Text>("challenge-task");
            _whyItMatters = contentContainer.Q<Text>("challenge-why-it-matters");
            _autoHint = contentContainer.Q<Text>("challenge-auto-hint");
            _actions = contentContainer.Q<VisualElement>("challenge-actions");
            _completedBox = contentContainer.Q<VisualElement>("challenge-completed-box");
            _btnHelper = contentContainer.Q<FMButton>("btn-helper-module");
            _btnDone = contentContainer.Q<FMButton>("btn-mark-done");
            _btnLater = contentContainer.Q<FMButton>("btn-later");

            if (_btnHelper != null)
            {
                _btnHelper.clicked += OnHelperClicked;
            }
            if (_btnDone != null)
            {
                _btnDone.clicked += OnDoneClicked;
            }
            if (_btnLater != null)
            {
                _btnLater.clicked += OnLaterClicked;
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            _isLeaving = false;

            _nutriView?.RefreshView();
            PlayEntranceAnimation();
            PlayNutriTalking();

            string codeOrId = null;
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "code" || a.name == "id")
                    {
                        codeOrId = a.value;
                        break;
                    }
                }
            }

            // Also runs when coming back from a helper module, so auto-completion is reflected
            if (!string.IsNullOrEmpty(codeOrId))
            {
                _ = _viewModel?.LoadChallengeAsync(codeOrId);
            }

            UpdateView();
        }

        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            ResetNutriToIdle();
            _cardSchedule?.Pause();
            _cardSchedule = null;
            base.OnExit(controller, destination, args);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            TrackLoadingOverlay(() => _viewModel.IsLoading, nameof(ChallengeDetailViewModel.IsLoading));
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            UpdateView();
        }

        protected override void OnViewModelUnbinding()
        {
            ResetNutriToIdle();
            _cardSchedule?.Pause();
            _cardSchedule = null;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void PlayEntranceAnimation()
        {
            if (_card == null)
            {
                return;
            }

            _cardSchedule?.Pause();
            _card.AddToClassList("fm-challenge-card--entering");

            _cardSchedule = _card.schedule.Execute(() =>
            {
                _card.RemoveFromClassList("fm-challenge-card--entering");
                _cardSchedule = null;
            }).StartingIn(50);
        }

        private void PlayNutriTalking(float durationSeconds = 1.5f, long startDelayMs = 100)
        {
            _nutriService ??= App.current?.services?.GetService<INutriService>();
            if (_nutriService == null)
            {
                return;
            }

            _nutriSpeechSchedule?.Pause();
            _nutriSpeechSchedule = null;

            _nutriSpeechSchedule = schedule.Execute(() =>
            {
                NutriSfxType[] candidates = s_LastTalkSfx switch
                {
                    NutriSfxType.Talk1 => new[] { NutriSfxType.Talk2, NutriSfxType.Talk3 },
                    NutriSfxType.Talk2 => new[] { NutriSfxType.Talk1, NutriSfxType.Talk3 },
                    NutriSfxType.Talk3 => new[] { NutriSfxType.Talk1, NutriSfxType.Talk2 },
                    _ => new[] { NutriSfxType.Talk1, NutriSfxType.Talk2, NutriSfxType.Talk3 }
                };

                NutriSfxType sfxType = candidates[UnityEngine.Random.Range(0, candidates.Length)];
                s_LastTalkSfx = sfxType;

                _nutriService.SetAction(NutriAction.Talking);
                _audioService?.PlayNutriSfx(sfxType, 0.5f);

                _nutriSpeechSchedule = schedule.Execute(() =>
                {
                    _nutriService?.SetAction(NutriAction.Idle);
                    _nutriSpeechSchedule = null;
                }).StartingIn((long)(durationSeconds * 1000));
            }).StartingIn(startDelayMs);
        }

        private void ResetNutriToIdle()
        {
            if (_nutriSpeechSchedule != null)
            {
                _nutriSpeechSchedule.Pause();
                _nutriSpeechSchedule = null;
            }

            _nutriService ??= App.current?.services?.GetService<INutriService>();
            _nutriService?.SetAction(NutriAction.Idle);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
            else
            {
                UpdateView();
            }
        }

        private void UpdateView()
        {
            if (_viewModel == null)
            {
                return;
            }

            Challenge challenge = _viewModel.Challenge;
            if (_title != null)
            {
                _title.text = challenge?.title ?? string.Empty;
            }
            if (_task != null)
            {
                _task.text = challenge?.task ?? string.Empty;
            }
            if (_whyItMatters != null)
            {
                _whyItMatters.text = challenge?.whyItMatters ?? string.Empty;
            }
            if (_levelBadge != null)
            {
                string levelKey = _viewModel.LevelKey;
                _levelBadge.text = levelKey != null ? Localize(levelKey) : string.Empty;
                _levelBadge.EnableInClassList("hidden", levelKey == null);
            }

            bool completed = _viewModel.IsCompleted;
            _actions?.EnableInClassList("hidden", completed);
            _completedBox?.EnableInClassList("hidden", !completed);
            _btnDone?.EnableInClassList("hidden", !_viewModel.ShowsDoneButton);
            _btnLater?.EnableInClassList("hidden", !_viewModel.ShowsLaterButton);

            bool hasHelper = _viewModel.HasHelperModule;
            _btnHelper?.EnableInClassList("hidden", !hasHelper);
            if (_btnHelper != null && hasHelper)
            {
                _btnHelper.title = Localize(_viewModel.Interaction.ModuleButtonKey);
            }
            _autoHint?.EnableInClassList("hidden", !_viewModel.ShowsAutoCompleteHint);

            bool actionsEnabled = _viewModel.AreActionsEnabled;
            _btnHelper?.SetEnabled(actionsEnabled);
            _btnDone?.SetEnabled(actionsEnabled);
            _btnLater?.SetEnabled(actionsEnabled);
        }

        private void OnHelperClicked()
        {
            _audioService?.PlaySfx(SfxType.PositiveButton);
            _viewModel?.OpenHelperModule();
        }

        private async void OnDoneClicked()
        {
            if (_viewModel == null || _isLeaving)
            {
                return;
            }

            _audioService?.PlaySfx(SfxType.PositiveButton);

            // The user can still leave through the app bar / system back while the PATCH runs, which unbinds
            // _viewModel and _navController: keep local references so the reward is shown anyway
            ChallengeDetailViewModel viewModel = _viewModel;
            NavController navController = _navController;
            try
            {
                bool completed = await viewModel.MarkCompletedAsync();
                if (!completed)
                {
                    // ErrorDetail shows the API error dialog; stay so the user can retry
                    return;
                }

                bool stillOnScreen = _viewModel == viewModel;
                _isLeaving = true;
                ResetNutriToIdle();
                ContentReward reward = viewModel.EarnedReward;
                if (reward != null)
                {
                    RewardCelebrationDialog.Show(reward, contextTitle: "@UI:CHALLENGE_REWARD_TITLE",
                        onDismiss: () =>
                        {
                            if (stillOnScreen)
                            {
                                navController?.PopBackStack();
                            }
                        });
                }
                else if (stillOnScreen)
                {
                    navController?.PopBackStack();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ChallengeDetailScreen] Done failed: {ex.Message}");
                _isLeaving = false;
            }
        }

        private void OnLaterClicked()
        {
            // "Más tarde": no PATCH, the challenge stays pending in its quest
            ResetNutriToIdle();
            _navController?.PopBackStack();
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel?.ErrorDetail == null)
            {
                return;
            }

            FMDialog.ShowApiError(this, Localize("ERROR_TITLE"), _viewModel.ErrorDetail);
            _viewModel.ErrorDetail = null;
        }

        private static string Localize(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }
    }
}
