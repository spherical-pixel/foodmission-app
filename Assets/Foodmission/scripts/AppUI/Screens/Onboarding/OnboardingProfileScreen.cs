using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Core;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class OnboardingProfileScreen : StepFlowScreenBase<OnboardingProfileViewModel>
    {
        protected override int StepCount => 6;

        protected override string NextButtonLabel => "@UI:TXT_NEXT";
        protected override string PreviousButtonLabel => "@UI:TXT_BACK";
        protected override string CompleteButtonLabel => "@UI:SAVE";

        private ExVisualElement _messageCard;
        private Unity.AppUI.UI.Text _messageText;
        private FMNutriView _nutriView;
        //private FMButton _skipHeaderButton;

        // Form dropdowns per step
        private FormFieldItemDropDownField _motivationDropdown;
        private FormFieldItemDropDownField _genderDropdown;
        private FormFieldItemDropDownField _educationLevelDropdown;
        private FormFieldItemDropDownField _annualIncomeDropdown;
        private FormFieldItemDropDownField _dietaryPreferencesDropdown;
        private FormFieldItemDropDownField _activityLevelDropdown;
        private FormFieldItemDropDownField _shoppingResponsibilityDropdown;
        private FormFieldItemDropDownField _dailyTimeCommitmentDropdown;
        private FormFieldItemDropDownField _segmentDropdown;

        private ExVisualElement _step1Container;
        private ExVisualElement _step2Container;
        private ExVisualElement _step3Container;
        private ExVisualElement _step4Container;
        private ExVisualElement _step5Container;

        protected override bool IsFixedContent => true;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public OnboardingProfileScreen() : base()
        {
            CreateFormFields();
        }

        private void CreateFormFields()
        {
            _motivationDropdown = new FormFieldItemDropDownField
            {
                name = "motivation-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_MOTIVATION",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_MOTIVATION"
            };

            _genderDropdown = new FormFieldItemDropDownField
            {
                name = "gender-dropdown",
                HeadingText = "@UI:SELECT_GENDER",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_GENDER"
            };

            _educationLevelDropdown = new FormFieldItemDropDownField
            {
                name = "education-level-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_EDUCATION",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_EDUCATION"
            };

            _annualIncomeDropdown = new FormFieldItemDropDownField
            {
                name = "annual-income-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_INCOME",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_INCOME"
            };

            _dietaryPreferencesDropdown = new FormFieldItemDropDownField
            {
                name = "dietary-preferences-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_DIET",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_DIET",
                DropdownSelectionType = PickerSelectionType.Multiple,
                DropdownCloseOnSelect = false
            };

            _activityLevelDropdown = new FormFieldItemDropDownField
            {
                name = "activity-level-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_ACTIVITY",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_ACTIVITY"
            };

            _shoppingResponsibilityDropdown = new FormFieldItemDropDownField
            {
                name = "shopping-responsibility-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_SHOPPING",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_SHOPPING"
            };

            _dailyTimeCommitmentDropdown = new FormFieldItemDropDownField
            {
                name = "daily-time-commitment-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_DAILY_TIME",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_DAILY_TIME"
            };

            _segmentDropdown = new FormFieldItemDropDownField
            {
                name = "segment-dropdown",
                HeadingText = "@UI:ONBOARDING_PROFILE.LABEL_SEGMENT",
                DropdownDefaultMessage = "@UI:ONBOARDING_PROFILE.PLACEHOLDER_SEGMENT"
            };
        }

        protected override VisualElement CreateStepContent(int stepIndex)
        {
            return stepIndex switch
            {
                0 => BuildStep0Welcome(),
                1 => BuildStepCard(_step1Container = CreateStepContainer(_motivationDropdown)),
                2 => BuildStepCard(_step2Container = CreateStepContainer(_genderDropdown, _educationLevelDropdown, _annualIncomeDropdown)),
                3 => BuildStepCard(_step3Container = CreateStepContainer(_dietaryPreferencesDropdown)),
                4 => BuildStepCard(_step4Container = CreateStepContainer(_activityLevelDropdown, _shoppingResponsibilityDropdown, _dailyTimeCommitmentDropdown)),
                5 => BuildStepCard(_step5Container = CreateStepContainer(_segmentDropdown)),
                _ => new VisualElement()
            };
        }

        private ExVisualElement CreateStepContainer(params VisualElement[] children)
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

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null) container.Add(child);
                }
            }
            return container;
        }

        private VisualElement BuildStepCard(VisualElement element)
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

        private VisualElement BuildStep0Welcome()
        {
            var root = new ExVisualElement();
            root.style.paddingTop = 24;
            root.style.paddingBottom = 24;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.width = new StyleLength(Length.Percent(100));
            root.style.alignItems = Align.Center;

            var welcomeBox = new ExVisualElement();
            welcomeBox.AddToClassList("box-background");
            welcomeBox.AddToClassList("fm-shadow-wrapper");
            welcomeBox.style.width = new StyleLength(Length.Percent(100));
            welcomeBox.style.paddingTop = 24;
            welcomeBox.style.paddingBottom = 24;
            welcomeBox.style.paddingLeft = 20;
            welcomeBox.style.paddingRight = 20;
            welcomeBox.style.alignItems = Align.Center;


            var desc = new Unity.AppUI.UI.Text
            {
                text = "@UI:ONBOARDING_PROFILE.WELCOME_DESC"
            };
            desc.style.marginTop = 12;
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.primary = false;
            desc.AddToClassList("centered-text");
            welcomeBox.Add(desc);

            root.Add(welcomeBox);
            return root;
        }


        protected override void SetupCompanionSlot(VisualElement slot)
        {
            _nutriView = new FMNutriView();
            _nutriView.AddToClassList("fm-step-flow__guide-nutri");
            slot.Add(_nutriView);

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

            string message = "@UI:ONBOARDING_PROFILE.NUTRI_STEP_" + stepIndex;
            UpdateMascotMessage(message);

        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null)
            {
                await _viewModel.LoadCatalogDataAsync();
                _viewModel.PrePopulateFromState();
                PopulateAllDropdowns();
                PrePopulateSelections();
                RegisterDropdownEvents();
            }
        }

        private void PopulateAllDropdowns()
        {
            if (_viewModel == null) return;

            ConfigureDropdown(_motivationDropdown, _viewModel.MotivationOptions);
            ConfigureDropdown(_genderDropdown, _viewModel.GenderOptions);
            ConfigureDropdown(_educationLevelDropdown, _viewModel.EducationLevelOptions);
            ConfigureDropdown(_annualIncomeDropdown, _viewModel.AnnualIncomeOptions);
            ConfigureDropdown(_dietaryPreferencesDropdown, _viewModel.DietaryPreferenceOptions);
            ConfigureDropdown(_activityLevelDropdown, _viewModel.ActivityLevelOptions);
            ConfigureDropdown(_shoppingResponsibilityDropdown, _viewModel.ShoppingResponsibilityOptions);
            ConfigureDropdown(_dailyTimeCommitmentDropdown, _viewModel.DailyTimeCommitmentOptions);
            ConfigureDropdown(_segmentDropdown, _viewModel.SegmentOptions);
        }

        private void ConfigureDropdown(FormFieldItemDropDownField dropdown, IList<string> options)
        {
            if (dropdown == null || options == null || options.Count == 0) return;

            dropdown.Dropdown.sourceItems = (System.Collections.IList)options;
            dropdown.Dropdown.bindItem = (item, index) =>
            {
                item.label = options[index];
                item.icon = null;
            };
        }

        private void PrePopulateSelections()
        {
            if (_viewModel == null) return;

            SetDropdownSelection(_motivationDropdown, _viewModel.SelectedMotivationIndex);
            SetDropdownSelection(_genderDropdown, _viewModel.SelectedGenderIndex);
            SetDropdownSelection(_educationLevelDropdown, _viewModel.SelectedEducationLevelIndex);
            SetDropdownSelection(_annualIncomeDropdown, _viewModel.SelectedAnnualIncomeIndex);
            SetDropdownSelection(_activityLevelDropdown, _viewModel.SelectedActivityLevelIndex);
            SetDropdownSelection(_shoppingResponsibilityDropdown, _viewModel.SelectedShoppingResponsibilityIndex);
            SetDropdownSelection(_dailyTimeCommitmentDropdown, _viewModel.SelectedDailyTimeCommitmentIndex);
            SetDropdownSelection(_segmentDropdown, _viewModel.SelectedSegmentIndex);
            SetDropdownSelectionMulti(_dietaryPreferencesDropdown, _viewModel.SelectedDietaryPreferenceIndices);
        }

        private static void SetDropdownSelection(FormFieldItemDropDownField dropdown, int index)
        {
            if (dropdown == null || index < 0) return;
            dropdown.Dropdown.value = new[] { index };
        }

        private static void SetDropdownSelectionMulti(FormFieldItemDropDownField dropdown, int[] indices)
        {
            if (dropdown == null || indices == null || indices.Length == 0) return;
            dropdown.Dropdown.SetValueWithoutNotify(indices);
        }

        private void RegisterDropdownEvents()
        {
            _motivationDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedMotivationIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _genderDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedGenderIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _educationLevelDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedEducationLevelIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _annualIncomeDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedAnnualIncomeIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _dietaryPreferencesDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                _viewModel.SelectedDietaryPreferenceIndices = evt.newValue?.ToArray() ?? new int[0];
            });

            _activityLevelDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedActivityLevelIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _shoppingResponsibilityDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedShoppingResponsibilityIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _dailyTimeCommitmentDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedDailyTimeCommitmentIndex = val != null && val.Length > 0 ? val[0] : -1;
            });

            _segmentDropdown?.Dropdown.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue?.ToArray();
                _viewModel.SelectedSegmentIndex = val != null && val.Length > 0 ? val[0] : -1;
            });
        }

        private void OnSkipClicked()
        {
            if (_viewModel != null)
            {
                _viewModel.Skip();
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.ShowErrorRequest += OnShowErrorRequested;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.ShowErrorRequest -= OnShowErrorRequested;
            }
            base.OnViewModelUnbinding();
        }

        private void OnShowErrorRequested(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Toast.Build(this, message, NotificationDuration.Long)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }
    }
}