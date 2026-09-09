using System;
using System.Collections.Generic;
using System.ComponentModel;
using eu.foodmission.platform.Components;
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
    public class QuestsScreen : NavigationScreenBase<QuestsViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private IDimensionService _dimensionService;
        private IBannerService _bannerService;

        private ActionGroup _groupLevelFilters;
        private ActionGroup _groupStatusFilters;

        private ActionButton _btnLevelAll;
        private ActionButton _btnLevelBeginner;
        private ActionButton _btnLevelIntermediate;
        private ActionButton _btnLevelAdvanced;

        private ActionButton _btnStatusAll;
        private ActionButton _btnStatusPending;
        private ActionButton _btnStatusCompleted;

        private Unity.AppUI.UI.Text _emptyStateText;
        private VisualElement _groupsContainer;
        private FMActiveQuestCard _activeQuestBanner;

        public QuestsScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.QuestsScreen));

            _dimensionService = App.current?.services?.GetService<IDimensionService>();
            _bannerService = App.current?.services?.GetService<IBannerService>();

            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _activeQuestBanner = contentContainer.Q<FMActiveQuestCard>("active-quest-banner");
            if (_activeQuestBanner != null)
            {
                _activeQuestBanner.Clicked += () => _viewModel?.OpenActiveQuest();
            }

            _groupLevelFilters = contentContainer.Q<ActionGroup>("group-level-filters");
            _groupStatusFilters = contentContainer.Q<ActionGroup>("group-status-filters");

            _btnLevelAll = contentContainer.Q<ActionButton>("btn-level-all");
            _btnLevelBeginner = contentContainer.Q<ActionButton>("btn-level-beginner");
            _btnLevelIntermediate = contentContainer.Q<ActionButton>("btn-level-intermediate");
            _btnLevelAdvanced = contentContainer.Q<ActionButton>("btn-level-advanced");

            _btnStatusAll = contentContainer.Q<ActionButton>("btn-status-all");
            _btnStatusPending = contentContainer.Q<ActionButton>("btn-status-pending");
            _btnStatusCompleted = contentContainer.Q<ActionButton>("btn-status-completed");

            _emptyStateText = contentContainer.Q<Unity.AppUI.UI.Text>("empty-state");
            _groupsContainer = contentContainer.Q<VisualElement>("groups-container");
        }

        private void RegisterManualEvents()
        {
            if (_btnLevelAll != null) _btnLevelAll.clicked += () => _viewModel?.SetLevelFilter(QuestFilterLevel.All);
            if (_btnLevelBeginner != null) _btnLevelBeginner.clicked += () => _viewModel?.SetLevelFilter(QuestLevel.Beginner);
            if (_btnLevelIntermediate != null) _btnLevelIntermediate.clicked += () => _viewModel?.SetLevelFilter(QuestLevel.Intermediate);
            if (_btnLevelAdvanced != null) _btnLevelAdvanced.clicked += () => _viewModel?.SetLevelFilter(QuestLevel.Advanced);

            if (_btnStatusAll != null) _btnStatusAll.clicked += () => _viewModel?.SetStatusFilter(QuestFilterStatus.All);
            if (_btnStatusPending != null) _btnStatusPending.clicked += () => _viewModel?.SetStatusFilter(QuestFilterStatus.Pending);
            if (_btnStatusCompleted != null) _btnStatusCompleted.clicked += () => _viewModel?.SetStatusFilter(QuestFilterStatus.Completed);
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            UpdateFilterStates();
            schedule.Execute(UpdateFilterStates);
            _ = _viewModel?.LoadDataAsync();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            UpdateFilterStates();
            RebuildHierarchy();
            UpdateActiveQuestBanner();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            _activeQuestBanner = null;
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.DisplayGroups))
            {
                RebuildHierarchy();
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedLevel) || e.PropertyName == nameof(_viewModel.SelectedStatus))
            {
                UpdateFilterStates();
            }
            else if (e.PropertyName == nameof(_viewModel.HasActiveQuest) ||
                     e.PropertyName == nameof(_viewModel.ActiveQuestTitle) ||
                     e.PropertyName == nameof(_viewModel.ActiveQuestActivityStates))
            {
                UpdateActiveQuestBanner();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                UpdateApiErrorState();
            }
        }

        private void UpdateFilterStates()
        {
            if (_viewModel == null) return;

            int levelIndex = _viewModel.SelectedLevel switch
            {
                QuestLevel.Beginner => 1,
                QuestLevel.Intermediate => 2,
                QuestLevel.Advanced => 3,
                _ => 0
            };
            _groupLevelFilters?.SetSelectionWithoutNotify(new[] { levelIndex });

            int statusIndex = _viewModel.SelectedStatus switch
            {
                QuestFilterStatus.Pending => 1,
                QuestFilterStatus.Completed => 2,
                _ => 0
            };
            _groupStatusFilters?.SetSelectionWithoutNotify(new[] { statusIndex });
        }

        private void RebuildHierarchy()
        {
            if (_groupsContainer == null) return;
            _groupsContainer.Clear();

            var groups = _viewModel?.DisplayGroups;
            if (groups == null || groups.Count == 0)
            {
                if (_emptyStateText != null) _emptyStateText.style.display = DisplayStyle.Flex;
                _groupsContainer.style.display = DisplayStyle.None;
                return;
            }

            if (_emptyStateText != null) _emptyStateText.style.display = DisplayStyle.None;
            _groupsContainer.style.display = DisplayStyle.Flex;

            foreach (var group in groups)
            {
                if (group == null || group.Quests == null || group.Quests.Count == 0)
                    continue;

                var dimBox = new VisualElement();
                dimBox.AddToClassList("fm-quizzes-dim-group");

                // Dimension Header
                var dimHeader = new VisualElement();
                dimHeader.AddToClassList("fm-quizzes-dim-header");

                var dimIcon = new Image();
                dimIcon.AddToClassList("fm-quizzes-dim-icon");
                _ = _bannerService?.BindDimensionBanner(dimIcon, group.Dimension?.code);
                dimHeader.Add(dimIcon);

                var row = new VisualElement();
                row.AddToClassList("fm-quizzes-dim-header-row");
                dimHeader.Add(row);

                var dimTitle = new Unity.AppUI.UI.Text();
                dimTitle.AddToClassList("fm-quizzes-dim-title");
                dimTitle.text = group.Dimension?.name ?? group.Dimension?.code ?? "Dimension";
                dimTitle.size = TextSize.M;
                row.Add(dimTitle);

                var progressBadge = new VisualElement();
                progressBadge.AddToClassList("fm-quizzes-dim-progress-badge");
                var progressText = new Unity.AppUI.UI.Text();
                progressText.AddToClassList("fm-quizzes-dim-progress-text");
                progressText.text = $"{group.CompletedCount}/{group.TotalCount}";
                progressBadge.Add(progressText);
                row.Add(progressBadge);

                var chevron = new Icon();
                chevron.AddToClassList("fm-quizzes-dim-chevron");
                chevron.iconName = "caret-down";
                if (!group.IsExpanded)
                {
                    chevron.AddToClassList("fm-quizzes-dim-chevron--collapsed");
                }
                row.Add(chevron);

                // Dimension Content Container
                var dimContent = new VisualElement();
                dimContent.AddToClassList("fm-quizzes-dim-content");
                if (!group.IsExpanded)
                {
                    dimContent.AddToClassList("fm-quizzes-dim-content--hidden");
                }

                string dimCode = group.Dimension?.code ?? group.Dimension?.id;
                dimHeader.RegisterCallback<ClickEvent>(_ =>
                {
                    group.IsExpanded = !group.IsExpanded;
                    if (group.IsExpanded)
                    {
                        chevron.RemoveFromClassList("fm-quizzes-dim-chevron--collapsed");
                        dimContent.RemoveFromClassList("fm-quizzes-dim-content--hidden");
                    }
                    else
                    {
                        chevron.AddToClassList("fm-quizzes-dim-chevron--collapsed");
                        dimContent.AddToClassList("fm-quizzes-dim-content--hidden");
                    }
                    _viewModel?.ToggleDimensionExpanded(dimCode);
                });

                dimBox.Add(dimHeader);

                var cardsContainer = new VisualElement();
                cardsContainer.AddToClassList("fm-quizzes-cards-container");

                if (group.Quests != null)
                {
                    foreach (var qItem in group.Quests)
                    {
                        if (qItem?.Quest == null) continue;

                        var questCard = new FMItemQuest();
                        questCard.Text = !string.IsNullOrEmpty(qItem.Quest.title)
                            ? qItem.Quest.title
                            : (!string.IsNullOrEmpty(qItem.Quest.name) ? qItem.Quest.name : (qItem.Quest.code ?? ""));

                        if (qItem.Quest.items != null && qItem.Quest.items.Length > 0)
                        {
                            string actFormat = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUEST_ACTIVITIES_COUNT") ?? "{0} actividades";
                            questCard.SetSubtitle(string.Format(actFormat, qItem.Quest.items.Length));
                        }
                        else if (!string.IsNullOrEmpty(qItem.Quest.description))
                        {
                            questCard.SetSubtitle(qItem.Quest.description);
                        }

                        questCard.SetLevel(qItem.Quest.level);
                        questCard.SetCompleted(qItem.IsCompleted);

                        var questRef = qItem.Quest;
                        questCard.OnQuestClicked += () => _viewModel?.OpenQuest(questRef);

                        cardsContainer.Add(questCard);
                    }
                }

                dimContent.Add(cardsContainer);
                dimBox.Add(dimContent);
                _groupsContainer.Add(dimBox);
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

        private void UpdateActiveQuestBanner()
        {
            if (_activeQuestBanner == null || _viewModel == null) return;

            if (_viewModel.HasActiveQuest)
            {
                _activeQuestBanner.style.display = DisplayStyle.Flex;
                _activeQuestBanner.Setup(_viewModel.ActiveQuestTitle, _viewModel.ActiveQuestActivityStates);
            }
            else
            {
                _activeQuestBanner.style.display = DisplayStyle.None;
            }
        }
    }
}
