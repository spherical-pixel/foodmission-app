using System;
using eu.foodmission.platform.Components;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class TestSurveyScreen : StepFlowScreenBase<TestSurveyViewModel>
    {
        protected override int StepCount => 3;
        protected override string[] StepLabels => new string[] { "Diet", "Rating", "Feedback" };

        protected override string NextButtonLabel => "Next";
        protected override string PreviousButtonLabel => "Back";
        protected override string CompleteButtonLabel => "Submit";

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            switch (stepIndex)
            {
                case 0:
                    return BuildStep1();
                case 1:
                    return BuildStep2();
                case 2:
                    return BuildStep3();
                default:
                    return new VisualElement();
            }
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            // Simple companion guide card styled via USS
            var card = new VisualElement();
            card.AddToClassList("fm-step-flow__guide-card");

            var title = new Unity.AppUI.UI.Heading { text = "Nutri Guide 🌱" };

            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.size = HeadingSize.XL;
            card.Add(title);

            var message = new Unity.AppUI.UI.Text { text = "Please answer the questions below to customize your experience!" };
            message.style.whiteSpace = WhiteSpace.Normal;
            card.Add(message);

            slot.Add(card);
        }

        private VisualElement BuildStep1()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            var title = new Unity.AppUI.UI.Heading { text = "What is your diet preference?", size = HeadingSize.M };
            title.style.marginBottom = 16;
            root.Add(title);

            var veganCheckbox = new FormFieldItemCheckbox { Text = "Vegan", CheckboxValue = _viewModel.IsVegan ? CheckboxState.Checked : CheckboxState.Unchecked };
            var veganCb = veganCheckbox.Q<Checkbox>();
            if (veganCb != null)
            {
                veganCb.RegisterValueChangedCallback(evt =>
                {
                    _viewModel.IsVegan = evt.newValue == CheckboxState.Checked;
                    _viewModel.InvalidateValidation();
                });
            }
            root.Add(veganCheckbox);

            var vegetarianCheckbox = new FormFieldItemCheckbox { Text = "Vegetarian", CheckboxValue = _viewModel.IsVegetarian ? CheckboxState.Checked : CheckboxState.Unchecked };
            var vegCb = vegetarianCheckbox.Q<Checkbox>();
            if (vegCb != null)
            {
                vegCb.RegisterValueChangedCallback(evt =>
                {
                    _viewModel.IsVegetarian = evt.newValue == CheckboxState.Checked;
                    _viewModel.InvalidateValidation();
                });
            }
            root.Add(vegetarianCheckbox);

            var omnivoreCheckbox = new FormFieldItemCheckbox { Text = "Omnivore", CheckboxValue = _viewModel.IsOmnivore ? CheckboxState.Checked : CheckboxState.Unchecked };
            var omniCb = omnivoreCheckbox.Q<Checkbox>();
            if (omniCb != null)
            {
                omniCb.RegisterValueChangedCallback(evt =>
                {
                    _viewModel.IsOmnivore = evt.newValue == CheckboxState.Checked;
                    _viewModel.InvalidateValidation();
                });
            }
            root.Add(omnivoreCheckbox);

            return root;
        }

        private VisualElement BuildStep2()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            var title = new Unity.AppUI.UI.Heading { text = "How would you rate the app?", size = HeadingSize.M };
            title.style.marginBottom = 16;
            root.Add(title);

            var stepper = new FMArrowStepper
            {
                Choices = new string[] { "Select rating...", "1 Star", "2 Stars", "3 Stars", "4 Stars", "5 Stars" },
                SelectedIndex = _viewModel.RatingValue
            };

            stepper.valueChanged += (sender, evt) =>
            {
                _viewModel.RatingValue = evt.newValue;
                _viewModel.InvalidateValidation();
            };
            root.Add(stepper);

            return root;
        }

        private VisualElement BuildStep3()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            var title = new Unity.AppUI.UI.Heading { text = "Any additional feedback?", size = HeadingSize.M };
            title.style.marginBottom = 16;
            root.Add(title);

            var textField = new Unity.AppUI.UI.TextField
            {
                placeholder = "Write your feedback here...",
                value = _viewModel.FeedbackText
            };

            // Register on the inner text field directly for real-time validation
            var innerField = textField.Q<UnityEngine.UIElements.TextField>();
            if (innerField != null)
            {
                innerField.RegisterValueChangedCallback(evt =>
                {
                    _viewModel.FeedbackText = evt.newValue;
                    _viewModel.InvalidateValidation();
                });
            }
            else
            {
                textField.RegisterValueChangedCallback(evt =>
                {
                    _viewModel.FeedbackText = evt.newValue;
                    _viewModel.InvalidateValidation();
                });
            }

            root.Add(textField);

            return root;
        }
    }
}
