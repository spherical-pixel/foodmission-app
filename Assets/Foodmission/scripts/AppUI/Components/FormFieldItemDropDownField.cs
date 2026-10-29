
using System.Linq;
using Unity.AppUI.UI;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;


namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemDropDownField : FormFieldItemBase
    {

        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("dropdown-defaultmessage")] [CreateProperty]
        public string DropdownDefaultMessage
        {
            get => _dropdown?.defaultMessage ?? "";
            set
            {
                if (_dropdown != null)
                {
                    _dropdown.defaultMessage = value;
                }
            }
        }

        [UxmlAttribute("dropdown-selectiontype")] [CreateProperty]
        public PickerSelectionType DropdownSelectionType
        {
            get => _dropdown?.selectionType ?? PickerSelectionType.Single;
            set
            {
                if (_dropdown != null)
                {
                    _dropdown.selectionType = value;
                }
            }
        }

        [UxmlAttribute("dropdown-closeonselect")] [CreateProperty]
        public bool DropdownCloseOnSelect
        {
            get => _dropdown?.closeOnSelection ?? false;
            set
            {
                if (_dropdown != null)
                {
                    _dropdown.closeOnSelection = value;
                }
            }
        }

        /// <summary>
        /// Opens <see cref="FMSearchablePicker"/> instead of the App UI menu, so the user can type
        /// to filter long lists. Labels are the <c>ToString()</c> of each source item.
        /// </summary>
        [UxmlAttribute("dropdown-searchable")] [CreateProperty]
        public bool Searchable
        {
            get => _searchable;
            set
            {
                _searchable = value;
                if (_dropdown != null)
                {
                    _dropdown.clickable = value ? new Pressable(OpenSearchablePicker) : _defaultClickable;
                }
            }
        }

        public Dropdown Dropdown
        {
            get => _dropdown;
            set
            {
                _dropdown = value;
            }
        }

        /* ========= INTERNAL ELEMENTS ========= */
        protected Unity.AppUI.UI.Dropdown _dropdown;
        private readonly Pressable _defaultClickable;
        private bool _searchable;


        public FormFieldItemDropDownField()
        {
            _dropdown = new Unity.AppUI.UI.Dropdown();
            _dropdown.AddToClassList("item-field");
            _fieldContainer.Add(_dropdown);
            _dropdown.RegisterValueChangedCallback(OnDropdownValueChanged);
            _defaultClickable = _dropdown.clickable;
        }

        private void OpenSearchablePicker()
        {
            var items = _dropdown?.sourceItems;
            if (items == null || items.Count == 0)
            {
                return;
            }

            var labels = new System.Collections.Generic.List<string>(items.Count);
            foreach (var item in items)
            {
                labels.Add(item?.ToString() ?? "");
            }

            var current = _dropdown.value?.ToList();
            var selected = current is { Count: > 0 } ? current[0] : -1;

            FMSearchablePicker.Show(this, HeadingText, labels, selected, index =>
            {
                _dropdown.value = new[] { index };
            });
        }

        public override AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label)
        {
            base.CreateAccessibilityNode(hierarchy, label);
            if (_accessibilityNode == null) return null;

            _accessibilityNode.role = AccessibilityRole.Dropdown;
            _accessibilityNode.value = FormatDropdownValue(_dropdown?.value);
            _accessibilityNode.frameGetter = MakeFrameGetter(_dropdown);

            if (_dropdown != null && !_dropdown.enabledSelf)
            {
                _accessibilityNode.state = AccessibilityState.Disabled;
            }

            return _accessibilityNode;
        }

        public override void UpdateAccessibilityNode()
        {
            if (_accessibilityNode != null && _dropdown != null)
            {
                _accessibilityNode.value = FormatDropdownValue(_dropdown.value);
            }
        }

        private static string FormatDropdownValue(System.Collections.IEnumerable value)
        {
            if (value == null) return "";
            var indices = value.Cast<int>().ToList();
            return indices.Count > 0 ? string.Join(",", indices) : "";
        }

        private void OnDropdownValueChanged(ChangeEvent<System.Collections.Generic.IEnumerable<int>> evt)
        {
            UpdateAccessibilityNode();
        }
        
    }
}