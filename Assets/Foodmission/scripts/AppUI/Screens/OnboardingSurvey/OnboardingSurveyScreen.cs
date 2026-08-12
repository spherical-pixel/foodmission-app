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


        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:txtSubmit";

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;

        private ExVisualElement _meatContainer;
        private ExVisualElement _beefContainer;
        private ExVisualElement _foodWasteContainer;
        private ExVisualElement _ultraProcessedContainer;
        private ExVisualElement _reusableContainer;

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChangedInternal;
            }
        }

        private void OnViewModelPropertyChangedInternal(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.MeatMealsOptions))
            {
                PopulateAllQuestionOptions();
            }
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null)
            {
                await _viewModel.LoadCatalogOptionsAsync();
                PopulateAllQuestionOptions();
            }
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            return stepIndex switch
            {
                0 => BuildWelcomeStep(),
                1 => BuildCardStep(_meatContainer = CreateQuestionStepContainer()),
                2 => BuildCardStep(_beefContainer = CreateQuestionStepContainer()),
                3 => BuildCardStep(_foodWasteContainer = CreateQuestionStepContainer()),
                4 => BuildCardStep(_ultraProcessedContainer = CreateQuestionStepContainer()),
                5 => BuildCardStep(_reusableContainer = CreateQuestionStepContainer()),
                _ => new VisualElement()
            };
        }

        private ExVisualElement CreateQuestionStepContainer()
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
            return container;
        }

        private VisualElement BuildCardStep(VisualElement element)
        {
            var root = new ExVisualElement();
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));

            if (element != null)
            {
                root.Add(element);
            }
            return root;
        }

        private void PopulateAllQuestionOptions()
        {
            if (_viewModel == null) return;

            PopulateSingleChoiceOptions(_meatContainer, _viewModel.MeatMealsOptions, _viewModel.MeatMealsIndex, idx =>
            {
                _viewModel.MeatMealsIndex = idx;
                _viewModel.InvalidateValidation();
            });

            PopulateSingleChoiceOptions(_beefContainer, _viewModel.BeefFrequencyOptions, _viewModel.BeefFrequencyIndex, idx =>
            {
                _viewModel.BeefFrequencyIndex = idx;
                _viewModel.InvalidateValidation();
            });

            PopulateSingleChoiceOptions(_foodWasteContainer, _viewModel.FoodWasteFrequencyOptions, _viewModel.FoodWasteFrequencyIndex, idx =>
            {
                _viewModel.FoodWasteFrequencyIndex = idx;
                _viewModel.InvalidateValidation();
            });

            PopulateSingleChoiceOptions(_ultraProcessedContainer, _viewModel.UltraProcessedFrequencyOptions, _viewModel.UltraProcessedFrequencyIndex, idx =>
            {
                _viewModel.UltraProcessedFrequencyIndex = idx;
                _viewModel.InvalidateValidation();
            });

            PopulateSingleChoiceOptions(_reusableContainer, _viewModel.ReusableContainersFrequencyOptions, _viewModel.ReusableContainersFrequencyIndex, idx =>
            {
                _viewModel.ReusableContainersFrequencyIndex = idx;
                _viewModel.InvalidateValidation();
            });
        }

        private void PopulateSingleChoiceOptions(ExVisualElement container, string[] options, int currentSelectedIndex, Action<int> onOptionSelected)
        {
            if (container == null) return;
            container.Clear();

            options ??= Array.Empty<string>();
            var checkboxes = new FormFieldItemCheckbox[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var checkboxItem = new FormFieldItemCheckbox
                {
                    Text = options[i],
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
    }
}
