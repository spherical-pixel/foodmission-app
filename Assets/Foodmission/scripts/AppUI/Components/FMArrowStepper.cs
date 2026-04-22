using System;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Unity.Properties;
using UnityEngine;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMArrowStepper : VisualElement
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

        [UxmlAttribute("cyclic")] [CreateProperty]
        public bool Cyclic
        {
            get => _cyclic;
            set
            {
                _cyclic = value;
                UpdateButtonStates();
            }
        }

        private string[] _choices = Array.Empty<string>();
        private int _selectedIndex = 0;
        private bool _cyclic = true;

        public FMArrowStepper()
        {
            AddToClassList("fm-simple-stepper");
            // Create the stepper row layout
            _stepperRow = new VisualElement();
            _stepperRow.style.flexDirection = FlexDirection.Row;
            _stepperRow.style.alignItems = Align.Center;
            _stepperRow.AddToClassList("fm-arrow-stepper__row");

            // Previous button
            _prevButton = new IconButton();
            _prevButton.icon = "fm-arrow-left";
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
            _nextButton.icon = "fm-arrow-right";
            _nextButton.quiet = true;
            _nextButton.clicked += OnNextClicked;
            _stepperRow.Add(_nextButton);

            Add(_stepperRow);

            UpdateLabel();
            UpdateButtonStates();
        }

        private void OnPrevClicked()
        {
            if (_cyclic && _selectedIndex == 0)
                SelectedIndex = Choices.Length - 1;
            else
                SelectedIndex--;
        }

        private void OnNextClicked()
        {
            if (_cyclic && _selectedIndex == Choices.Length - 1)
                SelectedIndex = 0;
            else
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
                _prevButton.SetEnabled(_cyclic || _selectedIndex > 0);
            }
            if (_nextButton != null)
            {
                _nextButton.SetEnabled(_cyclic || _selectedIndex < Choices.Length - 1);
            }
        }
    }
}
