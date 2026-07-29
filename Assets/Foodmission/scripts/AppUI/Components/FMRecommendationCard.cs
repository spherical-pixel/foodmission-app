using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [UxmlElement]
    public partial class FMRecommendationCard : VisualElement
    {
        private readonly Text _titleText;
        private readonly Text _badgeText;

        public FMRecommendationCard()
        {
            AddToClassList("fm-r-carousel-card");

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Column;
            body.style.flexGrow = 1;
            body.style.justifyContent = Justify.FlexEnd;
            Add(body);

            _badgeText = new Text();
            _badgeText.AddToClassList("fm-r-carousel-card-badge");
            body.Add(_badgeText);

            _titleText = new Text();
            _titleText.AddToClassList("fm-r-carousel-card-title");
            body.Add(_titleText);
        }

        [UxmlAttribute("title")]
        [CreateProperty]
        public string Title
        {
            get => _titleText.text;
            set => _titleText.text = value;
        }

        [UxmlAttribute("badge-text")]
        [CreateProperty]
        public string BadgeText
        {
            get => _badgeText.text;
            set => _badgeText.text = value;
        }
    }
}
