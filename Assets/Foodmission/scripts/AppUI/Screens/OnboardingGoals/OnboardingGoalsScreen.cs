using System;
using System.Collections.Generic;
using eu.foodmission.platform.Components;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class OnboardingGoalsScreen : StepFlowScreenBase<OnboardingGoalsViewModel>
    {
        protected override int StepCount => 7;

        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:TXT_DONE";

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;

        private readonly ExVisualElement[] _dimensionContainers = new ExVisualElement[6];
        private readonly List<FormFieldItemCheckbox> _checkboxItems = new();

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChangedInternal;
                _viewModel.OnSelectionChanged += SyncCheckboxesFromViewModel;
                _viewModel.OnDimensionsLoaded += UpdateCheckboxLabelsFromBackend;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChangedInternal;
                _viewModel.OnSelectionChanged -= SyncCheckboxesFromViewModel;
                _viewModel.OnDimensionsLoaded -= UpdateCheckboxLabelsFromBackend;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChangedInternal(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.ErrorDetail) && _viewModel.ErrorDetail != null)
            {
                string errorMsg = _viewModel.ErrorDetail.message == "GOALS_VALIDATION_ERROR"
                    ? "@UI:GOALS_VALIDATION_ERROR"
                    : _viewModel.ErrorDetail.message;
                UpdateMascotMessage(errorMsg);
            }
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null)
            {
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (arg.name == "fromEditProfile" && arg.value?.ToString() == "true")
                        {
                            _viewModel.FromEditProfile = true;
                        }
                        else if (arg.name == "fromHome" && arg.value?.ToString() == "true")
                        {
                            _viewModel.FromHome = true;
                        }
                    }
                }

                _viewModel.LoadInitialGoals();
                PopulateAllDimensionCheckboxes();
                _ = _viewModel.EnsureDimensionsLoadedAsync();
            }
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            if (stepIndex == 0)
            {
                return BuildWelcomeStep();
            }

            int dimIndex = stepIndex - 1;
            if (dimIndex >= 0 && dimIndex < _dimensionContainers.Length)
            {
                _dimensionContainers[dimIndex] = CreateQuestionStepContainer();
                return BuildCardStep(_dimensionContainers[dimIndex]);
            }

            return new VisualElement();
        }

        private ExVisualElement CreateQuestionStepContainer()
        {
            var container = new ExVisualElement();
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

        private VisualElement BuildWelcomeStep()
        {
            var root = new ExVisualElement();
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));
            return root;
        }

        private void PopulateAllDimensionCheckboxes()
        {
            if (_viewModel == null) return;
            _checkboxItems.Clear();

            for (int i = 0; i < DimensionGoalsCatalog.Dimensions.Length; i++)
            {
                var dimDef = DimensionGoalsCatalog.Dimensions[i];
                var container = _dimensionContainers[i];
                if (container == null) continue;

                container.Clear();

                foreach (var topic in dimDef.Topics)
                {
                    string topicCode = topic.Code;
                    string localizedName = _viewModel.GetTopicDisplayName(topicCode);

                    var checkboxItem = new FormFieldItemCheckbox
                    {
                        Text = localizedName,
                        CheckboxValue = _viewModel.IsTopicSelected(topicCode) ? CheckboxState.Checked : CheckboxState.Unchecked
                    };

                    var cb = checkboxItem.Q<Checkbox>();
                    if (cb != null)
                    {
                        cb.RegisterValueChangedCallback(evt =>
                        {
                            _viewModel.SetTopicSelected(topicCode, evt.newValue == CheckboxState.Checked);
                        });
                    }

                    _checkboxItems.Add(checkboxItem);
                    container.Add(checkboxItem);
                }
            }
        }

        private void UpdateCheckboxLabelsFromBackend()
        {
            if (_viewModel == null) return;

            int itemIdx = 0;
            for (int i = 0; i < DimensionGoalsCatalog.Dimensions.Length; i++)
            {
                var dimDef = DimensionGoalsCatalog.Dimensions[i];
                foreach (var topic in dimDef.Topics)
                {
                    if (itemIdx < _checkboxItems.Count)
                    {
                        _checkboxItems[itemIdx].Text = _viewModel.GetTopicDisplayName(topic.Code);
                    }
                    itemIdx++;
                }
            }
        }

        private void SyncCheckboxesFromViewModel()
        {
            if (_viewModel == null) return;

            int itemIdx = 0;
            for (int i = 0; i < DimensionGoalsCatalog.Dimensions.Length; i++)
            {
                var dimDef = DimensionGoalsCatalog.Dimensions[i];
                foreach (var topic in dimDef.Topics)
                {
                    if (itemIdx < _checkboxItems.Count)
                    {
                        _checkboxItems[itemIdx].CheckboxValue = _viewModel.IsTopicSelected(topic.Code)
                            ? CheckboxState.Checked
                            : CheckboxState.Unchecked;
                    }
                    itemIdx++;
                }
            }
        }

        protected override void SetupCompanionSlot(VisualElement slot)
        {
            var container = new VisualElement();
            slot.Add(container);

            var title = new Unity.AppUI.UI.Heading { text = "@UI:GOALS_TITLE" };
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

            string message = stepIndex switch
            {
                0 => "@UI:GOALS_WELCOME_NUTRI",
                1 => "@UI:GOALS_Q_DIM_1",
                2 => "@UI:GOALS_Q_DIM_2",
                3 => "@UI:GOALS_Q_DIM_3",
                4 => "@UI:GOALS_Q_DIM_4",
                5 => "@UI:GOALS_Q_DIM_5",
                6 => "@UI:GOALS_Q_DIM_6",
                _ => ""
            };

            UpdateMascotMessage(message);
        }

        private string GetLocalized(string key, string fallback)
        {
            try
            {
                string localized = LocalizationSettings.StringDatabase?.GetLocalizedString("UI", key);
                return !string.IsNullOrEmpty(localized) ? localized : fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
