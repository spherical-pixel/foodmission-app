using System;
using System.Collections.Generic;
using System.ComponentModel;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform
{
    /// <summary>Explanation of one progress wheel: partner text, the 20 scenarios with this wheel's values and the sources.</summary>
    [Preserve]
    public class ProgressWheelInfoScreen : NavigationScreenBase<ProgressWheelInfoViewModel>
    {
        private const string CardHiddenClass = "fm-wheel-info-card--hidden";

        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private readonly FMProgressWheelCard _card;
        private readonly Heading _title;
        private readonly Text _subtitle;
        private readonly Heading _measureHeading;
        private readonly VisualElement _paragraphs;
        private readonly Text _colScenario;
        private readonly Text _colAmount;
        private readonly Text _colValue;
        private readonly VisualElement _scenarios;
        private readonly AccordionItem _sourcesItem;
        private readonly Text _sourcesLead;
        private readonly VisualElement _sourcesList;
        private readonly List<Text> _paragraphTexts = new List<Text>();
        private readonly List<VisualElement> _scenarioRows = new List<VisualElement>();
        private readonly List<string> _scenarioLabels = new List<string>();
        private readonly List<AccessibilityNode> _nodes = new List<AccessibilityNode>();

        public ProgressWheelInfoScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.ProgressWheelInfoScreen));

            _card = contentContainer.Q<FMProgressWheelCard>("wheel-card");
            _title = contentContainer.Q<Heading>("wheel-title");
            _subtitle = contentContainer.Q<Text>("wheel-subtitle");
            _measureHeading = contentContainer.Q<Heading>("measure-heading");
            _paragraphs = contentContainer.Q<VisualElement>("paragraphs");
            _colScenario = contentContainer.Q<Text>("col-scenario");
            _colAmount = contentContainer.Q<Text>("col-amount");
            _colValue = contentContainer.Q<Text>("col-value");
            _scenarios = contentContainer.Q<VisualElement>("scenarios");
            _sourcesItem = contentContainer.Q<AccordionItem>("sources-item");
            _sourcesLead = contentContainer.Q<Text>("sources-lead");
            _sourcesList = contentContainer.Q<VisualElement>("sources-list");

            if (_card != null)
            {
                _card.Interactive = false;
            }
            BuildSources();
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
            _viewModel?.Load(string.IsNullOrEmpty(kind) ? _viewModel.Kind : kind);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
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

        private void OnBackRequested()
        {
            _navController?.PopBackStack();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProgressWheelInfoViewModel.Kind)
                || e.PropertyName == nameof(ProgressWheelInfoViewModel.Wheel))
            {
                Render();
            }
        }

        // --------------------------------------------------------------------
        // Rendering
        // --------------------------------------------------------------------

        private void Render()
        {
            string kind = _viewModel?.Kind;
            if (string.IsNullOrEmpty(kind))
            {
                return;
            }

            RenderHeader(kind);
            RenderParagraphs(kind);
            RenderScenarios(kind);
            RefreshAccessibilityNodes();
        }

        private void RenderHeader(string kind)
        {
            ProgressWheel wheel = _viewModel.Wheel;
            if (_card != null)
            {
                _card.EnableInClassList(CardHiddenClass, wheel == null);
                if (wheel != null)
                {
                    _card.Bind(wheel);
                }
            }
            SetText(_title, ProgressWheelInfoCatalog.TitleKey(kind));
            SetText(_subtitle, ProgressWheelInfoCatalog.SubtitleKey(kind));
            SetText(_measureHeading, ProgressWheelInfoCatalog.MeasureHeadingKey);
        }

        private void RenderParagraphs(string kind)
        {
            if (_paragraphs == null)
            {
                return;
            }
            _paragraphs.Clear();
            _paragraphTexts.Clear();
            int count = ProgressWheelInfoCatalog.ParagraphCount(kind);
            for (int n = 1; n <= count; n++)
            {
                var text = new Text { size = TextSize.M, enableRichText = true };
                text.AddToClassList("fm-wheel-info-paragraph");
                text.text = ProgressWheelText.Localize(ProgressWheelInfoCatalog.ParagraphKey(kind, n));
                _paragraphs.Add(text);
                _paragraphTexts.Add(text);
            }
        }

        private void RenderScenarios(string kind)
        {
            SetText(_colScenario, ProgressWheelInfoCatalog.ColScenarioKey);
            SetText(_colAmount, ProgressWheelInfoCatalog.ColAmountKey);
            SetText(_colValue, ProgressWheelInfoCatalog.ValueHeaderKey(kind));

            if (_scenarios == null)
            {
                return;
            }
            _scenarios.Clear();
            _scenarioRows.Clear();
            _scenarioLabels.Clear();
            string unit = ProgressWheelText.Localize(ProgressWheelText.UnitKey(kind));
            IReadOnlyList<WheelScenario> scenarios = ProgressWheelInfoCatalog.Scenarios;
            for (int i = 0; i < scenarios.Count; i++)
            {
                WheelScenario scenario = scenarios[i];
                if (!scenario.TryGetValue(kind, out float value))
                {
                    continue;
                }
                string name = ProgressWheelText.Localize(ProgressWheelInfoCatalog.ScenarioKey(scenario.Code));
                string amount = ProgressWheelText.Localize(ProgressWheelInfoCatalog.AmountKey(scenario.Unit), scenario.Amount);
                string valueText = ProgressWheelText.Localize(ProgressWheelInfoCatalog.ValueKey, value, unit);

                var row = new VisualElement();
                row.AddToClassList("fm-wheel-info-row");
                row.EnableInClassList("fm-wheel-info-row--alt", _scenarioRows.Count % 2 == 1);
                row.Add(Cell(name, "fm-wheel-info-cell--name"));
                row.Add(Cell(amount, "fm-wheel-info-cell--amount"));
                row.Add(Cell(valueText, "fm-wheel-info-cell--value"));
                _scenarios.Add(row);
                _scenarioRows.Add(row);
                _scenarioLabels.Add($"{name}, {amount}, {valueText}");
            }
        }

        private static Text Cell(string value, string modifierClass)
        {
            var text = new Text { size = TextSize.S, text = value };
            text.AddToClassList("fm-wheel-info-cell");
            text.AddToClassList(modifierClass);
            return text;
        }

        private void BuildSources()
        {
            if (_sourcesItem != null)
            {
                _sourcesItem.title = ProgressWheelText.Localize(ProgressWheelInfoCatalog.SourcesHeadingKey);
                _sourcesItem.value = false;
            }
            if (_sourcesLead != null)
            {
                _sourcesLead.text = ProgressWheelText.Localize(ProgressWheelInfoCatalog.SourcesLeadKey);
            }
            if (_sourcesList == null)
            {
                return;
            }
            // FMSourceItemView opens the link itself.
            foreach (string source in ProgressWheelInfoCatalog.Sources)
            {
                var item = new FMSourceItemView { ShowPrefix = false };
                item.SetSource(source);
                _sourcesList.Add(item);
            }
        }

        private static void SetText(TextElement element, string key)
        {
            if (element != null && key != null)
            {
                element.text = ProgressWheelText.Localize(key);
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
            if (_accessibilityHierarchy == null || _viewModel == null || string.IsNullOrEmpty(_viewModel.Kind))
            {
                return;
            }

            if (_card != null && _viewModel.Wheel != null)
            {
                AddStaticNode(_card.AccessibilityLabel, _card);
            }
            if (_title != null)
            {
                AddStaticNode($"{_title.text}, {_subtitle?.text}", _title);
            }
            if (_measureHeading != null)
            {
                AddStaticNode(_measureHeading.text, _measureHeading);
            }
            foreach (Text paragraph in _paragraphTexts)
            {
                // Screen readers would read the rich-text tags aloud.
                AddStaticNode(paragraph.text.Replace("<b>", "").Replace("</b>", ""), paragraph);
            }
            for (int i = 0; i < _scenarioRows.Count; i++)
            {
                AddStaticNode(_scenarioLabels[i], _scenarioRows[i]);
            }
            if (_sourcesItem != null)
            {
                AccordionItem item = _sourcesItem;
                AccessibilityNode sourcesNode = _accessibilityHierarchy.AddNode(item.title);
                sourcesNode.role = AccessibilityRole.Button;
                sourcesNode.frameGetter = MakeElementFrameGetter(item);
                sourcesNode.invoked += () =>
                {
                    item.value = !item.value;
                    return true;
                };
                _nodes.Add(sourcesNode);
            }
        }

        private void AddStaticNode(string label, VisualElement element)
        {
            AccessibilityNode node = _accessibilityHierarchy.AddNode(label);
            node.role = AccessibilityRole.StaticText;
            node.frameGetter = MakeElementFrameGetter(element);
            _nodes.Add(node);
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
