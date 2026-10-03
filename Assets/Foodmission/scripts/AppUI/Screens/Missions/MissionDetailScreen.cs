using System.Collections.Generic;
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
    /// <summary>Nutri mission detail (spec §3.7): automatic modules, helper modules and "Tell Nutri" (check-in for this mission).</summary>
    [Preserve]
    public class MissionDetailScreen : NavigationScreenBase<MissionDetailViewModel>
    {
        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private Text _levelBadge;
        private Heading _title;
        private Text _goal;
        private Text _whyItMatters;
        private Text _status;
        private Text _autoHint;
        private Text _progressLabel;
        private VisualElement _progressFill;
        private VisualElement _card;
        private VisualElement _actions;
        private VisualElement _autoModules;
        private VisualElement _helperModules;
        private VisualElement _completedBox;
        private VisualElement _failedBox;
        private Text _failedText;
        private FMButton _btnRestartFailed;
        private FMButton _btnRestartActive;
        private FMButton _btnTellNutri;
        private FMButton _btnLater;
        private FMNutriView _nutriView;
        private INutriService _nutriService;
        private IVisualElementScheduledItem _cardSchedule;
        private IVisualElementScheduledItem _nutriSpeechSchedule;
        private static NutriSfxType s_LastTalkSfx = NutriSfxType.Talk1;

        private readonly IAudioService _audioService;

        public MissionDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.MissionDetailScreen));

            _audioService = App.current?.services?.GetService<IAudioService>();
            _nutriService = App.current?.services?.GetService<INutriService>();
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _card = contentContainer.Q<VisualElement>("mission-card");
            _nutriView = contentContainer.Q<FMNutriView>("nutri-view") ?? contentContainer.Q<FMNutriView>();
            _card?.AddToClassList("fm-mission-card--entering");

            _levelBadge = contentContainer.Q<Text>("mission-level-badge");
            _title = contentContainer.Q<Heading>("mission-title");
            _goal = contentContainer.Q<Text>("mission-goal");
            _whyItMatters = contentContainer.Q<Text>("mission-why-it-matters");
            _status = contentContainer.Q<Text>("mission-status");
            _autoHint = contentContainer.Q<Text>("mission-auto-hint");
            _progressLabel = contentContainer.Q<Text>("mission-progress-label");
            _progressFill = contentContainer.Q<VisualElement>("mission-progress-fill");
            _actions = contentContainer.Q<VisualElement>("mission-actions");
            _autoModules = contentContainer.Q<VisualElement>("mission-auto-modules");
            _helperModules = contentContainer.Q<VisualElement>("mission-helper-modules");
            _completedBox = contentContainer.Q<VisualElement>("mission-completed-box");
            _btnTellNutri = contentContainer.Q<FMButton>("btn-tell-nutri");
            _btnLater = contentContainer.Q<FMButton>("btn-later");
            _failedBox = contentContainer.Q<VisualElement>("mission-failed-box");
            _failedText = contentContainer.Q<Text>("mission-failed-text");
            _btnRestartFailed = contentContainer.Q<FMButton>("btn-restart-failed");
            _btnRestartActive = contentContainer.Q<FMButton>("btn-restart-active");

            if (_btnTellNutri != null)
            {
                _btnTellNutri.clicked += OnTellNutriClicked;
            }
            if (_btnLater != null)
            {
                _btnLater.clicked += OnLaterClicked;
            }
            if (_btnRestartFailed != null)
            {
                _btnRestartFailed.clicked += OnRestartFailedClicked;
            }
            if (_btnRestartActive != null)
            {
                _btnRestartActive.clicked += OnRestartActiveClicked;
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

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

            // Also runs when coming back from the check-in or a module, so the progress refreshes
            if (!string.IsNullOrEmpty(codeOrId))
            {
                _ = _viewModel?.LoadMissionAsync(codeOrId);
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
            TrackLoadingOverlay(() => _viewModel.IsLoading, nameof(MissionDetailViewModel.IsLoading));
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
            _card.AddToClassList("fm-mission-card--entering");

            _cardSchedule = _card.schedule.Execute(() =>
            {
                _card.RemoveFromClassList("fm-mission-card--entering");
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

            Mission mission = _viewModel.Mission;
            if (_title != null)
            {
                _title.text = mission?.title ?? string.Empty;
            }
            if (_goal != null)
            {
                _goal.text = mission?.goal ?? string.Empty;
            }
            if (_whyItMatters != null)
            {
                _whyItMatters.text = mission?.whyItMatters ?? string.Empty;
            }
            if (_levelBadge != null)
            {
                string levelKey = _viewModel.LevelKey;
                _levelBadge.text = levelKey != null ? Localize(levelKey) : string.Empty;
                _levelBadge.EnableInClassList("hidden", levelKey == null);
            }

            int pct = _viewModel.ProgressPercent;
            if (_progressLabel != null)
            {
                _progressLabel.text = $"{pct}%";
            }
            if (_progressFill != null)
            {
                _progressFill.style.width = Length.Percent(pct);
            }

            _completedBox?.EnableInClassList("hidden", !_viewModel.IsCompleted);
            _failedBox?.EnableInClassList("hidden", !_viewModel.IsFailed);
            // "You can start it again" only where it can be restarted (the current quest)
            _failedText?.EnableInClassList("hidden", !_viewModel.CanRestartFailed);
            _btnRestartFailed?.EnableInClassList("hidden", !_viewModel.CanRestartFailed);
            _btnRestartActive?.EnableInClassList("hidden", !_viewModel.CanRestartActive);
            _progressLabel?.EnableInClassList("fm-mission-progress-label--failed", _viewModel.IsFailed);
            _actions?.EnableInClassList("hidden", !_viewModel.CanAct);

            string statusKey = _viewModel.IsPendingRule ? "MISSION_NOT_AVAILABLE"
                : _viewModel.ShowsNotCurrentQuest ? "MISSION_NOT_CURRENT_QUEST"
                : _viewModel.IsFailed && !_viewModel.IsCurrentQuestMission ? "MISSION_NOT_CURRENT_QUEST"
                : null;
            if (_status != null)
            {
                _status.text = statusKey != null ? Localize(statusKey) : string.Empty;
                _status.EnableInClassList("hidden", statusKey == null);
            }

            _autoHint?.EnableInClassList("hidden", _viewModel.AutoModules.Count == 0);
            RebuildModuleButtons(_autoModules, _viewModel.AutoModules, ButtonVariant.Accent);
            RebuildModuleButtons(_helperModules, _viewModel.HelperModules, ButtonVariant.Default);
        }

        private void RebuildModuleButtons(VisualElement container, IReadOnlyList<MissionModuleLink> modules, ButtonVariant variant)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();
            foreach (MissionModuleLink module in modules)
            {
                MissionModuleLink captured = module;
                var button = new FMButton
                {
                    title = Localize(module.ButtonKey),
                    variant = variant,
                    size = Size.L,
                    trailingIcon = "fm-arrow-right"
                };
                button.AddToClassList("fm-mission-btn");
                button.AddToClassList("fm-button");
                button.AddToClassList("fm-button-align-left");
                button.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.OpenModule(captured);
                };
                container.Add(button);
            }
        }

        private void OnTellNutriClicked()
        {
            _audioService?.PlaySfx(SfxType.PositiveButton);
            _viewModel?.OpenCheckIn();
        }

        private void OnLaterClicked()
        {
            // "Más tarde" only leaves the screen
            ResetNutriToIdle();
            _navController?.PopBackStack();
        }

        private void OnRestartFailedClicked()
        {
            _audioService?.PlaySfx(SfxType.PositiveButton);
            _ = _viewModel?.RestartMissionAsync();
        }

        private void OnRestartActiveClicked()
        {
            FMDialog.ShowConfirm(
                this,
                Localize("MISSION_RESTART_CONFIRM_TITLE"),
                Localize("MISSION_RESTART_CONFIRM_MESSAGE"),
                () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _ = _viewModel?.RestartMissionAsync();
                },
                confirmLabel: "@UI:MISSION_BTN_RESTART");
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
