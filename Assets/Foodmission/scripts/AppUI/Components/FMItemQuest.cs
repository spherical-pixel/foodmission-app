using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemQuest : VisualElement
    {
        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("text")]
        [CreateProperty]
        public string Text
        {
            get => _titleText?.text ?? "";
            set
            {
                if (_titleText != null)
                {
                    _titleText.text = value;
                }
            }
        }

        [UxmlAttribute("subtitle")]
        [CreateProperty]
        public string Subtitle
        {
            get => _subtitle;
            set => SetSubtitle(value);
        }

        [UxmlAttribute("level")]
        [CreateProperty]
        public string Level
        {
            get => _level;
            set => SetLevel(value);
        }

        [UxmlAttribute("is-completed")]
        [CreateProperty]
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetCompleted(value);
        }

        /* ========= INTERNAL ELEMENTS ========= */
        private readonly VisualElement _titlesContainer;
        private readonly Unity.AppUI.UI.Text _titleText;
        private readonly Unity.AppUI.UI.Text _subtitleText;
        private readonly VisualElement _badgesContainer;
        private readonly VisualElement _levelBadge;
        private readonly Unity.AppUI.UI.Text _levelText;
        private readonly VisualElement _statusBadge;
        private readonly Unity.AppUI.UI.Text _statusText;
        private readonly Icon _statusIcon;
        private readonly Icon _arrowIcon;
        private readonly Unity.AppUI.UI.Button _openButton;

        private string _level = QuestLevel.Beginner;
        private string _subtitle = "";
        private bool _isCompleted = false;

        public event Action OnQuestClicked;
        public Unity.AppUI.UI.Button OpenButton => _openButton;

        public FMItemQuest()
        {
            AddToClassList("fm-quiz-item");
            AddToClassList("fm-challenge-item");
            AddToClassList("fm-mission-item");
            AddToClassList("fm-quest-item");

            // Main container
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("fm-quiz-item-content");
            contentContainer.AddToClassList("fm-challenge-item-content");
            contentContainer.AddToClassList("fm-mission-item-content");
            contentContainer.AddToClassList("fm-quest-item-content");
            Add(contentContainer);

            // Left side: Title and Subtitle / Item Count
            _titlesContainer = new VisualElement();
            _titlesContainer.AddToClassList("fm-mission-titles-container");
            _titlesContainer.AddToClassList("fm-quest-titles-container");
            _titlesContainer.style.flexGrow = 1;
            _titlesContainer.style.flexShrink = 1;
            _titlesContainer.style.justifyContent = Justify.Center;

            _titleText = new Unity.AppUI.UI.Text();
            _titleText.primary = true;
            _titleText.AddToClassList("fm-challenge-item-title");
            _titleText.AddToClassList("fm-mission-item-title");
            _titleText.AddToClassList("fm-quest-item-title");
            _titlesContainer.Add(_titleText);

            _subtitleText = new Unity.AppUI.UI.Text();
            _subtitleText.size = TextSize.XS;
            _subtitleText.AddToClassList("fm-mission-item-duration");
            _subtitleText.AddToClassList("fm-quest-item-subtitle");
            _subtitleText.style.display = DisplayStyle.None;
            _subtitleText.style.opacity = 0.75f;
            _subtitleText.style.marginTop = 2;
            _titlesContainer.Add(_subtitleText);

            contentContainer.Add(_titlesContainer);

            // Right side: Badges and Action Arrow
            var rightContainer = new VisualElement();
            rightContainer.AddToClassList("fm-challenge-item-right");
            rightContainer.AddToClassList("fm-mission-item-right");
            rightContainer.AddToClassList("fm-quest-item-right");
            contentContainer.Add(rightContainer);

            _badgesContainer = new VisualElement();
            _badgesContainer.AddToClassList("fm-challenge-item-badges");
            _badgesContainer.AddToClassList("fm-mission-item-badges");
            _badgesContainer.AddToClassList("fm-quest-item-badges");
            rightContainer.Add(_badgesContainer);

            // Level Badge
            _levelBadge = new VisualElement();
            _levelBadge.AddToClassList("fm-quiz-level-badge");
            _levelText = new Unity.AppUI.UI.Text();
            _levelText.AddToClassList("fm-quiz-level-badge-text");
            _levelBadge.Add(_levelText);
            _badgesContainer.Add(_levelBadge);

            // Status Badge
            _statusBadge = new VisualElement();
            _statusBadge.AddToClassList("fm-quiz-status-badge");

            _statusIcon = new Icon();
            _statusIcon.AddToClassList("fm-quiz-status-badge-icon");
            _statusIcon.iconName = "check";
            _statusBadge.Add(_statusIcon);

            _statusText = new Unity.AppUI.UI.Text();
            _statusText.AddToClassList("fm-quiz-status-badge-text");
            _statusBadge.Add(_statusText);
            _badgesContainer.Add(_statusBadge);

            _arrowIcon = new Icon();
            _arrowIcon.AddToClassList("fm-quiz-item-arrow");
            _arrowIcon.AddToClassList("fm-challenge-item-arrow");
            _arrowIcon.AddToClassList("fm-mission-item-arrow");
            _arrowIcon.AddToClassList("fm-quest-item-arrow");
            _arrowIcon.iconName = "fm-arrow-right";
            rightContainer.Add(_arrowIcon);

            // Full clickable overlay button
            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;
            _openButton.clicked += () => OnQuestClicked?.Invoke();
            contentContainer.Add(_openButton);

            SetLevel(QuestLevel.Beginner);
            SetCompleted(false);
            SetSubtitle(null);
        }

        public void SetSubtitle(string subtitle)
        {
            _subtitle = subtitle ?? "";
            // No subtitle by now
            if (/*string.IsNullOrWhiteSpace(_subtitle)*/true)
            {
                _subtitleText.style.display = DisplayStyle.None;
                _subtitleText.text = "";
            }
            else
            {
                _subtitleText.style.display = DisplayStyle.Flex;
                _subtitleText.text = _subtitle;
            }
        }

        public void SetLevel(string level)
        {
            _level = level ?? QuestLevel.Beginner;

            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--beginner");
            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--intermediate");
            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--advanced");

            string localizedLevel;
            switch (_level.ToUpperInvariant())
            {
                case QuestLevel.Intermediate:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--intermediate");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_INTERMEDIATE") ?? "Intermedio";
                    break;
                case QuestLevel.Advanced:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--advanced");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_ADVANCED") ?? "Avanzado";
                    break;
                case QuestLevel.Beginner:
                default:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--beginner");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_BEGINNER") ?? "Principiante";
                    break;
            }

            _levelText.text = localizedLevel;
        }

        public void SetCompleted(bool isCompleted)
        {
            _isCompleted = isCompleted;

            _statusBadge.RemoveFromClassList("fm-quiz-status-badge--completed");
            _statusBadge.RemoveFromClassList("fm-quiz-status-badge--pending");

            if (_isCompleted)
            {
                _statusBadge.AddToClassList("fm-quiz-status-badge--completed");
                _statusIcon.style.display = DisplayStyle.Flex;
                _statusText.text = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_STATUS_COMPLETED") ?? "Completado";
            }
            else
            {
                _statusBadge.AddToClassList("fm-quiz-status-badge--pending");
                _statusIcon.style.display = DisplayStyle.None;
                _statusText.text = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_STATUS_PENDING") ?? "Pendiente";
            }
        }
    }
}
