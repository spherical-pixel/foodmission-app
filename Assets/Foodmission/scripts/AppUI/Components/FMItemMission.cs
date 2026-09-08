using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemMission : VisualElement
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

        [UxmlAttribute("duration")]
        [CreateProperty]
        public string Duration
        {
            get => _duration;
            set => SetDuration(value);
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
        private readonly Unity.AppUI.UI.Text _durationText;
        private readonly VisualElement _badgesContainer;
        private readonly VisualElement _levelBadge;
        private readonly Unity.AppUI.UI.Text _levelText;
        private readonly VisualElement _statusBadge;
        private readonly Unity.AppUI.UI.Text _statusText;
        private readonly Icon _statusIcon;
        private readonly Icon _arrowIcon;
        private readonly Unity.AppUI.UI.Button _openButton;

        private string _level = MissionLevel.Beginner;
        private string _duration = "";
        private bool _isCompleted = false;

        public event Action OnMissionClicked;
        public Unity.AppUI.UI.Button OpenButton => _openButton;

        public FMItemMission()
        {
            AddToClassList("fm-quiz-item");
            AddToClassList("fm-challenge-item");
            AddToClassList("fm-mission-item");

            // Main container
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("fm-quiz-item-content");
            contentContainer.AddToClassList("fm-challenge-item-content");
            contentContainer.AddToClassList("fm-mission-item-content");
            Add(contentContainer);

            // Left side: Title and Duration
            _titlesContainer = new VisualElement();
            _titlesContainer.AddToClassList("fm-mission-titles-container");
            _titlesContainer.style.flexGrow = 1;
            _titlesContainer.style.flexShrink = 1;
            _titlesContainer.style.justifyContent = Justify.Center;

            _titleText = new Unity.AppUI.UI.Text();
            _titleText.primary = true;
            _titleText.AddToClassList("fm-challenge-item-title");
            _titleText.AddToClassList("fm-mission-item-title");
            _titlesContainer.Add(_titleText);

            _durationText = new Unity.AppUI.UI.Text();
            _durationText.size = TextSize.XS;
            _durationText.AddToClassList("fm-mission-item-duration");
            _durationText.style.display = DisplayStyle.None;
            _durationText.style.opacity = 0.75f;
            _durationText.style.marginTop = 2;
            _titlesContainer.Add(_durationText);

            contentContainer.Add(_titlesContainer);

            // Right side: Badges and Action Arrow
            var rightContainer = new VisualElement();
            rightContainer.AddToClassList("fm-challenge-item-right");
            rightContainer.AddToClassList("fm-mission-item-right");
            contentContainer.Add(rightContainer);

            _badgesContainer = new VisualElement();
            _badgesContainer.AddToClassList("fm-challenge-item-badges");
            _badgesContainer.AddToClassList("fm-mission-item-badges");
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
            _arrowIcon.iconName = "fm-arrow-right";
            rightContainer.Add(_arrowIcon);

            // Full clickable overlay button
            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;
            _openButton.clicked += () => OnMissionClicked?.Invoke();
            contentContainer.Add(_openButton);

            SetLevel(MissionLevel.Beginner);
            SetCompleted(false);
            SetDuration(null);
        }

        public void SetDuration(string duration)
        {
            _duration = duration ?? "";
            if (string.IsNullOrWhiteSpace(_duration))
            {
                _durationText.style.display = DisplayStyle.None;
                _durationText.text = "";
            }
            else
            {
                _durationText.style.display = DisplayStyle.Flex;
                _durationText.text = $"⏱️ {_duration}";
            }
        }

        public void SetLevel(string level)
        {
            _level = level ?? MissionLevel.Beginner;

            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--beginner");
            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--intermediate");
            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--advanced");

            string localizedLevel;
            switch (_level.ToUpperInvariant())
            {
                case MissionLevel.Intermediate:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--intermediate");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_INTERMEDIATE") ?? "Intermedio";
                    break;
                case MissionLevel.Advanced:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--advanced");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_ADVANCED") ?? "Avanzado";
                    break;
                case MissionLevel.Beginner:
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
