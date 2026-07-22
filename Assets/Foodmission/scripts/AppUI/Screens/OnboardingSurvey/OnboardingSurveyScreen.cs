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
    public class OnboardingSurveyScreen : StepFlowScreenBase<OnboardingSurveyViewModel>
    {
        protected override int StepCount => 6;
        //protected override string[] StepLabels => new string[] { "Welcome", "Meat", "Beef", "Waste", "Processed", "Refill" };

        protected override string NextButtonLabel => "@UI:txtNext";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:txtSubmit";

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            return stepIndex switch
            {
                0 => BuildWelcomeStep(),
                1 => CreateSingleChoiceQuestionStep(
                    OnboardingSurveyViewModel.MeatMealsOptions,
                    _viewModel.MeatMealsIndex,
                    index =>
                    {
                        _viewModel.MeatMealsIndex = index;
                        _viewModel.InvalidateValidation();
                    }),
                2 => CreateSingleChoiceQuestionStep(
                    OnboardingSurveyViewModel.BeefFrequencyOptions,
                    _viewModel.BeefFrequencyIndex,
                    index =>
                    {
                        _viewModel.BeefFrequencyIndex = index;
                        _viewModel.InvalidateValidation();
                    }),
                3 => CreateSingleChoiceQuestionStep(
                    OnboardingSurveyViewModel.FoodWasteFrequencyOptions,
                    _viewModel.FoodWasteFrequencyIndex,
                    index =>
                    {
                        _viewModel.FoodWasteFrequencyIndex = index;
                        _viewModel.InvalidateValidation();
                    }),
                4 => CreateSingleChoiceQuestionStep(
                    OnboardingSurveyViewModel.UltraProcessedFrequencyOptions,
                    _viewModel.UltraProcessedFrequencyIndex,
                    index =>
                    {
                        _viewModel.UltraProcessedFrequencyIndex = index;
                        _viewModel.InvalidateValidation();
                    }),
                5 => CreateSingleChoiceQuestionStep(
                    OnboardingSurveyViewModel.ReusableContainersFrequencyOptions,
                    _viewModel.ReusableContainersFrequencyIndex,
                    index =>
                    {
                        _viewModel.ReusableContainersFrequencyIndex = index;
                        _viewModel.InvalidateValidation();
                    }),
                _ => new VisualElement()
            };
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            var container = new VisualElement();
            slot.Add(container);

            var title = new Unity.AppUI.UI.Heading { text = "📋 Onboarding Survey" };
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

            slot.Add(_messageCard);

            _messageCard.schedule.Execute(() =>
            {
                _messageCard.AddToClassList("fm-step-flow__guide-card--visible");
            }).StartingIn(50);
        }

        public void UpdateMascotMessage(string newMessage)
        {
            if (_messageText == null || _messageText.text == newMessage) return;

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
                0 => "@UI:txtOnboardSurvey_Step_0",
                1 => "@UI:txtOnboardSurvey_Step_1",
                2 => "@UI:txtOnboardSurvey_Step_2",
                3 => "@UI:txtOnboardSurvey_Step_3",
                4 => "@UI:txtOnboardSurvey_Step_4",
                5 => "@UI:txtOnboardSurvey_Step_5",
                _ => ""
            };

            UpdateMascotMessage(message);
        }

        private VisualElement BuildWelcomeStep()
        {
            var root = new ExVisualElement();


            return root;
        }

        private VisualElement CreateSingleChoiceQuestionStep(LocalizedOption[] options, int currentSelectedIndex, Action<int> onOptionSelected)
        {
            var container = new ExVisualElement();
            container.AddToClassList("box-background");
            container.AddToClassList("fm-shadow-wrapper");
            container.style.flexDirection = FlexDirection.Column;
            container.style.width = new StyleLength(Length.Percent(100));
            container.style.paddingTop = 16;
            container.style.paddingBottom = 16;
            container.style.paddingLeft = 16;
            container.style.paddingRight = 16;

            var checkboxes = new FormFieldItemCheckbox[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var checkboxItem = new FormFieldItemCheckbox
                {
                    Text = options[i].GetText(),
                    CheckboxValue = (currentSelectedIndex == index) ? CheckboxState.Checked : CheckboxState.Unchecked
                };

                var cb = checkboxItem.Q<Checkbox>();
                if (cb != null)
                {
                    cb.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue == CheckboxState.Checked)
                        {
                            for (int j = 0; j < checkboxes.Length; j++)
                            {
                                if (j != index && checkboxes[j] != null)
                                {
                                    checkboxes[j].CheckboxValue = CheckboxState.Unchecked;
                                }
                            }
                            onOptionSelected?.Invoke(index);
                        }
                        else
                        {
                            bool anyChecked = false;
                            for (int j = 0; j < checkboxes.Length; j++)
                            {
                                if (checkboxes[j] != null && checkboxes[j].CheckboxValue == CheckboxState.Checked)
                                {
                                    anyChecked = true;
                                    break;
                                }
                            }
                            if (!anyChecked)
                            {
                                onOptionSelected?.Invoke(-1);
                            }
                        }
                    });
                }

                checkboxes[i] = checkboxItem;
                container.Add(checkboxItem);
            }

            return container;
        }
    }
}
