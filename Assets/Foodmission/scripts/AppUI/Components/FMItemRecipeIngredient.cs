using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Reusable row component for displaying an ingredient line item in Recipe Editor.
    /// Follows the FMItemPantry / FMItemShoppingListDetail design pattern.
    /// </summary>
    [UxmlElement]
    public partial class FMItemRecipeIngredient : VisualElement
    {
        private readonly Text _nameText;
        private readonly Text _measureText;
        private readonly VisualElement _textContainer;
        private readonly VisualElement _buttonsContainer;
        private readonly Unity.AppUI.UI.Button _removeButton;

        public FMItemRecipeIngredient()
        {
            AddToClassList("fm-re-ingredient-row");

            _textContainer = new VisualElement();
            _textContainer.AddToClassList("fm-re-ingredient-text-container");
            Add(_textContainer);

            _nameText = new Text();
            _nameText.AddToClassList("fm-re-ingredient-name");
            _textContainer.Add(_nameText);

            _measureText = new Text();
            _measureText.AddToClassList("fm-re-ingredient-measure");
            _textContainer.Add(_measureText);

            _buttonsContainer = new VisualElement();
            _buttonsContainer.AddToClassList("fm-re-ingredient-buttons-container");
            Add(_buttonsContainer);

            _removeButton = new Unity.AppUI.UI.Button();
            _removeButton.quiet = true;
            _removeButton.leadingIcon = "close";
            _removeButton.size = Size.S;
            _removeButton.AddToClassList("fm-re-ingredient-remove");
            _buttonsContainer.Add(_removeButton);
        }

        [UxmlAttribute("name-text")]
        [CreateProperty]
        public string NameText
        {
            get => _nameText.text;
            set => _nameText.text = value;
        }

        [UxmlAttribute("measure-text")]
        [CreateProperty]
        public string MeasureText
        {
            get => _measureText.text;
            set
            {
                _measureText.text = value;
                _measureText.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public Unity.AppUI.UI.Button RemoveButton => _removeButton;
    }
}
