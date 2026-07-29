using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Reusable card item component for displaying a Recipe in the Recipe Book list.
    /// Inspired by the Foodmission Recipe Book design mockups.
    /// </summary>
    [UxmlElement]
    public partial class FMItemRecipe : VisualElement
    {
        private readonly Heading _titleText;
        private readonly Text _authorText;
        private readonly Text _heroEmoji;
        private readonly VisualElement _heroImageContainer;
        private readonly Text _ratingText;
        private readonly VisualElement _footerContainer;

        public FMItemRecipe()
        {
            AddToClassList("fm-r-card");

            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("fm-r-card-header");
            Add(headerContainer);

            _titleText = new Heading();
            _titleText.AddToClassList("fm-r-card-title");
            headerContainer.Add(_titleText);

            _authorText = new Text();
            _authorText.AddToClassList("fm-r-card-author");
            headerContainer.Add(_authorText);

            _heroImageContainer = new VisualElement();
            _heroImageContainer.AddToClassList("fm-r-card-hero");
            Add(_heroImageContainer);

            _heroEmoji = new Text { text = "🍝" };
            _heroEmoji.AddToClassList("fm-r-card-hero-emoji");
            _heroImageContainer.Add(_heroEmoji);

            var metaContainer = new VisualElement();
            metaContainer.AddToClassList("fm-r-card-meta-container");
            Add(metaContainer);

            _ratingText = new Text();
            _ratingText.AddToClassList("fm-r-card-rating");
            metaContainer.Add(_ratingText);
        }

        [UxmlAttribute("text")]
        [CreateProperty]
        public string Text
        {
            get => _titleText.text;
            set => _titleText.text = value;
        }

        [UxmlAttribute("author")]
        [CreateProperty]
        public string Author
        {
            get => _authorText.text;
            set
            {
                _authorText.text = value;
                _authorText.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("rating-text")]
        [CreateProperty]
        public string RatingText
        {
            get => _ratingText.text;
            set
            {
                _ratingText.text = value;
                _ratingText.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private string _imageUrl;

        [UxmlAttribute("image-url")]
        [CreateProperty]
        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                _ = LoadImageAsync(value);
            }
        }

        private async System.Threading.Tasks.Task LoadImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _heroImageContainer.style.backgroundImage = StyleKeyword.Null;
                _heroEmoji.style.display = DisplayStyle.Flex;
                return;
            }

            try
            {
                var imageService = Unity.AppUI.MVVM.App.current?.services?.GetService(typeof(IImageService)) as IImageService;
                if (imageService == null) return;

                var texture = await imageService.LoadImageAsync(url);
                if (texture != null)
                {
                    _heroImageContainer.style.backgroundImage = Background.FromTexture2D(texture);
                    _heroEmoji.style.display = DisplayStyle.None;
                }
                else
                {
                    _heroImageContainer.style.backgroundImage = StyleKeyword.Null;
                    _heroEmoji.style.display = DisplayStyle.Flex;
                }
            }
            catch
            {
                _heroImageContainer.style.backgroundImage = StyleKeyword.Null;
                _heroEmoji.style.display = DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("emoji")]
        [CreateProperty]
        public string Emoji
        {
            get => _heroEmoji.text;
            set => _heroEmoji.text = string.IsNullOrEmpty(value) ? "🍝" : value;
        }
    }
}
