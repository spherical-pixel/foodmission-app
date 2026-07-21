using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.AppUI.UI;

using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMQuantityUnitPanel : ExVisualElement
    {

        private static List<string> _unitValues;
        private static List<string> _unitLabels;
        private static string _cachedLang;

        public static List<string> UnitValues => _unitValues;
        public static List<string> UnitChoices => _unitLabels;

        public static async Task InitializeAsync(ICatalogService catalogService, string lang)
        {
            if (_unitValues != null && _cachedLang == lang) return;

            try
            {
                var (units, error) = await catalogService.GetUnitsAsync(lang);
                if (error == null && units != null && units.Length > 0)
                {
                    _unitValues = new List<string>(units.Length);
                    _unitLabels = new List<string>(units.Length);
                    foreach (var u in units)
                    {
                        _unitValues.Add(u.code);
                        _unitLabels.Add(u.label);
                    }
                    Debug.Log($"[FMQuantityUnitPanel] Units loaded from API: {_unitValues.Count} (lang={lang})");
                    _cachedLang = lang;
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FMQuantityUnitPanel] Failed to load units from API: {ex.Message}");
            }

            Debug.Log("[FMQuantityUnitPanel] Using default unit catalog");
        }

        public float Quantity
        {
            get => _qtyField.value;
            set => _qtyField.value = value;
        }

        public string Unit
        {
            get => _unitDropdown != null && _unitDropdown.selectedIndex >= 0
                ? UnitValues[_unitDropdown.selectedIndex]
                : "PIECES";
            set
            {
                if (_unitDropdown == null) return;
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
            if (_unitDropdown == null) return;
            int idx = UnitValues.IndexOf(unit);
            if (idx >= 0)
                _unitDropdown.SetValueWithoutNotify(new[] { idx });
        }
    }
}
