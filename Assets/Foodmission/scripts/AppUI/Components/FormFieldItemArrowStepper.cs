using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemArrowStepper : FormFieldItemBase
    {
        /* ========= UXML ATTRIBUTES ========= */

        [UxmlAttribute("stepper-choices")] [CreateProperty]
        public string[] Choices
        {
            get => _choices ?? Array.Empty<string>();
            set
            {
                _choices = value ?? Array.Empty<string>();
                // Clamp selected index if necessary
                if (_selectedIndex >= _choices.Length)
                {
                    _selectedIndex = Mathf.Max(0, _choices.Length - 1);
                }
                UpdateLabel();
                UpdateButtonStates();
            }
        }

        [UxmlAttribute("stepper-selected-index")] [CreateProperty]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                var newIndex = Mathf.Clamp(value, 0, Mathf.Max(0, Choices.Length - 1));
                if (newIndex != _selectedIndex)
                {
                    var oldIndex = _selectedIndex;
                    _selectedIndex = newIndex;
                    UpdateLabel();
                    UpdateButtonStates();
                    this.NotifyPropertyChanged(nameof(SelectedIndex));

                    // Fire value changed event
                    var changeEvent = ChangeEvent<int>.GetPooled(oldIndex, newIndex);
                    changeEvent.target = this;
                    valueChanged?.Invoke(this, changeEvent);
                }
            }
        }

        public string SelectedValue => _selectedIndex >= 0 && _selectedIndex < Choices.Length ? Choices[_selectedIndex] : "";

        /* ========= EVENTS ========= */
        public event System.EventHandler<ChangeEvent<int>> valueChanged;

        public void RegisterValueChangedCallback(System.EventHandler<ChangeEvent<int>> callback)
        {
            valueChanged += callback;
        }

        public void UnregisterValueChangedCallback(System.EventHandler<ChangeEvent<int>> callback)
        {
            valueChanged -= callback;
        }

        /* ========= INTERNAL ELEMENTS ========= */
        private VisualElement _stepperRow;
        private IconButton _prevButton;
        private Text _valueLabel;
        private IconButton _nextButton;

        private string[] _choices = Array.Empty<string>();
        private int _selectedIndex = 0;

        public override AccessibilityNode CreateAccessibilityNode(AccessibilityHierarchy hierarchy, string label)
        {
            base.CreateAccessibilityNode(hierarchy, label);
            if (_accessibilityNode == null) return null;

            _accessibilityNode.role = AccessibilityRole.Slider;
            _accessibilityNode.value = SelectedValue;
            _accessibilityNode.frameGetter = MakeFrameGetter(this);

            _accessibilityNode.incremented += OnStepperIncremented;
            _accessibilityNode.decremented += OnStepperDecremented;

            return _accessibilityNode;
        }

        public override void UpdateAccessibilityNode()
        {
            if (_accessibilityNode != null)
            {
                _accessibilityNode.value = SelectedValue;
            }
        }

        public override void DestroyAccessibilityNode()
        {
            if (_accessibilityNode != null)
            {
                _accessibilityNode.incremented -= OnStepperIncremented;
                _accessibilityNode.decremented -= OnStepperDecremented;
            }
            base.DestroyAccessibilityNode();
        }

        private void OnStepperIncremented()
        {
            if (_selectedIndex < Choices.Length - 1)
            {
                SelectedIndex++;
            }
        }

        private void OnStepperDecremented()
        {
            if (_selectedIndex > 0)
            {
                SelectedIndex--;
            }
        }

        public FormFieldItemArrowStepper()
        {
            // Create the stepper row layout
            _stepperRow = new VisualElement();
            _stepperRow.style.flexDirection = FlexDirection.Row;
            _stepperRow.style.alignItems = Align.Center;
            _stepperRow.AddToClassList("fm-arrow-stepper__row");

            // Previous button
            _prevButton = new IconButton();
            _prevButton.icon = "caret-left";
            _prevButton.quiet = true;
            _prevButton.clicked += OnPrevClicked;
            _stepperRow.Add(_prevButton);

            // Value label
            _valueLabel = new Text();
            _valueLabel.style.flexGrow = 1;
            _valueLabel.AddToClassList("fm-arrow-stepper__label");
            _stepperRow.Add(_valueLabel);

            // Next button
            _nextButton = new IconButton();
            _nextButton.icon = "caret-right";
            _nextButton.quiet = true;
            _nextButton.clicked += OnNextClicked;
            _stepperRow.Add(_nextButton);

            // Add to field container
            _fieldContainer.Add(_stepperRow);

            UpdateLabel();
            UpdateButtonStates();
        }

        private void OnPrevClicked()
        {
            SelectedIndex--;
        }

        private void OnNextClicked()
        {
            SelectedIndex++;
        }

        private void UpdateLabel()
        {
            if (_valueLabel != null)
            {
                _valueLabel.text = SelectedValue;
            }
        }

        private void UpdateButtonStates()
        {
            if (_prevButton != null)
            {
                _prevButton.SetEnabled(_selectedIndex > 0);
            }
            if (_nextButton != null)
            {
                _nextButton.SetEnabled(_selectedIndex < Choices.Length - 1);
            }
        }
    }
}
