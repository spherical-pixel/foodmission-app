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

        private VisualElement _typesContainer;
        private readonly List<(string Code, FMButton Button)> _typeButtons = new();

        private VisualElement _questionsContainer;
        private FMMealFactsPicker _factsPicker;
        private FormFieldItemCheckbox _onlyMissionsToggle;
        private Unity.AppUI.UI.TextField _inputMealName;
        private VisualElement _editModeBanner;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            //AddDevDayOffsetButton();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Development-only: shifts the meal-log date (+0…+6 days) to test day-based missions. Not localized on purpose.</summary>
        private void AddDevDayOffsetButton()
        {
            if (_onlyMissionsToggle?.parent == null)
            {
                return;
            }

            var button = new FMButton { variant = ButtonVariant.Default, size = Size.S, quiet = true };
            button.AddToClassList("fm-quick-meal-dev-offset");
            void Refresh() => button.title = $"DEV · meal date +{DevMealDayOffset.Days}d";
            Refresh();
            button.clicked += () =>
            {
                DevMealDayOffset.Cycle();
                Refresh();
            };
            VisualElement parent = _onlyMissionsToggle.parent;
            parent.Insert(parent.IndexOf(_onlyMissionsToggle), button);
        }
#endif

        private void CacheUIElements()
        {
            _typesContainer = contentContainer.Q<VisualElement>("types-row") ?? contentContainer.Q<VisualElement>(className: "fm-quick-meal-types-row");

            _questionsContainer = contentContainer.Q<VisualElement>("questions-container");
            if (_questionsContainer != null)
            {
                _factsPicker = new FMMealFactsPicker();
                _questionsContainer.Add(_factsPicker);
            }
            _onlyMissionsToggle = contentContainer.Q<FormFieldItemCheckbox>("only-missions-toggle");
            _inputMealName = contentContainer.Q<Unity.AppUI.UI.TextField>("input-meal-name");
            _editModeBanner = contentContainer.Q<VisualElement>("edit-mode-banner");
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
                        if (_viewModel.IsEditing)
                        {
                            ScheduleNavigateBack(1000);
                        }
                        else
                        {
                            ScheduleNavigateToHome(1000);
                        }
                    }
                };
            }
        }

        private void ScheduleNavigateBack(long delayMs = 1000)
        {
            _navigationScheduleItem?.Pause();
            _navigationScheduleItem = schedule.Execute(() =>
            {
                if (panel == null) return;
                _navController?.PopBackStack();
            }).StartingIn(delayMs);
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

            string mealLogId = null;
            bool isEdit = false;

            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a.name == "mealLogId") mealLogId = a.value;
                    else if (a.name == "mode" && a.value == "edit") isEdit = true;
                }
            }

            if (isEdit && !string.IsNullOrEmpty(mealLogId))
            {
                if (MealLogViewModel.PendingQuickMealEditPayload != null &&
                    MealLogViewModel.PendingQuickMealEditPayload.id == mealLogId)
                {
                    var payload = MealLogViewModel.PendingQuickMealEditPayload;
                    MealLogViewModel.PendingQuickMealEditPayload = null;
                    _ = _viewModel?.LoadForEditAsync(payload);
                }
                else
                {
                    _ = _viewModel?.LoadForEditByIdAsync(mealLogId);
                }
            }
            else
            {
                _viewModel?.ResetEditState();
                _ = _viewModel?.LoadActiveQuestQuestionsAsync();
            }

            UpdateView();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            TrackLoadingOverlay(() => _viewModel.IsLoading || _viewModel.IsSubmitting, nameof(QuickMealLogViewModel.IsLoading), nameof(QuickMealLogViewModel.IsSubmitting));
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            _onlyMissionsToggle?.RegisterCallback<ChangeEvent<CheckboxState>>(OnOnlyMissionsChanged);
            if (_factsPicker != null)
            {
                _factsPicker.SectionToggled += OnSectionToggled;
                _factsPicker.ItemToggled += OnItemToggled;
                _factsPicker.SwapSelected += OnSwapSelected;
            }
            UpdateView();
        }

        private void OnSectionToggled(string id)
        {
            _viewModel?.ToggleSection(id);
        }

        private void OnItemToggled(string id)
        {
            _viewModel?.ToggleQuestion(id);
        }

        private void OnSwapSelected(string id, string option)
        {
            _viewModel?.SelectSwapForQuestion(id, option);
        }

        private void OnOnlyMissionsChanged(ChangeEvent<CheckboxState> evt)
        {
            _viewModel?.SetOnlyMissionItems(evt.newValue == CheckboxState.Checked);
        }

        protected override void OnViewModelUnbinding()
        {
            _navigationScheduleItem?.Pause();
            _navigationScheduleItem = null;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            _onlyMissionsToggle?.UnregisterCallback<ChangeEvent<CheckboxState>>(OnOnlyMissionsChanged);
            if (_factsPicker != null)
            {
                _factsPicker.SectionToggled -= OnSectionToggled;
                _factsPicker.ItemToggled -= OnItemToggled;
                _factsPicker.SwapSelected -= OnSwapSelected;
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
            else if (e.PropertyName == nameof(_viewModel.IsEditing))
            {
                UpdateEditState();
            }
            else if (e.PropertyName == nameof(_viewModel.ErrorDetail))
            {
                if (_viewModel.ErrorDetail != null)
                {
                    _audioService?.PlaySfx(SfxType.NegativeButton);
                    string title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE");
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

            _onlyMissionsToggle?.EnableInClassList("fm-quick-meal-only-missions--hidden", !_viewModel.HasMissionItems);


            UpdateEditState();
            RebuildTypeButtons();
            RebuildQuestions();
            UpdateSubmitState();
        }

        private void UpdateEditState()
        {
            if (_viewModel == null) return;
            bool isEditing = _viewModel.IsEditing;
            if (_editModeBanner != null)
            {
                _editModeBanner.style.display = isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_btnSubmitQuickMeal != null)
            {
                _btnSubmitQuickMeal.title = isEditing
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUICK_MEAL_LOG_UPDATE_SUBMIT")
                    : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SAVE");
            }
            if (_inputMealName != null && isEditing && !string.IsNullOrEmpty(_viewModel.MealName))
            {
                _inputMealName.value = _viewModel.MealName;
            }
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
            if (_factsPicker == null || _viewModel == null)
            {
                return;
            }
            _factsPicker.SetContent(_viewModel.Sections, _viewModel.Questions);
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
