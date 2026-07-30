using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemMember : VisualElement
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

        /* ========= INTERNAL ELEMENTS ========= */
        protected VisualElement _textContainer;
        protected Unity.AppUI.UI.Text _titleText;
        protected Unity.AppUI.UI.Text _detailText;
        protected VisualElement _badgesContainer;
        protected Unity.AppUI.UI.Text _adminBadge;
        protected Unity.AppUI.UI.Text _virtualBadge;
        protected VisualElement _buttonsContainer;
        protected Unity.AppUI.UI.Button _makeAdminButton;
        protected Unity.AppUI.UI.Button _editButton;
        protected Unity.AppUI.UI.Button _removeButton;

        public Unity.AppUI.UI.Text TitleText => _titleText;
        public Unity.AppUI.UI.Text DetailText => _detailText;
        public Unity.AppUI.UI.Text AdminBadge => _adminBadge;
        public Unity.AppUI.UI.Text VirtualBadge => _virtualBadge;
        public Unity.AppUI.UI.Button MakeAdminButton => _makeAdminButton;
        // public Unity.AppUI.UI.Button EditButton => _editButton;
        // public Unity.AppUI.UI.Button RemoveButton => _removeButton;

        public FMItemMember()
        {
            this.AddToClassList("fm-group-member-item");

            _textContainer = new VisualElement();
            _textContainer.AddToClassList("fm-group-member-item-text-container");
            this.Add(_textContainer);

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.flexShrink = 1;
            titleRow.style.minWidth = 0;
            _textContainer.Add(titleRow);

            _titleText = new Unity.AppUI.UI.Text();
            _titleText.primary = true;
            _titleText.AddToClassList("fm-group-member-item-title");
            _titleText.style.width = StyleKeyword.Auto;
            _titleText.style.flexShrink = 1;
            titleRow.Add(_titleText);

            _badgesContainer = new VisualElement();
            _badgesContainer.style.flexDirection = FlexDirection.Row;
            _badgesContainer.style.alignItems = Align.Center;
            _badgesContainer.style.flexShrink = 0;
            _badgesContainer.style.marginLeft = 8;
            titleRow.Add(_badgesContainer);

            _adminBadge = new Unity.AppUI.UI.Text();
            _adminBadge.AddToClassList("fm-gd-member-badge");
            _adminBadge.AddToClassList("fm-gd-member-badge--admin");
            _adminBadge.style.display = DisplayStyle.None;
            _badgesContainer.Add(_adminBadge);

            _virtualBadge = new Unity.AppUI.UI.Text();
            _virtualBadge.AddToClassList("fm-gd-member-badge");
            _virtualBadge.AddToClassList("fm-gd-member-badge--virtual");
            _virtualBadge.style.display = DisplayStyle.None;
            _badgesContainer.Add(_virtualBadge);

            _detailText = new Unity.AppUI.UI.Text();
            _detailText.AddToClassList("fm-pantry-item-detail");
            _detailText.style.display = DisplayStyle.None;
            _textContainer.Add(_detailText);

            _buttonsContainer = new VisualElement();
            _buttonsContainer.AddToClassList("fm-pantry-item-buttons-container");
            _buttonsContainer.pickingMode = PickingMode.Position;
            this.Add(_buttonsContainer);

            _makeAdminButton = new Unity.AppUI.UI.Button();
            _makeAdminButton.quiet = true;
            _makeAdminButton.leadingIcon = "user";
            _makeAdminButton.size = Size.S;
            _makeAdminButton.AddToClassList("fm-icon-button-item-list");
            _makeAdminButton.pickingMode = PickingMode.Position;
            _makeAdminButton.style.display = DisplayStyle.None;
            _buttonsContainer.Add(_makeAdminButton);

            // _editButton = new Unity.AppUI.UI.Button();
            // _editButton.quiet = true;
            // _editButton.leadingIcon = "edit";
            // _editButton.size = Size.S;
            // _editButton.AddToClassList("fm-icon-button-item-list");
            // _editButton.style.display = DisplayStyle.None;
            // _buttonsContainer.Add(_editButton);

            // _removeButton = new Unity.AppUI.UI.Button();
            // _removeButton.quiet = true;
            // _removeButton.leadingIcon = "fm-trash";
            // _removeButton.size = Size.S;
            // _removeButton.AddToClassList("fm-icon-button-item-list");
            // _removeButton.style.display = DisplayStyle.None;
            // _buttonsContainer.Add(_removeButton);
        }

        public void SetAdminBadge(bool isVisible, string label = "ADMIN")
        {
            if (_adminBadge != null)
            {
                _adminBadge.text = label;
                _adminBadge.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetVirtualBadge(bool isVisible, string label = "VIRTUAL")
        {
            if (_virtualBadge != null)
            {
                _virtualBadge.text = label;
                _virtualBadge.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
