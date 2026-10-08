using System;
using System.Linq;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMActiveQuestCard : VisualElement
    {
        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("heading-text")]
        [CreateProperty]
        public string HeadingText
        {
            get => _headingText?.text ?? "";
            set
            {
                if (_headingText != null)
                {
                    _headingText.text = value;
                }
            }
        }

        [UxmlAttribute("quest-title")]
        [CreateProperty]
        public string QuestTitle
        {
            get => _questTitleText?.text ?? "";
            set
            {
                if (_questTitleText != null)
                {
                    _questTitleText.text = value;
                }
            }
        }

        /* ========= INTERNAL UI ELEMENTS ========= */
        private readonly Unity.AppUI.UI.Text _headingText;
        private readonly VisualElement _cardContainer;
        private readonly Unity.AppUI.UI.Text _questTitleText;
        private readonly ScrollView _timelineContainer;
        private readonly Unity.AppUI.UI.Button _openButton;
        private readonly FMButton _btnQuickMeal;
        private readonly FMButton _btnCheckIn;

        // Natural timeline metrics (must match .fm-active-quest-node / -line in Foodmission_Global_Styles.uss)
        private const float NodeSize = 64f;
        private const float NodeBorder = 12f;
        private const float LineWidth = 32f;
        private const float LineHeight = 24f;
        private const float LineOverlap = 6f;

        private int _nodeCount;
        private float _lastTimelineWidth = -1f;

        public event Action Clicked;
        public event Action QuickMealClicked;
        public event Action CheckInClicked;

        public FMActiveQuestCard()
        {
            AddToClassList("fm-active-quest-wrapper");

            // Section Heading ("Active Quests")
            _headingText = new Unity.AppUI.UI.Text();
            _headingText.AddToClassList("fm-active-quest-heading");
            _headingText.text = "@UI:ACTIVE_QUESTS";
            Add(_headingText);

            // Card Container
            _cardContainer = new VisualElement();
            _cardContainer.AddToClassList("fm-active-quest-card");
            Add(_cardContainer);

            // Quest Title
            _questTitleText = new Unity.AppUI.UI.Text();
            _questTitleText.AddToClassList("fm-active-quest-title");
            _questTitleText.text = "";
            _cardContainer.Add(_questTitleText);

            // Timeline container (horizontal circles & connecting lines)
            _timelineContainer = new ScrollView(ScrollViewMode.Horizontal)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden
            };
            _timelineContainer.AddToClassList("fm-active-quest-timeline");
            _timelineContainer.RegisterCallback<GeometryChangedEvent>(OnTimelineGeometryChanged);
            _cardContainer.Add(_timelineContainer);

            // Full clickable overlay button (placed beneath the CTA button, covering the card)
            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;
            _openButton.clicked += () => Clicked?.Invoke();
            _cardContainer.Add(_openButton);

            // Quick Meal Check CTA Button (placed on top of _openButton)
            _btnQuickMeal = new FMButton
            {
                title = "@UI:QUICK_MEAL_LOG_GOTO_BUTTON",
                size = Size.S,
                variant = ButtonVariant.Accent
            };
            _btnQuickMeal.AddToClassList("fm-active-quest-cart-quick-meal-btn");
            _btnQuickMeal.style.marginTop = 25;
            _btnQuickMeal.style.width = Length.Percent(100);
            if (_btnQuickMeal.clickable != null)
            {
                _btnQuickMeal.clickable.keepEventPropagation = false;
            }
            _btnQuickMeal.clicked += () => QuickMealClicked?.Invoke();
            _cardContainer.Add(_btnQuickMeal);

            // Check-in with Foody CTA Button (also on top of _openButton)
            _btnCheckIn = new FMButton
            {
                title = "@UI:HOME_QUEST_CHECKIN_BUTTON",
                size = Size.S,
                variant = ButtonVariant.Default
            };
            _btnCheckIn.AddToClassList("fm-active-quest-card-checkin-btn");
            if (_btnCheckIn.clickable != null)
            {
                _btnCheckIn.clickable.keepEventPropagation = false;
            }
            _btnCheckIn.clicked += () => CheckInClicked?.Invoke();
            _cardContainer.Add(_btnCheckIn);
        }

        /// <summary>
        /// Configures the card with the quest title and individual activity completion states.
        /// </summary>
        public void Setup(string title, bool[] activityCompletedStates)
        {
            QuestTitle = title ?? "";
            RebuildTimeline(activityCompletedStates);
        }

        private void RebuildTimeline(bool[] activityCompletedStates)
        {
            _timelineContainer.contentContainer.Clear();
            _nodeCount = 0;
            _lastTimelineWidth = -1f;

            if (activityCompletedStates == null || activityCompletedStates.Length == 0)
            {
                return;
            }

            var sortedStates = activityCompletedStates.OrderByDescending(s => s).ToArray();
            int count = sortedStates.Length;
            for (int i = 0; i < count; i++)
            {
                bool isCompleted = sortedStates[i];

                // Node Circle
                var node = new VisualElement();
                node.AddToClassList("fm-active-quest-node");
                if (isCompleted)
                {
                    node.AddToClassList("fm-active-quest-node--completed");
                }
                else
                {
                    node.AddToClassList("fm-active-quest-node--pending");
                }
                _timelineContainer.contentContainer.Add(node);

                // Connecting Line between adjacent nodes
                if (i < count - 1)
                {
                    var line = new VisualElement();
                    line.AddToClassList("fm-active-quest-line");
                    _timelineContainer.contentContainer.Add(line);
                }
            }

            _nodeCount = count;
            ApplyTimelineScale(_timelineContainer.contentViewport.layout.width);
        }

        private void OnTimelineGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyTimelineScale(_timelineContainer.contentViewport.layout.width);
        }

        /// <summary>
        /// Shrinks nodes and lines proportionally when the quest has more activities than fit in the card.
        /// </summary>
        private void ApplyTimelineScale(float availableWidth)
        {
            if (_nodeCount == 0 || float.IsNaN(availableWidth) || availableWidth <= 0f)
            {
                return;
            }
            if (Mathf.Approximately(availableWidth, _lastTimelineWidth))
            {
                return;
            }
            _lastTimelineWidth = availableWidth;

            float naturalWidth = _nodeCount * NodeSize + (_nodeCount - 1) * (LineWidth - 2f * LineOverlap);
            float scale = Mathf.Min(availableWidth / naturalWidth, 1f);

            foreach (var child in _timelineContainer.contentContainer.Children())
            {
                if (child.ClassListContains("fm-active-quest-line"))
                {
                    ApplyLineScale(child, scale);
                }
                else
                {
                    ApplyNodeScale(child, scale);
                }
            }
        }

        private static void ApplyNodeScale(VisualElement node, float scale)
        {
            if (scale >= 1f)
            {
                node.style.width = StyleKeyword.Null;
                node.style.height = StyleKeyword.Null;
                node.style.minWidth = StyleKeyword.Null;
                node.style.minHeight = StyleKeyword.Null;
                node.style.maxWidth = StyleKeyword.Null;
                node.style.maxHeight = StyleKeyword.Null;
                node.style.borderTopLeftRadius = StyleKeyword.Null;
                node.style.borderTopRightRadius = StyleKeyword.Null;
                node.style.borderBottomLeftRadius = StyleKeyword.Null;
                node.style.borderBottomRightRadius = StyleKeyword.Null;
                node.style.borderTopWidth = StyleKeyword.Null;
                node.style.borderRightWidth = StyleKeyword.Null;
                node.style.borderBottomWidth = StyleKeyword.Null;
                node.style.borderLeftWidth = StyleKeyword.Null;
                return;
            }

            float size = Mathf.Max(4f, Mathf.Floor(NodeSize * scale));
            node.style.width = size;
            node.style.height = size;
            node.style.minWidth = size;
            node.style.minHeight = size;
            node.style.maxWidth = size;
            node.style.maxHeight = size;
            node.style.borderTopLeftRadius = size;
            node.style.borderTopRightRadius = size;
            node.style.borderBottomLeftRadius = size;
            node.style.borderBottomRightRadius = size;

            if (node.ClassListContains("fm-active-quest-node--completed"))
            {
                float border = Mathf.Max(1f, Mathf.Round(NodeBorder * scale));
                node.style.borderTopWidth = border;
                node.style.borderRightWidth = border;
                node.style.borderBottomWidth = border;
                node.style.borderLeftWidth = border;
            }
        }

        private static void ApplyLineScale(VisualElement line, float scale)
        {
            if (scale >= 1f)
            {
                line.style.width = StyleKeyword.Null;
                line.style.height = StyleKeyword.Null;
                line.style.marginLeft = StyleKeyword.Null;
                line.style.marginRight = StyleKeyword.Null;
                return;
            }

            line.style.width = Mathf.Floor(LineWidth * scale);
            line.style.height = Mathf.Max(2f, Mathf.Round(LineHeight * scale));
            line.style.marginLeft = -Mathf.Round(LineOverlap * scale);
            line.style.marginRight = -Mathf.Round(LineOverlap * scale);
        }
    }
}
