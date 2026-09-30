using Unity.AppUI.UI;
using Unity.Properties;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Card for a food waste record: food name, "quantity unit · reason", date and a remove button.
    /// </summary>
    [UxmlElement]
    public partial class FMItemFoodWaste : VisualElement
    {
        private readonly Unity.AppUI.UI.Text _titleText;
        private readonly Unity.AppUI.UI.Text _detailText;
        private readonly Unity.AppUI.UI.Text _dateText;
        private readonly Unity.AppUI.UI.Button _removeButton;

        [UxmlAttribute("text")]
        [CreateProperty]
        public string Text
        {
            get => _titleText.text;
            set => _titleText.text = value;
        }

        [UxmlAttribute("detail")]
        [CreateProperty]
        public string Detail
        {
            get => _detailText.text;
            set
            {
                _detailText.text = value;
                _detailText.EnableInClassList("fm-waste-item-hidden", string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("date-text")]
        [CreateProperty]
        public string DateText
        {
            get => _dateText.text;
            set
            {
                _dateText.text = value;
                _dateText.EnableInClassList("fm-waste-item-hidden", string.IsNullOrEmpty(value));
            }
        }

        public Unity.AppUI.UI.Button RemoveButton => _removeButton;

        public FMItemFoodWaste()
        {
            AddToClassList("fm-pantry-item");
            AddToClassList("fm-waste-item");

            var textContainer = new VisualElement();
            textContainer.AddToClassList("fm-pantry-item-text-container");
            Add(textContainer);

            _titleText = new Unity.AppUI.UI.Text { primary = true };
            _titleText.AddToClassList("fm-pantry-item-title");
            textContainer.Add(_titleText);

            _detailText = new Unity.AppUI.UI.Text();
            _detailText.AddToClassList("fm-pantry-item-detail");
            _detailText.AddToClassList("fm-waste-item-hidden");
            textContainer.Add(_detailText);

            var buttonsContainer = new VisualElement();
            buttonsContainer.AddToClassList("fm-pantry-item-buttons-container");
            Add(buttonsContainer);

            _dateText = new Unity.AppUI.UI.Text();
            _dateText.AddToClassList("fm-waste-item-date");
            _dateText.AddToClassList("fm-waste-item-hidden");
            buttonsContainer.Add(_dateText);

            _removeButton = new Unity.AppUI.UI.Button
            {
                quiet = true,
                leadingIcon = "fm-trash",
                size = Size.S
            };
            _removeButton.AddToClassList("fm-icon-button-item-list");
            buttonsContainer.Add(_removeButton);
        }
    }
}
