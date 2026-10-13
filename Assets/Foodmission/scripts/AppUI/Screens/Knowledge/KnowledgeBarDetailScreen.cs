using System;
using System.Collections.Generic;
using System.ComponentModel;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>One knowledge bar: header with progress and its items grouped by content type.</summary>
    [Preserve]
    public class KnowledgeBarDetailScreen : NavigationScreenBase<KnowledgeBarDetailViewModel>
    {
        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private readonly Heading _emoji;
        private readonly Heading _title;
        private readonly LinearProgress _progress;
        private readonly Unity.AppUI.UI.Text _count;
        private readonly VisualElement _empty;
        private readonly VisualElement _error;
        private readonly FMButton _btnRetry;
        private readonly Accordion _sectionsRoot;
        private readonly List<AccordionItem> _sectionItems = new List<AccordionItem>();
        private readonly HashSet<AccordionItem> _builtSections = new HashSet<AccordionItem>();
        private readonly List<AccessibilityNode> _nodes = new List<AccessibilityNode>();

        public KnowledgeBarDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.KnowledgeBarDetailScreen));

            _emoji = contentContainer.Q<Heading>("bar-emoji");
            _title = contentContainer.Q<Heading>("bar-title");
            _progress = contentContainer.Q<LinearProgress>("bar-progress");
            _count = contentContainer.Q<Unity.AppUI.UI.Text>("bar-count");
            _empty = contentContainer.Q<VisualElement>("bar-empty");
            _error = contentContainer.Q<VisualElement>("bar-error");
            _btnRetry = contentContainer.Q<FMButton>("btn-retry");
            _sectionsRoot = contentContainer.Q<Accordion>("sections");

            if (_btnRetry != null)
            {
                _btnRetry.clicked += OnRetryClicked;
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string kind = null;
            if (args != null)
            {
                foreach (Argument a in args)
                {
                    if (a.name == "kind")
                    {
                        kind = a.value;
                        break;
                    }
                }
            }
            // Reload on every enter: coming back from a quiz or fact must show it as finished.
            _ = _viewModel?.LoadAsync(string.IsNullOrEmpty(kind) ? _viewModel.Kind : kind);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            TrackLoadingOverlay(() => _viewModel.IsLoading, nameof(KnowledgeBarDetailViewModel.IsLoading));
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.BackRequested += OnBackRequested;
            }
            Render();
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.BackRequested -= OnBackRequested;
            }
            base.OnViewModelUnbinding();
        }

        private void OnRetryClicked()
        {
            if (_viewModel != null)
            {
                _ = _viewModel.LoadAsync(_viewModel.Kind);
            }
        }

        private void OnBackRequested()
        {
            _navController?.PopBackStack();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KnowledgeBarDetailViewModel.ErrorDetail))
            {
                if (_viewModel.ErrorDetail != null)
                {
                    FMDialog.ShowApiError(
                        this,
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"),
                        _viewModel.ErrorDetail,
                        onOk: () => { });
                }
                RenderHeader();
            }
            else if (e.PropertyName == nameof(KnowledgeBarDetailViewModel.Sections))
            {
                RebuildSections();
                RenderHeader();
            }
            else if (e.PropertyName == nameof(KnowledgeBarDetailViewModel.Kind)
                || e.PropertyName == nameof(KnowledgeBarDetailViewModel.Finished)
                || e.PropertyName == nameof(KnowledgeBarDetailViewModel.Total)
                || e.PropertyName == nameof(KnowledgeBarDetailViewModel.Progress)
                || e.PropertyName == nameof(KnowledgeBarDetailViewModel.IsLoading))
            {
                RenderHeader();
            }
        }

        private void Render()
        {
            RenderHeader();
            RebuildSections();
        }

        private void RenderHeader()
        {
            if (_viewModel == null)
            {
                return;
            }

            string nameKey = KnowledgeBarText.NameKey(_viewModel.Kind);
            if (_emoji != null)
            {
                _emoji.text = KnowledgeBarText.Emoji(_viewModel.Kind);
            }
            if (_title != null)
            {
                _title.text = nameKey != null ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", nameKey) : "";
            }
            if (_progress != null)
            {
                _progress.value = _viewModel.Progress;
            }
            if (_count != null)
            {
                _count.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "KNOWLEDGE_BAR_COMPLETED", null, FallbackBehavior.UseProjectSettings, _viewModel.Finished, _viewModel.Total);
            }

            bool hasError = _viewModel.ErrorDetail != null;
            _error?.EnableInClassList("fm-kb--hidden", !hasError);
            _empty?.EnableInClassList("fm-kb--hidden", hasError || _viewModel.IsLoading || _viewModel.Sections.Count > 0);
            RefreshAccessibilityNodes();
        }

        private void RebuildSections()
        {
            if (_sectionsRoot == null || _viewModel == null)
            {
                return;
            }
            _sectionsRoot.Clear();
            _sectionItems.Clear();
            _builtSections.Clear();

            IReadOnlyList<KnowledgeBarSection> sections = _viewModel.Sections;
            var expanded = new HashSet<int>(KnowledgeBarExpansion.ExpandedIndices(_viewModel.Kind, sections));
            for (int i = 0; i < sections.Count; i++)
            {
                KnowledgeBarSection section = sections[i];
                string count = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "KNOWLEDGE_SECTION_COUNT", null, FallbackBehavior.UseProjectSettings, section.Finished, section.Total);
                var item = new AccordionItem
                {
                    title = $"{KnowledgeBarSections.IconFor(section.ContentType)} {LocalizationSettings.StringDatabase.GetLocalizedString("UI", section.TitleKey)} · {count}",
                };
                // Rows are built on first expand: a bar can hold ~200 items.
                item.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        BuildRows(item, section);
                    }
                    KnowledgeBarExpansion.Remember(_viewModel?.Kind, section.ContentType, evt.newValue);
                    RefreshAccessibilityNodes();
                });
                _sectionsRoot.Add(item);
                _sectionItems.Add(item);
                if (expanded.Contains(i))
                {
                    BuildRows(item, section);
                    item.SetValueWithoutNotify(true);
                }
            }
            RefreshAccessibilityNodes();
        }

        private void BuildRows(AccordionItem item, KnowledgeBarSection section)
        {
            if (!_builtSections.Add(item))
            {
                return;
            }
            foreach (KnowledgeBarRow row in section.Rows)
            {
                var element = new VisualElement();
                element.AddToClassList("fm-kb-row");
                element.EnableInClassList("fm-kb-row--finished", row.Finished);

                var icon = new Unity.AppUI.UI.Text(KnowledgeBarSections.IconFor(row.ContentType));
                icon.AddToClassList("fm-kb-row__icon");
                element.Add(icon);

                var title = new Unity.AppUI.UI.Text(row.Title) { size = TextSize.M };
                title.AddToClassList("fm-kb-row__title");
                element.Add(title);

                var check = new Unity.AppUI.UI.Text(row.Finished ? "✓" : "");
                check.AddToClassList("fm-kb-row__check");
                element.Add(check);

                if (row.CanOpen)
                {
                    element.AddToClassList("fm-kb-row--clickable");
                    KnowledgeBarRow captured = row;
                    element.AddManipulator(new UnityEngine.UIElements.Clickable(() => _viewModel?.Open(captured)));
                }
                item.Add(element);
            }
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            AddAccessibilityNodes();
        }

        protected override void TeardownAccessibilityNodes()
        {
            _nodes.Clear();
            base.TeardownAccessibilityNodes();
        }

        private void RefreshAccessibilityNodes()
        {
            if (_accessibilityHierarchy == null)
            {
                return;
            }
            foreach (AccessibilityNode node in _nodes)
            {
                _accessibilityHierarchy.RemoveNode(node);
            }
            _nodes.Clear();
            AddAccessibilityNodes();
        }

        private void AddAccessibilityNodes()
        {
            if (_accessibilityHierarchy == null || _viewModel == null)
            {
                return;
            }
            AccessibilityHierarchy h = _accessibilityHierarchy;

            if (_title != null)
            {
                AccessibilityNode header = h.AddNode($"{_title.text}, {_count?.text}");
                header.role = AccessibilityRole.StaticText;
                header.frameGetter = MakeElementFrameGetter(_title);
                _nodes.Add(header);
            }

            string done = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "KNOWLEDGE_ITEM_COMPLETED");
            string pending = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "KNOWLEDGE_ITEM_PENDING");
            IReadOnlyList<KnowledgeBarSection> sections = _viewModel.Sections;
            for (int i = 0; i < _sectionItems.Count && i < sections.Count; i++)
            {
                AccordionItem item = _sectionItems[i];
                AccessibilityNode sectionNode = h.AddNode(item.title);
                sectionNode.role = AccessibilityRole.Button;
                sectionNode.frameGetter = MakeElementFrameGetter(item);
                sectionNode.invoked += () =>
                {
                    item.value = !item.value;
                    return true;
                };
                _nodes.Add(sectionNode);

                if (!item.value)
                {
                    continue;
                }
                List<VisualElement> rowElements = item.Query<VisualElement>(className: "fm-kb-row").ToList();
                KnowledgeBarSection section = sections[i];
                for (int r = 0; r < rowElements.Count && r < section.Rows.Count; r++)
                {
                    KnowledgeBarRow row = section.Rows[r];
                    AccessibilityNode rowNode = h.AddNode($"{row.Title}, {(row.Finished ? done : pending)}");
                    rowNode.role = row.CanOpen ? AccessibilityRole.Button : AccessibilityRole.StaticText;
                    rowNode.frameGetter = MakeElementFrameGetter(rowElements[r]);
                    if (row.CanOpen)
                    {
                        rowNode.invoked += () =>
                        {
                            _viewModel?.Open(row);
                            return true;
                        };
                    }
                    _nodes.Add(rowNode);
                }
            }
        }

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null)
                {
                    return Rect.zero;
                }
                Rect rect = element.worldBound;
                float scale = element.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
        }
    }
}
