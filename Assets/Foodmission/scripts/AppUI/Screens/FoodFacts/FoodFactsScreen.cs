using System;
using System.ComponentModel;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class FoodFactsScreen : NavigationScreenBase<FoodFactsViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private IDimensionService _dimensionService;
        private IBannerService _bannerService;

        private FMButton _btnRandomFact;
        private ActionGroup _groupLevelFilters;

        private ActionButton _btnLevelAll;
        private ActionButton _btnLevelBeginner;
        private ActionButton _btnLevelIntermediate;
        private ActionButton _btnLevelAdvanced;

        private Unity.AppUI.UI.Text _emptyStateText;
        private VisualElement _groupsContainer;

        public FoodFactsScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodFactsScreen));

            _dimensionService = App.current?.services?.GetService<IDimensionService>();
            _bannerService = App.current?.services?.GetService<IBannerService>();

            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _btnRandomFact = contentContainer.Q<FMButton>("btn-random-fact");
            _groupLevelFilters = contentContainer.Q<ActionGroup>("group-level-filters");

            _btnLevelAll = contentContainer.Q<ActionButton>("btn-level-all");
            _btnLevelBeginner = contentContainer.Q<ActionButton>("btn-level-beginner");
            _btnLevelIntermediate = contentContainer.Q<ActionButton>("btn-level-intermediate");
            _btnLevelAdvanced = contentContainer.Q<ActionButton>("btn-level-advanced");

            _emptyStateText = contentContainer.Q<Unity.AppUI.UI.Text>("empty-state");
            _groupsContainer = contentContainer.Q<VisualElement>("groups-container");
        }

        private void RegisterManualEvents()
        {
            if (_btnRandomFact != null) _btnRandomFact.clicked += () => _viewModel?.OpenRandomFact();

            if (_btnLevelAll != null) _btnLevelAll.clicked += () => _viewModel?.SetLevelFilter(FoodFactFilterLevel.All);
            if (_btnLevelBeginner != null) _btnLevelBeginner.clicked += () => _viewModel?.SetLevelFilter(FoodFactFilterLevel.Beginner);
            if (_btnLevelIntermediate != null) _btnLevelIntermediate.clicked += () => _viewModel?.SetLevelFilter(FoodFactFilterLevel.Intermediate);
            if (_btnLevelAdvanced != null) _btnLevelAdvanced.clicked += () => _viewModel?.SetLevelFilter(FoodFactFilterLevel.Advanced);
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
            if (e.PropertyName == nameof(_viewModel.DisplayGroups))
            {
                RebuildHierarchy();
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedLevel))
            {
                UpdateFilterStates();
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
                FoodFactLevel.Beginner => 1,
                FoodFactLevel.Intermediate => 2,
                FoodFactLevel.Advanced => 3,
                _ => 0
            };
            _groupLevelFilters?.SetSelectionWithoutNotify(new[] { levelIndex });
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
                if (group == null || group.Topics == null || group.Topics.Count == 0)
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
                progressText.text = $"{group.TotalCount}";
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

                foreach (var topicGroup in group.Topics)
                {
                    if (topicGroup == null || topicGroup.Facts == null || topicGroup.Facts.Count == 0)
                        continue;

                    var topicBox = new VisualElement();
                    topicBox.AddToClassList("fm-quizzes-topic-group");

                    // Topic Header
                    var topicHeader = new VisualElement();
                    topicHeader.AddToClassList("fm-quizzes-topic-header");

                    var topicTitle = new Unity.AppUI.UI.Text();
                    topicTitle.AddToClassList("fm-quizzes-topic-title");
                    topicTitle.text = topicGroup.Topic?.name ?? topicGroup.Topic?.code ?? "Topic";
                    topicHeader.Add(topicTitle);

                    topicBox.Add(topicHeader);

                    // Cards Container
                    var cardsContainer = new VisualElement();
                    cardsContainer.AddToClassList("fm-quizzes-cards-container");

                    foreach (var fItem in topicGroup.Facts)
                    {
                        if (fItem?.FoodFact == null) continue;

                        var factCard = new FMItemFoodFact();
                        factCard.Text = fItem.FoodFact.code ?? "";
                        factCard.SetLevel(fItem.FoodFact.level);

                        var factRef = fItem.FoodFact;
                        factCard.OnFoodFactClicked += () => _viewModel?.OpenFoodFact(factRef);

                        cardsContainer.Add(factCard);
                    }

                    topicBox.Add(cardsContainer);
                    dimContent.Add(topicBox);
                }

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
    }
}
