using System.Collections.Generic;

using Unity.AppUI.UI;

using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMQuantityUnitPanel : ExVisualElement
    {
        public static readonly List<string> UnitValues = new() { "PIECES", "G", "KG", "ML", "L", "CUPS" };

        private static List<string> _unitChoices;

        public static List<string> UnitChoices
        {
            get
            {
                if (_unitChoices == null)
                {
                    _unitChoices = new List<string>
                    {
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_PIECES"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_G"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_KG"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_ML"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_L"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_CUPS"),
                    };
                }
                return _unitChoices;
            }
        }

        public float Quantity
        {
            get => _qtyField.value;
            set => _qtyField.value = value;
        }

        public string Unit
        {
            get => _unitDropdown.selectedIndex >= 0 ? UnitValues[_unitDropdown.selectedIndex] : "PIECES";
            set
            {
                int idx = UnitValues.IndexOf(value);
                if (idx >= 0)
                    _unitDropdown.SetValueWithoutNotify(new[] { idx });
            }
        }

        private readonly Unity.AppUI.UI.FloatField _qtyField;
        private readonly Dropdown _unitDropdown;

        public FMQuantityUnitPanel()
        {
            style.flexDirection = FlexDirection.Column;

            var qtyLabel = new Text { text = "Quantity" };
            qtyLabel.style.marginBottom = 4;
            Add(qtyLabel);

            _qtyField = new Unity.AppUI.UI.FloatField { value = 1f };
            _qtyField.style.marginBottom = 8;
            Add(_qtyField);

            var unitLabel = new Text { text = "Unit" };
            unitLabel.style.marginBottom = 4;
            Add(unitLabel);

            _unitDropdown = new Dropdown();
            _unitDropdown.bindItem = (item, i) => item.label = UnitChoices[i];
            _unitDropdown.sourceItems = UnitChoices;
            _unitDropdown.SetValueWithoutNotify(new[] { 0 });
            _unitDropdown.style.marginBottom = 8;
            Add(_unitDropdown);
        }

        public void SetQuantityWithoutNotify(float value)
        {
            _qtyField.SetValueWithoutNotify(value);
        }

        public void SetUnitWithoutNotify(string unit)
        {
            int idx = UnitValues.IndexOf(unit);
            if (idx >= 0)
                _unitDropdown.SetValueWithoutNotify(new[] { idx });
        }
    }
}
