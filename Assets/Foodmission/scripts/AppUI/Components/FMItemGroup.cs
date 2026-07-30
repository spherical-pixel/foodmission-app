using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemGroup : VisualElement
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

        [UxmlAttribute("detail")]
        [CreateProperty]
        public string Detail
        {
            get => _detailText?.text ?? "";
            set
            {
                if (_detailText != null)
                {
                    _detailText.text = value;
                    _detailText.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        [UxmlAttribute("count-text")]
        [CreateProperty]
        public string CountText
        {
            get => _countBadge?.text ?? "";
            set
            {
                if (_countBadge != null)
                {
                    _countBadge.text = value;
                    _countBadge.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        /* ========= INTERNAL ELEMENTS ========= */
        protected VisualElement _textContainer;
        protected Unity.AppUI.UI.Text _titleText;
        protected Unity.AppUI.UI.Text _detailText;
        protected Unity.AppUI.UI.Button _openButton;
        protected VisualElement _rightContainer;
        protected Unity.AppUI.UI.Text _countBadge;

        public Unity.AppUI.UI.Button OpenButton => _openButton;
        public Unity.AppUI.UI.Text CountBadge => _countBadge;
        public Unity.AppUI.UI.Text TitleText => _titleText;
        public Unity.AppUI.UI.Text DetailText => _detailText;

        public FMItemGroup()
        {
            this.AddToClassList("fm-groups-item");


            _textContainer = new VisualElement();
            _textContainer.AddToClassList("fm-pantry-item-text-container");
            this.Add(_textContainer);

            _titleText = new Unity.AppUI.UI.Text();
            _titleText.primary = true;
            _titleText.AddToClassList("fm-groups-item-title");
            _textContainer.Add(_titleText);

            _detailText = new Unity.AppUI.UI.Text();
            _detailText.AddToClassList("fm-groups-item-detail");
            _detailText.style.display = DisplayStyle.None;
            _textContainer.Add(_detailText);

            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;
            _textContainer.Add(_openButton);

            _rightContainer = new VisualElement();
            _rightContainer.AddToClassList("fm-pantry-item-buttons-container");
            this.Add(_rightContainer);

            _countBadge = new Unity.AppUI.UI.Text();
            _countBadge.AddToClassList("fm-p-item-expiry");
            _countBadge.style.marginRight = 8;
            _countBadge.style.display = DisplayStyle.None;
            _rightContainer.Add(_countBadge);
        }
    }
}
