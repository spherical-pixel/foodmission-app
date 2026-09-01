using System;
using System.Collections.Generic;
using eu.foodmission.platform.Components;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class PilotSurveyScreen : StepFlowScreenBase<PilotSurveyViewModel>
    {
        protected override int StepCount => _viewModel?.Questions?.Length ?? 1;

        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:txtSubmit";

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;
        private readonly List<VisualElement> _stepContainers = new List<VisualElement>();

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null && args != null && args.Length > 0)
            {
                string slugOrId = args[0].value;
                if (!string.IsNullOrEmpty(slugOrId))
                {
                    await _viewModel.LoadSurveyAsync(slugOrId);
                    OnStepChanged(_viewModel.CurrentStepIndex);
                }
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                OnStepChanged(_viewModel.CurrentStepIndex);
            }
        }

        protected override void OnViewModelUnbinding()
        {
            base.OnViewModelUnbinding();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }

        protected override void OnNavigationRequested(string navigationAction, Argument[] args)
        {
            Debug.Log($"[PilotSurveyScreen] OnNavigationRequested: '{navigationAction}'");
            base.OnNavigationRequested(navigationAction, args);
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Questions) ||
                e.PropertyName == nameof(_viewModel.SurveyDescription) ||
                e.PropertyName == nameof(_viewModel.CurrentSurvey) ||
                e.PropertyName == nameof(_viewModel.StepCount))
            {
                OnStepChanged(_viewModel.CurrentStepIndex);
            }
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            var root = new ExVisualElement();
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));
            root.style.flexGrow = 1;


            if (_viewModel == null || _viewModel.Questions == null || stepIndex >= _viewModel.Questions.Length)
            {
                return root;
            }

            var question = _viewModel.Questions[stepIndex];

            var card = new ExVisualElement();
            card.name = "FMLikertSliderCard";
            // card.AddToClassList("box-background");
            // card.AddToClassList("fm-shadow-wrapper");
            card.style.flexDirection = FlexDirection.Column;
            card.style.width = new StyleLength(Length.Percent(100));
            card.style.justifyContent = Justify.Center;
            card.style.flexGrow = 1;
            card.style.paddingTop = 16;
            card.style.paddingBottom = 16;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;

            var answers = question.answers;
            if (answers == null || answers.Length == 0)
            {
                answers = new AnswerOptionDto[]
                {
                    new AnswerOptionDto { value = 1, label = "Totalmente en desacuerdo" },
                    new AnswerOptionDto { value = 2, label = "En desacuerdo" },
                    new AnswerOptionDto { value = 3, label = "Ni de acuerdo ni en desacuerdo" },
                    new AnswerOptionDto { value = 4, label = "De acuerdo" },
                    new AnswerOptionDto { value = 5, label = "Totalmente de acuerdo" }
                };
            }

            int currentVal = _viewModel.GetAnswer(stepIndex);

            var slider = new FMLikertSlider();
            slider.SetOptions(answers, currentVal);
            slider.OnValueChanged += (val) =>
            {
                _viewModel.SetAnswer(stepIndex, val);
            };

            card.Add(slider);
            root.Add(card);
            return root;
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            var container = new VisualElement();
            slot.Add(container);

            _nutriView = new FMNutriView();
            _nutriView.AddToClassList("fm-step-flow__guide-nutri");
            container.Add(_nutriView);

            _messageCard = new ExVisualElement();
            _messageCard.AddToClassList("box-background");
            _messageCard.AddToClassList("fm-shadow-wrapper");
            _messageCard.AddToClassList("fm-step-flow__guide-card");

            _messageText = new Unity.AppUI.UI.Text { text = "" };
            _messageText.style.whiteSpace = WhiteSpace.Normal;
            //_messageText.style.unityFontStyleAndWeight = FontStyle.Bold;
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

            if (_viewModel != null)
            {
                string message = _viewModel.GetStepNutriMessage(stepIndex);
                UpdateMascotMessage(message);
            }
        }
    }
}
