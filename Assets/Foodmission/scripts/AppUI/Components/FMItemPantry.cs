using System;
using System.Collections.Generic;

using Unity.AppUI.UI;
using Unity.Properties;

using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemPantry : VisualElement
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

        [UxmlAttribute("expiry-text")]
        [CreateProperty]
        public string ExpiryText
        {
            get => _expiryLabel?.text ?? "";
            set
            {
                if (_expiryLabel != null)
                {
                    _expiryLabel.text = value;
                    _expiryLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        /* ========= INTERNAL ELEMENTS ========= */
        protected VisualElement _textContainer;
        protected Unity.AppUI.UI.Text _titleText;
        protected Unity.AppUI.UI.Text _detailText;
        protected Unity.AppUI.UI.Button _openButton;
        protected VisualElement _buttonsContainer;
        protected Unity.AppUI.UI.Text _expiryLabel;
        protected Unity.AppUI.UI.Button _infoButton;
        protected Unity.AppUI.UI.Button _removeButton;

        public Unity.AppUI.UI.Button OpenButton => _openButton;
        public Unity.AppUI.UI.Button InfoButton => _infoButton;
        public Unity.AppUI.UI.Button RemoveButton => _removeButton;
        public Unity.AppUI.UI.Text ExpiryLabel => _expiryLabel;
        public Unity.AppUI.UI.Text TitleText => _titleText;
        public Unity.AppUI.UI.Text DetailText => _detailText;

        public FMItemPantry()
        {
            this.AddToClassList("fm-pantry-item");

            _textContainer = new VisualElement();
            _textContainer.AddToClassList("fm-pantry-item-text-container");
            this.Add(_textContainer);

            _titleText = new Unity.AppUI.UI.Text();
            _titleText.primary = true;
            _titleText.AddToClassList("fm-pantry-item-title");
            _textContainer.Add(_titleText);

            _detailText = new Unity.AppUI.UI.Text();
            _detailText.AddToClassList("fm-pantry-item-detail");
            _detailText.style.display = DisplayStyle.None;
            _textContainer.Add(_detailText);

            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;
            _textContainer.Add(_openButton);

            _buttonsContainer = new VisualElement();
            _buttonsContainer.AddToClassList("fm-pantry-item-buttons-container");
            this.Add(_buttonsContainer);

            _expiryLabel = new Unity.AppUI.UI.Text();
            _expiryLabel.AddToClassList("fm-p-item-expiry");
            _expiryLabel.style.marginRight = 8;
            _expiryLabel.style.display = DisplayStyle.None;
            _buttonsContainer.Add(_expiryLabel);

            _infoButton = new Unity.AppUI.UI.Button();
            _infoButton.quiet = true;
            _infoButton.leadingIcon = "info";
            _infoButton.size = Size.L;
            _infoButton.AddToClassList("fm-icon-button-item-list");
            _buttonsContainer.Add(_infoButton);

            _removeButton = new Unity.AppUI.UI.Button();
            _removeButton.quiet = true;
            _removeButton.leadingIcon = "fm-trash";
            _removeButton.size = Size.L;
            _removeButton.AddToClassList("fm-icon-button-item-list");
            _buttonsContainer.Add(_removeButton);
        }
    }
}
