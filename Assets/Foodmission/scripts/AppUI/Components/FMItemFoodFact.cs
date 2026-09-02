using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemFoodFact : VisualElement
    {
        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("text")]
        [CreateProperty]
        public string Text
        {
            get => _codeText?.text ?? "";
            set
            {
                if (_codeText != null)
                {
                    _codeText.text = value;
                }
            }
        }

        [UxmlAttribute("level")]
        [CreateProperty]
        public string Level
        {
            get => _level;
            set => SetLevel(value);
        }

        /* ========= INTERNAL ELEMENTS ========= */
        private readonly Unity.AppUI.UI.Text _codeText;
        private readonly VisualElement _badgesContainer;
        private readonly VisualElement _levelBadge;
        private readonly Unity.AppUI.UI.Text _levelText;
        private readonly Icon _arrowIcon;
        private readonly Unity.AppUI.UI.Button _openButton;

        private string _level = FoodFactLevel.Beginner;

        public event Action OnFoodFactClicked;
        public Unity.AppUI.UI.Button OpenButton => _openButton;

        public FMItemFoodFact()
        {
            AddToClassList("fm-quiz-item");
            AddToClassList("fm-foodfact-item");

            // Left side: Code and Badges
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("fm-quiz-item-content");
            Add(contentContainer);

            _codeText = new Unity.AppUI.UI.Text();
            _codeText.primary = true;
            _codeText.AddToClassList("fm-quiz-item-code");
            contentContainer.Add(_codeText);

            _badgesContainer = new VisualElement();
            _badgesContainer.AddToClassList("fm-quiz-item-badges");
            contentContainer.Add(_badgesContainer);

            // Level Badge
            _levelBadge = new VisualElement();
            _levelBadge.AddToClassList("fm-quiz-level-badge");
            _levelText = new Unity.AppUI.UI.Text();
            _levelText.AddToClassList("fm-quiz-level-badge-text");
            _levelBadge.Add(_levelText);
            _badgesContainer.Add(_levelBadge);

            _arrowIcon = new Icon();
            _arrowIcon.AddToClassList("fm-quiz-item-arrow");
            _arrowIcon.iconName = "fm-arrow-right";
            contentContainer.Add(_arrowIcon);

            // Full clickable overlay button
            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;
            _openButton.clicked += () => OnFoodFactClicked?.Invoke();
            contentContainer.Add(_openButton);

            SetLevel(FoodFactLevel.Beginner);
        }

        public void SetLevel(string level)
        {
            _level = level ?? FoodFactLevel.Beginner;

            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--beginner");
            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--intermediate");
            _levelBadge.RemoveFromClassList("fm-quiz-level-badge--advanced");

            string localizedLevel;
            switch (_level.ToUpperInvariant())
            {
                case FoodFactLevel.Intermediate:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--intermediate");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_INTERMEDIATE") ?? "Intermedio";
                    break;
                case FoodFactLevel.Advanced:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--advanced");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_ADVANCED") ?? "Avanzado";
                    break;
                case FoodFactLevel.Beginner:
                default:
                    _levelBadge.AddToClassList("fm-quiz-level-badge--beginner");
                    localizedLevel = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", "QUIZ_LEVEL_BEGINNER") ?? "Principiante";
                    break;
            }

            _levelText.text = localizedLevel;
        }
    }
}
