using System;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Unity.Properties;
using UnityEngine;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemArrowStepperSettings : VisualElement
    {
        /* ========= UXML ATTRIBUTES ========= */

        [UxmlAttribute("Heading-Text")]
        [CreateProperty]

		public string HeadingText
        {
            get => _heading?.text ?? "";
            set
            {
                if( _heading != null)
                {
                    _heading.text = value;
                }
            }
        }

        [UxmlAttribute("stepper-choices")] [CreateProperty]
        public string[] Choices
        {
            get => _stepper.Choices ?? Array.Empty<string>();
            set
            {
                if( _stepper != null)
                {
                    _stepper.Choices = value;
                }
            }
        }

        [UxmlAttribute("stepper-cyclic")] [CreateProperty]
        public bool Cyclic
        {
            get => _stepper?.Cyclic ?? true;
            set
            {
                if (_stepper != null)
                {
                    _stepper.Cyclic = value;
                }
            }
        }

        [UxmlAttribute("stepper-selected-index")] [CreateProperty]
        public int SelectedIndex
        {
            get => _stepper != null ? _stepper.SelectedIndex : 0;
            set
            {
                if( _stepper != null)
                {
                    _stepper.SelectedIndex = value; 
                }
            }
        }

        public string SelectedValue => _stepper != null ? Choices[_stepper.SelectedIndex] : "";

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

        private void OnStepperValueChanged(object sender, ChangeEvent<int> e)
        {
            this.NotifyPropertyChanged(nameof(SelectedIndex));
            valueChanged?.Invoke(this, e);
        }

        /* ========= INTERNAL ELEMENTS ========= */
        private FMArrowStepper _stepper;
        protected Unity.AppUI.UI.Heading _heading;
        

        public FormFieldItemArrowStepperSettings()
        {
            this.style.flexDirection = FlexDirection.Row;
            this.style.justifyContent = Justify.SpaceBetween;
            this.style.alignItems = Align.Center;
            this.style.flexGrow = 0;

            _heading = new Unity.AppUI.UI.Heading();
            _heading.AddToClassList("heading-wrap");
            _heading.AddToClassList("stepper-heading");
            _heading.size = HeadingSize.M;
            _heading.primary = true;
            _heading.style.flexShrink = 1;
            Add(_heading);

            _stepper = new FMArrowStepper();
            _stepper.RemoveFromClassList("fm-simple-stepper");
            _stepper.valueChanged += OnStepperValueChanged;
            _stepper.style.flexShrink = 0;
            Add(_stepper);

            
            
        }
    }
}
