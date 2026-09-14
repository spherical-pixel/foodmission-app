using System;
using System.ComponentModel;
using eu.foodmission.platform.Components;
using MainraGames;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
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
        private UnityEngine.UIElements.Button _btnTypeBreakfast;
        private UnityEngine.UIElements.Button _btnTypeLunch;
        private UnityEngine.UIElements.Button _btnTypeDinner;
        private UnityEngine.UIElements.Button _btnTypeSnack;

        private VisualElement _questionsContainer;
        private Unity.AppUI.UI.TextField _inputMealName;
        private Unity.AppUI.UI.Text _feedbackLabel;
        private FMButton _btnSubmitQuickMeal;

        private IAudioService _audioService;

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
            _btnTypeBreakfast = contentContainer.Q<UnityEngine.UIElements.Button>("btn-type-breakfast");
            _btnTypeLunch = contentContainer.Q<UnityEngine.UIElements.Button>("btn-type-lunch");
            _btnTypeDinner = contentContainer.Q<UnityEngine.UIElements.Button>("btn-type-dinner");
            _btnTypeSnack = contentContainer.Q<UnityEngine.UIElements.Button>("btn-type-snack");

            _questionsContainer = contentContainer.Q<VisualElement>("questions-container");
            _inputMealName = contentContainer.Q<Unity.AppUI.UI.TextField>("input-meal-name");
            _feedbackLabel = contentContainer.Q<Unity.AppUI.UI.Text>("feedback-label");
            _btnSubmitQuickMeal = contentContainer.Q<FMButton>("btn-submit-quick-meal");

            if (_btnTypeBreakfast != null)
            {
                _btnTypeBreakfast.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.SetMealType("BREAKFAST");
                };
            }

            if (_btnTypeLunch != null)
            {
                _btnTypeLunch.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.SetMealType("LUNCH");
                };
            }

            if (_btnTypeDinner != null)
            {
                _btnTypeDinner.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.SetMealType("DINNER");
                };
            }

            if (_btnTypeSnack != null)
            {
                _btnTypeSnack.clicked += () =>
                {
                    _audioService?.PlaySfx(SfxType.PositiveButton);
                    _viewModel?.SetMealType("SNACK");
                };
            }

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
                    }
                };
            }
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
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Questions) ||
                e.PropertyName == nameof(_viewModel.ActiveQuestTitle))
            {
                UpdateView();
            }
            else if (e.PropertyName == nameof(_viewModel.MealTypeLabels))
            {
                UpdateMealTypeLabels();
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

            UpdateMealTypeLabels();
            UpdateMealTypeSelection();
            RebuildQuestions();
            UpdateSubmitState();
        }

        private void UpdateMealTypeLabels()
        {
            if (_viewModel?.MealTypeLabels == null || _viewModel.MealTypeLabels.Count == 0) return;

            if (_btnTypeBreakfast != null && _viewModel.MealTypeLabels.TryGetValue("BREAKFAST", out var breakfastLabel))
            {
                _btnTypeBreakfast.text = breakfastLabel;
            }
            if (_btnTypeLunch != null && _viewModel.MealTypeLabels.TryGetValue("LUNCH", out var lunchLabel))
            {
                _btnTypeLunch.text = lunchLabel;
            }
            if (_btnTypeDinner != null && _viewModel.MealTypeLabels.TryGetValue("DINNER", out var dinnerLabel))
            {
                _btnTypeDinner.text = dinnerLabel;
            }
            if (_btnTypeSnack != null && _viewModel.MealTypeLabels.TryGetValue("SNACK", out var snackLabel))
            {
                _btnTypeSnack.text = snackLabel;
            }
        }

        private void UpdateMealTypeSelection()
        {
            if (_viewModel == null) return;
            string selected = _viewModel.SelectedMealType ?? "LUNCH";

            SetBtnActive(_btnTypeBreakfast, selected == "BREAKFAST");
            SetBtnActive(_btnTypeLunch, selected == "LUNCH");
            SetBtnActive(_btnTypeDinner, selected == "DINNER");
            SetBtnActive(_btnTypeSnack, selected == "SNACK");
        }

        private void SetBtnActive(UnityEngine.UIElements.Button btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList("fm-quick-meal-type-btn--active");
            else btn.RemoveFromClassList("fm-quick-meal-type-btn--active");
        }

        private void RebuildQuestions()
        {
            if (_questionsContainer == null || _viewModel?.Questions == null) return;
            _questionsContainer.Clear();

            foreach (var q in _viewModel.Questions)
            {
                if (q == null) continue;

                var card = new VisualElement();
                card.AddToClassList("fm-quick-meal-question-card");
                if (q.IsChecked) card.AddToClassList("fm-quick-meal-question-card--checked");

                bool isSwapSelector = q.QuestionType == DirectQuestionType.SwapSelector;
                string capturedId = q.Id;

                var header = new VisualElement();
                header.AddToClassList("fm-quick-meal-question-header");

                if (!isSwapSelector)
                {
                    var checkbox = new VisualElement();
                    checkbox.AddToClassList("fm-quick-meal-checkbox");
                    if (q.IsChecked)
                    {
                        checkbox.AddToClassList("fm-quick-meal-checkbox--checked");
                        var checkmark = new Label("✓");
                        checkmark.AddToClassList("fm-quick-meal-checkbox-mark");
                        checkbox.Add(checkmark);
                    }
                    header.Add(checkbox);

                    header.RegisterCallback<ClickEvent>(_ =>
                    {
                        _audioService?.PlaySfx(SfxType.PositiveButton);
                        _viewModel?.ToggleQuestion(capturedId);
                    });
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
                    swapsTitle.text = "Selecciona la sustitución realizada:";
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

                        var radio = new VisualElement();
                        radio.AddToClassList("fm-quick-meal-swap-chip-radio");
                        if (isSelected) radio.AddToClassList("fm-quick-meal-swap-chip-radio--active");
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

                _questionsContainer.Add(card);
            }
        }

        private void UpdateSubmitState()
        {
            if (_viewModel == null) return;

            if (_feedbackLabel != null)
            {
                if (_viewModel.SubmitSuccess)
                {
                    _feedbackLabel.style.display = DisplayStyle.Flex;
                    _feedbackLabel.text = _viewModel.SuccessMessage ?? "¡Registrado con éxito!";
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
