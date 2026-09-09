using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    public class QuestDetailScreen : NavigationScreenBase<QuestDetailViewModel>
    {
        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private Unity.AppUI.UI.Text _questTitle;
        private Image _imageDimensionBanner;
        private Unity.AppUI.UI.Text _questDescription;
        private VisualElement _activitiesContainer;
        private FMButton _btnStartQuest;

        private IBannerService _bannerService;
        private IAudioService _audioService;

        public QuestDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.QuestDetailScreen));

            _bannerService = App.current?.services?.GetService<IBannerService>();
            _audioService = App.current?.services?.GetService<IAudioService>();

            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _questTitle = contentContainer.Q<Unity.AppUI.UI.Text>("quest-title");
            _imageDimensionBanner = contentContainer.Q<Image>("image-dimension-banner");
            _questDescription = contentContainer.Q<Unity.AppUI.UI.Text>("quest-description");
            _activitiesContainer = contentContainer.Q<VisualElement>("activities-container");
            _btnStartQuest = contentContainer.Q<FMButton>("btn-start-quest");

            if (_btnStartQuest != null)
            {
                _btnStartQuest.clicked += () =>
                {
                    if (_viewModel == null) return;

                    _audioService?.PlaySfx(SfxType.PositiveButton);

                    if (_viewModel.HasOtherActiveQuest)
                    {
                        ShowChangeQuestConfirmation();
                    }
                    else
                    {
                        _ = _viewModel.StartQuestAsync();
                    }
                };
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string questCodeOrId = null;
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "code" || a.name == "id")
                    {
                        questCodeOrId = a.value;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(questCodeOrId))
            {
                _ = _viewModel?.LoadQuestAsync(questCodeOrId);
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
            if (e.PropertyName == nameof(_viewModel.Activities) ||
                e.PropertyName == nameof(_viewModel.QuestTitle) ||
                e.PropertyName == nameof(_viewModel.QuestDescription) ||
                e.PropertyName == nameof(_viewModel.DimensionName) ||
                e.PropertyName == nameof(_viewModel.DimensionCode) ||
                e.PropertyName == nameof(_viewModel.QuestLevel) ||
                e.PropertyName == nameof(_viewModel.ProgressPercent) ||
                e.PropertyName == nameof(_viewModel.IsCompleted) ||
                e.PropertyName == nameof(_viewModel.CompletedActivitiesCount) ||
                e.PropertyName == nameof(_viewModel.TotalActivitiesCount))
            {
                UpdateView();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
            else if (e.PropertyName == nameof(_viewModel.IsCurrentQuest) ||
                     e.PropertyName == nameof(_viewModel.IsStartingQuest))
            {
                UpdateStartQuestButton();
            }
        }

        private void UpdateView()
        {
            if (_viewModel == null) return;

            // 1. Title at the top
            if (_questTitle != null)
            {
                _questTitle.text = _viewModel.QuestTitle ?? string.Empty;
            }

            // 2. Banner hero image below title
            if (_imageDimensionBanner != null && _bannerService != null && !string.IsNullOrEmpty(_viewModel.DimensionCode))
            {
                _ = _bannerService.BindDimensionBanner(_imageDimensionBanner, _viewModel.DimensionCode);
            }

            // 3. Extended description below banner
            if (_questDescription != null)
            {
                _questDescription.text = _viewModel.QuestDescription ?? string.Empty;
            }

            // 4. Update start quest button state
            UpdateStartQuestButton();

            // 5. Rebuild activities vertical roadmap / timeline
            RebuildActivities();
        }

        private void RebuildActivities()
        {
            if (_activitiesContainer == null) return;
            _activitiesContainer.Clear();

            var activities = _viewModel?.Activities?
                .OrderByDescending(act => act.IsCompleted)
                .ThenBy(act => act.StepIndex)
                .ToList();
            if (activities == null || activities.Count == 0) return;

            for (int i = 0; i < activities.Count; i++)
            {
                var act = activities[i];
                if (act == null) continue;

                var row = new VisualElement();
                row.AddToClassList("fm-quest-timeline-row");

                // Track column (lines + circle node)
                var trackCol = new VisualElement();
                trackCol.AddToClassList("fm-quest-timeline-track");

                // Upper connector line (hidden on the first node)
                if (i > 0)
                {
                    var lineTop = new VisualElement();
                    lineTop.AddToClassList("fm-quest-timeline-line-top");
                    trackCol.Add(lineTop);
                }

                // Lower connector line (hidden on the last node)
                if (i < activities.Count - 1)
                {
                    var lineBottom = new VisualElement();
                    lineBottom.AddToClassList("fm-quest-timeline-line-bottom");
                    trackCol.Add(lineBottom);
                }

                // Node circle
                var node = new VisualElement();
                node.AddToClassList("fm-quest-timeline-node");
                if (act.IsCompleted)
                {
                    node.AddToClassList("fm-quest-timeline-node--completed");
                }
                else
                {
                    node.AddToClassList("fm-quest-timeline-node--pending");
                }
                trackCol.Add(node);
                row.Add(trackCol);

                // Label text
                var label = new Unity.AppUI.UI.Text();
                label.text = act.TimelineDisplayLabel;
                label.AddToClassList("fm-quest-timeline-label");
                row.Add(label);

                // Interactivity & Navigation
                string cType = act.Item?.contentType ?? string.Empty;
                bool isNavigable = string.Equals(cType, QuestContentType.Quiz, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(cType, QuestContentType.FoodFact, StringComparison.OrdinalIgnoreCase);

                if (isNavigable)
                {
                    row.AddToClassList("fm-quest-timeline-row--clickable");
                }

                var capturedAct = act;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.OpenActivity(capturedAct);
                });

                _activitiesContainer.Add(row);
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

        private void UpdateStartQuestButton()
        {
            if (_btnStartQuest == null || _viewModel == null) return;

            if (_viewModel.IsCurrentQuest)
            {
                _btnStartQuest.title = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUEST_ACTIVE") ?? "Misión activa";
                _btnStartQuest.SetEnabled(false);
                _btnStartQuest.variant = ButtonVariant.Default;
            }
            else
            {
                _btnStartQuest.title = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "START_QUEST") ?? "Empezar quest";
                _btnStartQuest.SetEnabled(!_viewModel.IsStartingQuest);
                _btnStartQuest.variant = ButtonVariant.Accent;
            }
        }

        private void ShowChangeQuestConfirmation()
        {
            string message = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUEST_CHANGE_CONFIRM_MSG");
            string confirmLabel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUEST_CHANGE_CONFIRM_BTN");
            string cancelLabel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUEST_CHANGE_CANCEL_BTN");


            NutriMessageDialog.Show(
                message: message,
                actions: new[]
                {
                    new FMDialogAction(confirmLabel, async () =>
                    {
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        if (_viewModel != null)
                        {
                            await _viewModel.StartQuestAsync();
                        }
                    }, ButtonVariant.Accent),
                    new FMDialogAction(cancelLabel, () =>
                    {
                        _audioService?.PlaySfx(SfxType.NegativeButton);
                    }, ButtonVariant.Default)
                }
            );
        }
    }
}
