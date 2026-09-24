using System;
using System.Collections.Generic;
using System.ComponentModel;
using eu.foodmission.platform.Components;
using MainraGames;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class QuickMealLogScreen : NavigationScreenBase<QuickMealLogViewModel>
    {
        protected override bool ApplySafeAreaBottom => true;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

        private Unity.AppUI.UI.Text _activeQuestHint;
        private VisualElement _typesContainer;
        private readonly List<(string Code, FMButton Button)> _typeButtons = new();

        private VisualElement _questionsContainer;
        private Unity.AppUI.UI.TextField _inputMealName;
        private Unity.AppUI.UI.Text _feedbackLabel;
        private FMButton _btnSubmitQuickMeal;

        private IAudioService _audioService;
        private IVisualElementScheduledItem _navigationScheduleItem;

        public QuickMealLogScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.QuickMealLogScreen));

            _audioService = App.current?.services?.GetService<IAudioService>();

            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _activeQuestHint = contentContainer.Q<Unity.AppUI.UI.Text>("active-quest-hint");
            _typesContainer = contentContainer.Q<VisualElement>("types-row") ?? contentContainer.Q<VisualElement>(className: "fm-quick-meal-types-row");

            _questionsContainer = contentContainer.Q<VisualElement>("questions-container");
            _inputMealName = contentContainer.Q<Unity.AppUI.UI.TextField>("input-meal-name");
            _feedbackLabel = contentContainer.Q<Unity.AppUI.UI.Text>("feedback-label");
            _btnSubmitQuickMeal = contentContainer.Q<FMButton>("btn-submit-quick-meal");

            if (_inputMealName != null)
            {
                _inputMealName.RegisterValueChangedCallback(evt =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.MealName = evt.newValue;
                    }
                });
            }

            if (_btnSubmitQuickMeal != null)
            {
                _btnSubmitQuickMeal.clicked += async () =>
                {
                    if (_viewModel == null) return;
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    bool success = await _viewModel.SubmitQuickMealLogAsync();
                    if (success)
                    {
                        _audioService?.PlaySfx(SfxType.ProgressPath);
                        ScheduleNavigateToHome(1000);
                    }
                };
            }
        }

        private void ScheduleNavigateToHome(long delayMs = 1500)
        {
            _navigationScheduleItem?.Pause();
            _navigationScheduleItem = schedule.Execute(() =>
            {
                if (panel == null) return;
                if (_viewModel != null)
                {
                    _viewModel.NavigateToHome();
                }
                else
                {
                    OnNavigationRequested(Actions.go_to_home, null);
                }
            }).StartingIn(delayMs);
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            _ = _viewModel?.LoadActiveQuestQuestionsAsync();
            UpdateView();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            UpdateView();
        }

        protected override void OnViewModelUnbinding()
        {
            _navigationScheduleItem?.Pause();
            _navigationScheduleItem = null;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Questions) ||
                e.PropertyName == nameof(_viewModel.Sections) ||
                e.PropertyName == nameof(_viewModel.ActiveQuestTitle))
            {
                UpdateView();
            }
            else if (e.PropertyName == nameof(_viewModel.TypeOfMealOptions) ||
                     e.PropertyName == nameof(_viewModel.MealTypeLabels))
            {
                RebuildTypeButtons();
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedMealType))
            {
                UpdateMealTypeSelection();
            }
            else if (e.PropertyName == nameof(_viewModel.SubmitSuccess) ||
                     e.PropertyName == nameof(_viewModel.SuccessMessage) ||
                     e.PropertyName == nameof(_viewModel.IsSubmitting))
            {
                UpdateSubmitState();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorMessage))
            {
                if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
                {
                    ShowErrorToast(_viewModel.ErrorMessage);
                    _viewModel.ErrorMessage = "";
                }
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                if (_viewModel.ErrorDetail != null)
                {
                    _audioService?.PlaySfx(SfxType.NegativeButton);
                    string title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE") ?? "Error";
                    FMDialog.ShowApiError(this, title, _viewModel.ErrorDetail);
                    _viewModel.ErrorDetail = null;
                }
            }
        }

        private void ShowErrorToast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            string localized = message;
            if (message.StartsWith("@UI:"))
            {
                string key = message.Substring(4);
                localized = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key) ?? message;
            }

            _audioService?.PlaySfx(SfxType.NegativeButton);
            Toast.Build(this, localized, NotificationDuration.Short)
                .SetStyle(NotificationStyle.Negative)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }

        private void UpdateView()
        {
            if (_viewModel == null) return;

            if (_activeQuestHint != null)
            {
                if (!string.IsNullOrEmpty(_viewModel.ActiveQuestTitle))
                {
                    _activeQuestHint.text = $"Vinculado a tu misión activa: {_viewModel.ActiveQuestTitle}";
                }
                else
                {
                    _activeQuestHint.text = "Registra los aspectos saludables y sostenibles de tu comida.";
                }
            }

            RebuildTypeButtons();
            RebuildQuestions();
            UpdateSubmitState();
        }

        private void RebuildTypeButtons()
        {
            if (_typesContainer == null) return;
            _typesContainer.Clear();
            _typeButtons.Clear();

            if (_viewModel?.TypeOfMealOptions != null && _viewModel.TypeOfMealOptions.Length > 0)
            {
                foreach (var item in _viewModel.TypeOfMealOptions)
                {
                    if (item == null || string.IsNullOrEmpty(item.code)) continue;

                    string emoji = MealLogHelpers.GetEmojiForTypeOfMeal(item.code);
                    string label = string.IsNullOrEmpty(item.label) ? item.code : item.label;
                    string code = item.code;

                    var btn = new FMButton();
                    btn.AddToClassList("fm-quick-meal-type-btn");
                    btn.title = $"{emoji} {label}";
                    btn.size = Size.L;
                    btn.variant = ButtonVariant.Default;
                    btn.clicked += () =>
                    {
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        _viewModel?.SetMealType(code);
                    };

                    _typesContainer.Add(btn);
                    _typeButtons.Add((code, btn));
                }
            }
            else
            {
                var defaults = new[]
                {
                    ("BREAKFAST", "🌅 Desayuno"),
                    ("LUNCH", "☀️ Almuerzo"),
                    ("DINNER", "🌙 Cena"),
                    ("SNACK", "🍿 Snack")
                };

                foreach (var (code, defaultLabel) in defaults)
                {
                    string label = defaultLabel;
                    if (_viewModel?.MealTypeLabels != null && _viewModel.MealTypeLabels.TryGetValue(code, out var customLabel))
                    {
                        label = customLabel;
                    }

                    var btn = new FMButton();
                    btn.AddToClassList("fm-quick-meal-type-btn");
                    btn.title = label;
                    btn.size = Size.L;
                    btn.variant = ButtonVariant.Default;
                    string capturedCode = code;
                    btn.clicked += () =>
                    {
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        _viewModel?.SetMealType(capturedCode);
                    };

                    _typesContainer.Add(btn);
                    _typeButtons.Add((capturedCode, btn));
                }
            }

            UpdateMealTypeSelection();
        }

        private void UpdateMealTypeSelection()
        {
            if (_viewModel == null) return;
            string selected = _viewModel.SelectedMealType ?? "LUNCH";

            foreach (var (code, btn) in _typeButtons)
            {
                SetBtnActive(btn, string.Equals(code, selected, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void SetBtnActive(FMButton btn, bool active)
        {
            if (btn == null) return;
            btn.variant = active ? ButtonVariant.Accent : ButtonVariant.Default;
            btn.EnableInClassList("fm-quick-meal-type-btn--active", active);
        }

        private void RebuildQuestions()
        {
            if (_questionsContainer == null || _viewModel == null) return;
            _questionsContainer.Clear();

            if (_viewModel.Sections != null && _viewModel.Sections.Count > 0)
            {
                foreach (var sec in _viewModel.Sections)
                {
                    if (sec == null) continue;

                    var sectionBox = new VisualElement();
                    sectionBox.AddToClassList("fm-quick-meal-accordion-section");

                    var accordionHeader = new VisualElement();
                    accordionHeader.AddToClassList("fm-quick-meal-accordion-header");
                    if (sec.IsExpanded)
                    {
                        accordionHeader.AddToClassList("fm-quick-meal-accordion-header--expanded");
                    }

                    var headerLeft = new VisualElement();
                    headerLeft.AddToClassList("fm-quick-meal-accordion-header-left");

                    if (!string.IsNullOrEmpty(sec.Icon))
                    {
                        var iconText = new Unity.AppUI.UI.Text();
                        iconText.text = sec.Icon;
                        iconText.AddToClassList("fm-quick-meal-accordion-icon");
                        headerLeft.Add(iconText);
                    }

                    var titleText = new Unity.AppUI.UI.Text();
                    titleText.AddToClassList("fm-quick-meal-accordion-title");
                    titleText.text = sec.Title ?? "";
                    titleText.size = TextSize.M;
                    headerLeft.Add(titleText);
                    accordionHeader.Add(headerLeft);

                    var headerRight = new VisualElement();
                    headerRight.AddToClassList("fm-quick-meal-accordion-header-right");

                    int selectedCount = sec.SelectedCount;
                    if (selectedCount > 0)
                    {
                        var badge = new Unity.AppUI.UI.Text();
                        badge.AddToClassList("fm-quick-meal-accordion-badge");
                        badge.text = $"{selectedCount}";
                        headerRight.Add(badge);
                    }

                    var chevron = new Icon();
                    chevron.AddToClassList("fm-quick-meal-accordion-chevron");
                    chevron.iconName = "caret-down";
                    if (!sec.IsExpanded)
                    {
                        chevron.AddToClassList("fm-quick-meal-accordion-chevron--collapsed");
                    }
                    headerRight.Add(chevron);
                    accordionHeader.Add(headerRight);

                    string capturedSecId = sec.Id;
                    accordionHeader.RegisterCallback<ClickEvent>(_ =>
                    {
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        _viewModel?.ToggleSection(capturedSecId);
                    });

                    sectionBox.Add(accordionHeader);

                    var contentBox = new VisualElement();
                    contentBox.AddToClassList("fm-quick-meal-accordion-content");
                    if (!sec.IsExpanded)
                    {
                        contentBox.AddToClassList("fm-quick-meal-accordion-content--hidden");
                    }

                    if (sec.Items != null)
                    {
                        foreach (var q in sec.Items)
                        {
                            contentBox.Add(BuildQuestionCard(q));
                        }
                    }

                    sectionBox.Add(contentBox);
                    _questionsContainer.Add(sectionBox);
                }
            }
            else if (_viewModel.Questions != null)
            {
                foreach (var q in _viewModel.Questions)
                {
                    _questionsContainer.Add(BuildQuestionCard(q));
                }
            }
        }

        private VisualElement BuildQuestionCard(QuickMealCheckItem q)
        {
            if (q == null) return new VisualElement();

            var card = new VisualElement();
            card.AddToClassList("fm-quick-meal-question-card");
            if (q.IsChecked) card.AddToClassList("fm-quick-meal-question-card--checked");

            bool isSwapSelector = q.QuestionType == DirectQuestionType.SwapSelector;
            string capturedId = q.Id;

            var header = new VisualElement();
            header.AddToClassList("fm-quick-meal-question-header");

            if (!isSwapSelector)
            {
                var checkbox = new Unity.AppUI.UI.Checkbox
                {
                    value = q.IsChecked ? CheckboxState.Checked : CheckboxState.Unchecked
                };
                checkbox.AddToClassList("fm-quick-meal-checkbox");
                checkbox.RegisterValueChangedCallback(_ =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.ToggleQuestion(capturedId);
                });
                header.Add(checkbox);

                header.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Unity.AppUI.UI.Checkbox ||
                        (evt.target is VisualElement ve && ve.GetFirstAncestorOfType<Unity.AppUI.UI.Checkbox>() != null))
                    {
                        return;
                    }

                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.ToggleQuestion(capturedId);
                });
            }

            if (!string.IsNullOrEmpty(q.Icon))
            {
                var iconLabel = new Unity.AppUI.UI.Text();
                iconLabel.text = q.Icon;
                iconLabel.AddToClassList("fm-quick-meal-question-icon");
                header.Add(iconLabel);
            }

            var promptLabel = new Unity.AppUI.UI.Text();
            promptLabel.text = q.Prompt ?? "";
            promptLabel.AddToClassList("fm-quick-meal-question-text");
            header.Add(promptLabel);

            card.Add(header);

            // Render inline swap options directly if question is a SwapSelector
            if (isSwapSelector && q.SwapOptions != null && q.SwapOptions.Length > 0)
            {
                var swapsContainer = new VisualElement();
                swapsContainer.AddToClassList("fm-quick-meal-swaps-container");

                var swapsTitle = new Unity.AppUI.UI.Text();
                swapsTitle.text = "@UI:QUICK_MEAL_SWAPS_SUBTITLE";
                swapsTitle.AddToClassList("fm-quick-meal-swaps-title");
                swapsContainer.Add(swapsTitle);

                var swapsList = new VisualElement();
                swapsList.AddToClassList("fm-quick-meal-swaps-list");

                foreach (var swap in q.SwapOptions)
                {
                    if (string.IsNullOrEmpty(swap)) continue;

                    var chip = new VisualElement();
                    chip.AddToClassList("fm-quick-meal-swap-chip");
                    bool isSelected = q.IsChecked && swap == q.SelectedSwapOption;
                    if (isSelected) chip.AddToClassList("fm-quick-meal-swap-chip--active");

                    var radio = new Unity.AppUI.UI.Radio
                    {
                        value = isSelected,
                        size = Size.M
                    };
                    radio.AddToClassList("fm-quick-meal-swap-radio");
                    chip.Add(radio);

                    var chipLabel = new Unity.AppUI.UI.Text();
                    chipLabel.AddToClassList("fm-quick-meal-swap-chip-text");
                    chipLabel.text = ActivityEventMapper.GetSwapDisplayName(swap);
                    chip.Add(chipLabel);

                    string capturedSwap = swap;
                    chip.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        _viewModel?.SelectSwapForQuestion(capturedId, capturedSwap);
                    });

                    swapsList.Add(chip);
                }

                swapsContainer.Add(swapsList);
                card.Add(swapsContainer);
            }

            return card;
        }

        private void UpdateSubmitState()
        {
            if (_viewModel == null) return;

            if (_feedbackLabel != null)
            {
                if (_viewModel.SubmitSuccess)
                {
                    _feedbackLabel.style.display = DisplayStyle.Flex;
                    _feedbackLabel.text = _viewModel.SuccessMessage;
                }
                else
                {
                    _feedbackLabel.style.display = DisplayStyle.None;
                }
            }

            if (_btnSubmitQuickMeal != null)
            {
                _btnSubmitQuickMeal.SetEnabled(!_viewModel.IsSubmitting && !_viewModel.SubmitSuccess);
            }
        }
    }
}
