using System;
using System.Collections.Generic;
using UnityEngine;

using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMQuantityUnitPanel : ExVisualElement
    {

        public float Quantity
        {
            get => _qtyField.value;
            set => _qtyField.value = value;
        }

        public string Unit
        {
            get
            {
                IReadOnlyList<string> codes = UnitCatalog.Current.Codes;
                int idx = _unitDropdown != null ? _unitDropdown.selectedIndex : -1;
                return idx >= 0 && idx < codes.Count ? codes[idx] : UnitCodes.Default;
            }
            set
            {
                SetUnitWithoutNotify(value);
            }
        }

        public bool IsQuantityModified { get; private set; }
        public bool IsUnitModified { get; private set; }

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

            List<string> unitLabels = new List<string>(UnitCatalog.Current.Labels);
            _unitDropdown = new Dropdown();
            _unitDropdown.bindItem = (item, i) => item.label = unitLabels[i];
            _unitDropdown.sourceItems = unitLabels;
            _unitDropdown.SetValueWithoutNotify(new[] { 0 });
            _unitDropdown.style.marginBottom = 8;
            Add(_unitDropdown);

            _qtyField.RegisterValueChangedCallback(evt => IsQuantityModified = true);
            _unitDropdown.RegisterValueChangedCallback(evt => IsUnitModified = true);
        }

        public void SetQuantityWithoutNotify(float value)
        {
            _qtyField.SetValueWithoutNotify(value);
        }

        public void SetUnitWithoutNotify(string unit)
        {
            if (_unitDropdown == null)
            {
                return;
            }
            int idx = IndexOfCode(unit);
            if (idx >= 0)
            {
                _unitDropdown.SetValueWithoutNotify(new[] { idx });
            }
        }

        private static int IndexOfCode(string code)
        {
            IReadOnlyList<string> codes = UnitCatalog.Current.Codes;
            for (int i = 0; i < codes.Count; i++)
            {
                if (string.Equals(codes[i], code, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
