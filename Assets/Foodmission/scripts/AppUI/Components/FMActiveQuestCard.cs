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

        public event Action Clicked;
        public event Action QuickMealClicked;

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
            _btnQuickMeal.style.marginTop = 25;
            _btnQuickMeal.style.width = Length.Percent(100);
            if (_btnQuickMeal.clickable != null)
            {
                _btnQuickMeal.clickable.keepEventPropagation = false;
            }
            _btnQuickMeal.clicked += () => QuickMealClicked?.Invoke();
            _cardContainer.Add(_btnQuickMeal);
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
        }
    }
}
