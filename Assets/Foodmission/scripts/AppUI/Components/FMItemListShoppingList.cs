using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.AppUI.UI;

using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using Unity.Properties;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMItemListShoppingList : VisualElement
    {


        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("text")]
        [CreateProperty]
        public string Text
        {
            get => _text?.text ?? "";
            set
            {
                if (_text != null)
                {
                    _text.text = value;
                }
            }
        }


        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Text _text;
        protected Unity.AppUI.UI.Button _openButton;
        protected Unity.AppUI.UI.Button _removeButton;

        public Unity.AppUI.UI.Button OpenButton => _openButton;
        public Unity.AppUI.UI.Button RemoveButton => _removeButton;

        public FMItemListShoppingList()
        {
            this.AddToClassList("fm-shopping-list-item-list");

            _text = new Unity.AppUI.UI.Text();
            _text.primary = true;
            this.Add(_text);

            _openButton = new Unity.AppUI.UI.Button();
            _openButton.AddToClassList("fm-full-button");
            _openButton.quiet = true;

            _text.Add(_openButton);

            _removeButton = new Unity.AppUI.UI.Button();
            _removeButton.style.position = Position.Absolute;
            _removeButton.style.right = 0;
            _removeButton.quiet = true;
            _removeButton.leadingIcon = "fm-trash";
            _removeButton.size = Size.S;
            _removeButton.AddToClassList("fm-icon-button-item-list");
            this.Add(_removeButton);



        }


    }
}
