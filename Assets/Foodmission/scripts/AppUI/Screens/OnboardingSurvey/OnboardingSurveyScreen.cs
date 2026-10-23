using System;
using System.ComponentModel;
using System.Linq;
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
        // The ViewModel's StepCount (welcome + one per backend question) takes over once the questions load
        protected override int StepCount => 1;

        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:txtSubmit";
        protected override bool ShowCloseButton => true;

        protected override bool ShowSkipOnFirstStep => _viewModel != null && _viewModel.IsInOnboardingRun;

        protected override void OnSkipRequested()
        {
            if (_viewModel == null || _viewModel.IsSubmitting)
            {
                return;
            }
            ResetNutriToIdle();
            _viewModel.Skip();
        }

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            TrackLoadingOverlay(() => _viewModel.IsLoading || _viewModel.IsSubmitting, nameof(OnboardingSurveyViewModel.IsLoading), nameof(OnboardingSurveyViewModel.IsSubmitting));
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChangedInternal;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChangedInternal;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            // Questions arrived (or were reloaded in another language) while a question step is shown
            if (e.PropertyName == nameof(StepFlowViewModelBase.StepCount) && _viewModel != null)
            {
                OnStepChanged(_viewModel.CurrentStepIndex);
            }
        }

        // The questions did not load: retry instead of leaving a welcome step that cannot continue
        protected override Action OnApiErrorRetry()
        {
            if (_viewModel == null || _viewModel.HasQuestions)
            {
                return null;
            }
            return () => _ = _viewModel.LoadQuestionsAsync();
        }

        protected override void OnCloseRequested()
        {
            if (_viewModel == null || _viewModel.IsSubmitting)
            {
                return;
            }
            OnboardingExitDialog.Show(this, OnboardingExitDialog.SurveyMessage, () =>
            {
                ResetNutriToIdle();
                _viewModel.Close();
            });
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            // base.OnEnter calls Initialize, which loads the questions
            base.OnEnter(controller, destination, args);

            if (_viewModel != null && args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.name == "fromHome" && arg.value?.ToString() == "true")
                    {
                        _viewModel.FromHome = true;
                    }
                }
            }

            // The Skip button depends on the entry point read above
            RefreshNavigationButtons();
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            if (stepIndex == 0)
            {
                return BuildWelcomeStep();
            }

            OnboardingSurveyQuestion question = _viewModel?.QuestionForStep(stepIndex);
            if (question == null)
            {
                return new VisualElement();
            }

            ExVisualElement container = CreateQuestionStepContainer();
            // label ?? value: an option the backend sent without a label still shows something tappable
            string[] labels = question.options.Select(o => o.label ?? o.value).ToArray();
            int selected = Array.FindIndex(question.options, o => o.value == _viewModel.GetAnswer(question.field));
            PopulateSingleChoiceOptions(container, labels, selected, index =>
            {
                _viewModel.SetAnswer(question.field, index >= 0 ? question.options[index].value : null);
            });
            return BuildCardStep(container);
        }
        private ExVisualElement CreateQuestionStepContainer()
        {
            var container = new ExVisualElement();
            // container.AddToClassList("box-background");
            // container.AddToClassList("fm-shadow-wrapper");
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
            title.size = HeadingSize.L;
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
                ResetNutriToIdle();
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
                TriggerNutriSpeech();
            }).StartingIn(150);
        }

        protected override void OnStepChanged(int stepIndex)
        {
            base.OnStepChanged(stepIndex);

            UpdateMascotMessage(stepIndex == 0 ? "@UI:txtOnboardSurvey_Step_0" : _viewModel?.QuestionForStep(stepIndex)?.text);
        }

        private VisualElement BuildWelcomeStep()
        {
            var root = new ExVisualElement();
            return root;
        }
    }
}
