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

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;

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
                case 3:
                    return BuildStep4();
                default:
                    return new VisualElement();
            }
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            // Simple companion guide card styled via USS
            var container = new VisualElement();
            slot.Add(container);


            var title = new Unity.AppUI.UI.Heading { text = "📊 Your opinion matters" };
            title.AddToClassList("centered-text");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.size = HeadingSize.XL;
            container.Add(title);

            _nutriView = new FMNutriView();
            _nutriView.AddToClassList("fm-step-flow__guide-nutri");
            container.Add(_nutriView);


            _messageCard = new ExVisualElement();
            _messageCard.AddToClassList("box-background");
            _messageCard.AddToClassList("fm-shadow-wrapper");
            _messageCard.AddToClassList("fm-step-flow__guide-card");

            _messageText = new Unity.AppUI.UI.Text { text = "" };
            _messageText.style.whiteSpace = WhiteSpace.Normal;
            _messageText.primary = false;
            _messageCard.Add(_messageText);

            _messageCard.style.display = DisplayStyle.None;
            slot.Add(_messageCard);
        }

        public void UpdateMascotMessage(string newMessage)
        {
            if (_messageCard == null || _messageText == null) return;

            newMessage ??= string.Empty;

            if (string.IsNullOrWhiteSpace(newMessage))
            {
                _messageCard.RemoveFromClassList("fm-step-flow__guide-card--visible");
                _messageCard.AddToClassList("fm-step-flow__guide-card--exit");
                _messageCard.style.display = DisplayStyle.None;
                _messageText.text = string.Empty;
                return;
            }

            if (_messageText.text == newMessage && _messageCard.style.display == DisplayStyle.Flex && _messageCard.ClassListContains("fm-step-flow__guide-card--visible"))
            {
                return;
            }

            _messageCard.style.display = DisplayStyle.Flex;

            _messageCard.RemoveFromClassList("fm-step-flow__guide-card--visible");
            _messageCard.AddToClassList("fm-step-flow__guide-card--exit");

            _messageCard.schedule.Execute(() =>
            {
                _messageText.text = newMessage;
                _messageCard.RemoveFromClassList("fm-step-flow__guide-card--exit");
                _messageCard.AddToClassList("fm-step-flow__guide-card--visible");
            }).StartingIn(150);
        }


        protected override void OnStepChanged(int stepIndex)
        {
            base.OnStepChanged(stepIndex);

            string message = stepIndex switch
            {
                0 => "We want to hear from you! Let's start with a warm welcome.",
                1 => "How many meals containing meat do you typically eat each week?",
                2 => "How would you rate the app?",
                3 => "Any additional feedback?",
                _ => ""
            };

            UpdateMascotMessage(message);
        }

        private VisualElement BuildStep1()
        {
            var root = new ExVisualElement();
            return root;
        }


        private VisualElement BuildStep2()
        {
            var root = new ExVisualElement();
            root.AddToClassList("box-background");
            root.AddToClassList("fm-shadow-wrapper");
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = new StyleLength(Length.Percent(100));

            var veganCheckbox = CreateItemCheckbox("Vegan", _viewModel.IsVegan, value => _viewModel.IsVegan = value);
            var vegetarianCheckBox = CreateItemCheckbox("Vegetarian", _viewModel.IsVegetarian, value => _viewModel.IsVegetarian = value);
            var omnivoreCheckbox = CreateItemCheckbox("Omnivore", _viewModel.IsOmnivore, value => _viewModel.IsOmnivore = value);
            root.Add(vegetarianCheckBox);
            root.Add(omnivoreCheckbox);
            root.Add(veganCheckbox);

            return root;
        }

        private VisualElement CreateItemCheckbox(string label, bool isChecked, Action<bool> onValueChanged)
        {
            var checkbox = new FormFieldItemCheckbox { Text = label, CheckboxValue = isChecked ? CheckboxState.Checked : CheckboxState.Unchecked };
            var cb = checkbox.Q<Checkbox>();
            if (cb != null)
            {
                cb.RegisterValueChangedCallback(evt =>
                {
                    onValueChanged?.Invoke(evt.newValue == CheckboxState.Checked);
                    _viewModel.InvalidateValidation();
                });
            }
            return checkbox;
        }

        private VisualElement BuildStep3()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

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

        private VisualElement BuildStep4()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

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
