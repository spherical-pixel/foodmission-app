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
                int idx = _unitDropdown != null ? _unitDropdown.selectedIndex : -1;
                return _unitChoices.CodeAt(idx);
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
        private readonly UnitChoiceSnapshot _unitChoices;

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

            _unitChoices = UnitChoiceSnapshot.From(UnitCatalog.Current);
            _unitDropdown = new Dropdown();
            _unitDropdown.bindItem = (item, i) => item.label = _unitChoices.Labels[i];
            _unitDropdown.sourceItems = _unitChoices.Labels;
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
            int idx = _unitChoices.IndexOf(unit);
            if (idx >= 0)
            {
                _unitDropdown.SetValueWithoutNotify(new[] { idx });
            }
        }
    }
}
