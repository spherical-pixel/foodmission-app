using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public enum ScoreBadgeType { NutriScore, Nova, EcoScore }

    [UxmlElement]
    public partial class ScoreBadge : ExVisualElement
    {
        private Text _badgeText;

        public ScoreBadge()
        {
            AddToClassList("fm-fi-score-badge");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            _badgeText = new Text { size = TextSize.M };
            _badgeText.AddToClassList("fm-fi-score-badge__text");
            Add(_badgeText);
        }

        public void SetNutriScore(string grade)
        {
            if (string.IsNullOrEmpty(grade))
            {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;
            _badgeText.text = grade.ToUpper();
            AddToClassList($"fm-fi-score-badge--{grade.ToLower()}");
        }

        public void SetNovaGroup(int group)
        {
            if (group < 1 || group > 4)
            {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;
            _badgeText.text = $"NOVA {group}";
            AddToClassList($"fm-fi-nova-badge--{group}");
        }

        public void SetEcoScore(string grade)
        {
            if (string.IsNullOrEmpty(grade))
            {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;
            _badgeText.text = grade.ToUpper();
            AddToClassList($"fm-fi-eco-badge--{grade.ToLower()}");
        }
    }
}
