using System.Linq;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;
using System;

namespace eu.foodmission.platform
{
    [Preserve]
    class SettingsScreen : NavigationScreenBase<SettingsViewModel>
    {
        private FormFieldItemArrowStepperSettings _themeStepper;
        private FormFieldItemArrowStepperSettings _langStepper;
        private FormFieldItemArrowStepperSettings _scaleStepper;
        private FormFieldItemArrowStepperSettings _fontStepper;
        private FormFieldItemArrowStepperSettings _soundStepper;
        private FormFieldItemArrowStepperSettings _musicStepper;
        private FormFieldItemArrowStepperSettings _notificationsStepper;
        private FormFieldItemArrowStepperSettings _notificationTimeStepper;
        private FormFieldItemArrowStepperSettings _backgroundStepper;
        private FormFieldItemArrowStepperSettings _environmentStepper;
        private FormFieldItemTextField _localUrlFieldComponent;
        private Unity.AppUI.UI.TextField _localUrlInnerField;
        private Unity.AppUI.UI.Text _versionValue;
        private bool _isSettingEnvironment;
        private FMButton _testSurveyButton;

        private VisualElement _developerOptions;
        private AppearanceSettingsBinder _appearanceBinder;


        private static readonly string[] k_SoundChoices = Enumerable.Range(0, 21).Select(i => (i * 5).ToString()).ToArray();
        private static readonly string[] k_NotificationsChoices = { "@UI:OFF", "@UI:ON" };
        private static readonly string[] k_NotificationTimeChoices = Enumerable.Range(0, 24).Select(i => $"{i:D2}:00").ToArray();

        protected override bool IsFixedContent => false;
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;

        public SettingsScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.Settings));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _themeStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-theme");
            _langStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-lang");
            _scaleStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-scale");
            _fontStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-font");
            _soundStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-sound");
            _musicStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-music");
            _notificationsStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-notifications");
            _notificationTimeStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-notification-time");
            _backgroundStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-background");
            _environmentStepper = contentContainer.Q<FormFieldItemArrowStepperSettings>("stepper-environment");
            _appearanceBinder = new AppearanceSettingsBinder(_scaleStepper, _themeStepper, _backgroundStepper, _fontStepper, _langStepper);
            _localUrlFieldComponent = contentContainer.Q<FormFieldItemTextField>("local-url-field");
            _versionValue = contentContainer.Q<Unity.AppUI.UI.Text>("version-value");
            _testSurveyButton = contentContainer.Q<FMButton>("btn-test-survey");

            _developerOptions = contentContainer.Q<VisualElement>("developer-options");

#if UNITY_EDITOR || DEVELOPER_MODE
            _developerOptions.style.display = DisplayStyle.Flex;
#else
            _developerOptions.style.display = DisplayStyle.None;
#endif
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            SetupSteppers();

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            if (_testSurveyButton != null)
            {
                _testSurveyButton.clicked += OnTestSurveyClicked;
            }
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.PushNotifications))
            {
                if (_notificationsStepper != null)
                {
                    _notificationsStepper.SelectedIndex = _viewModel.PushNotifications ? 1 : 0;
                }
                UpdateNotificationTimeVisibility(_viewModel.PushNotifications);
            }
            else if (e.PropertyName == nameof(SettingsViewModel.NotificationPreferredTime))
            {
                if (_notificationTimeStepper != null)
                {
                    _notificationTimeStepper.SelectedIndex = NotificationTimeToIndex(_viewModel.NotificationPreferredTime);
                }
            }
        }

        private void OnTestSurveyClicked()
        {
            _navController?.Navigate(Unity.AppUI.Navigation.Generated.Actions.goto_test_survey);
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _appearanceBinder?.CreateAccessibilityNodes(h);
            _soundStepper?.CreateAccessibilityNode(h, "Sound volume");
            _musicStepper?.CreateAccessibilityNode(h, "Music volume");
            _notificationsStepper?.CreateAccessibilityNode(h, "Notifications");
            _notificationTimeStepper?.CreateAccessibilityNode(h, "Reminder time");
            _environmentStepper?.CreateAccessibilityNode(h, "Environment");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _appearanceBinder?.DestroyAccessibilityNodes();
            _soundStepper?.DestroyAccessibilityNode();
            _musicStepper?.DestroyAccessibilityNode();
            _notificationsStepper?.DestroyAccessibilityNode();
            _notificationTimeStepper?.DestroyAccessibilityNode();
            _environmentStepper?.DestroyAccessibilityNode();

            base.TeardownAccessibilityNodes();
        }

        private void SetupSteppers()
        {
            _appearanceBinder?.Bind(_viewModel);

            if (_soundStepper != null)
            {
                _soundStepper.Choices = k_SoundChoices;
                _soundStepper.SelectedIndex = SoundValueToIndex(_viewModel.Sound);
                _soundStepper.RegisterValueChangedCallback(OnSoundChanged);
            }

            if (_musicStepper != null)
            {
                _musicStepper.Choices = k_SoundChoices;
                _musicStepper.SelectedIndex = SoundValueToIndex(_viewModel.Music);
                _musicStepper.RegisterValueChangedCallback(OnMusicChanged);
            }

            if (_notificationsStepper != null)
            {
                _notificationsStepper.Choices = k_NotificationsChoices;
                _notificationsStepper.SelectedIndex = _viewModel.PushNotifications ? 1 : 0;
                _notificationsStepper.RegisterValueChangedCallback(OnNotificationsChanged);
            }

            if (_notificationTimeStepper != null)
            {
                _notificationTimeStepper.Choices = k_NotificationTimeChoices;
                _notificationTimeStepper.SelectedIndex = NotificationTimeToIndex(_viewModel.NotificationPreferredTime);
                _notificationTimeStepper.RegisterValueChangedCallback(OnNotificationTimeChanged);
            }

            UpdateNotificationTimeVisibility(_viewModel.PushNotifications);

            if (_environmentStepper != null)
            {
                _environmentStepper.Choices = ApiConfig.Environments
                    .Select(e => e.Name)
                    .ToArray();
                _environmentStepper.SelectedIndex = ApiConfig.ActiveEnvironmentIndex;
                _environmentStepper.RegisterValueChangedCallback(OnEnvironmentChanged);
            }

            if (_localUrlFieldComponent != null)
            {
                _localUrlFieldComponent.TextFieldValue = ApiConfig.LocalUrl;
                _localUrlInnerField = _localUrlFieldComponent.Q<Unity.AppUI.UI.TextField>();
                if (_localUrlInnerField != null)
                    _localUrlInnerField.RegisterValueChangedCallback(OnLocalUrlChanged);
            }

            if (_versionValue != null)
            {
                _versionValue.text = ApiConfig.AppVersion;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (_testSurveyButton != null)
            {
                _testSurveyButton.clicked -= OnTestSurveyClicked;
            }

            _appearanceBinder?.Unbind();

            if (_soundStepper != null)
            {
                _soundStepper.UnregisterValueChangedCallback(OnSoundChanged);
            }

            if (_musicStepper != null)
            {
                _musicStepper.UnregisterValueChangedCallback(OnMusicChanged);
            }

            if (_notificationsStepper != null)
            {
                _notificationsStepper.UnregisterValueChangedCallback(OnNotificationsChanged);
            }

            if (_notificationTimeStepper != null)
            {
                _notificationTimeStepper.UnregisterValueChangedCallback(OnNotificationTimeChanged);
            }

            if (_environmentStepper != null)
                _environmentStepper.UnregisterValueChangedCallback(OnEnvironmentChanged);
            if (_localUrlInnerField != null)
                _localUrlInnerField.UnregisterValueChangedCallback(OnLocalUrlChanged);

            _themeStepper = null;
            _langStepper = null;
            _scaleStepper = null;
            _fontStepper = null;
            _soundStepper = null;
            _musicStepper = null;
            _notificationsStepper = null;
            _notificationTimeStepper = null;
            _backgroundStepper = null;
            _environmentStepper = null;
            _localUrlFieldComponent = null;
            _localUrlInnerField = null;
            _versionValue = null;

            base.OnViewModelUnbinding();
        }

        private void OnSoundChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetSound(IndexToSoundValue(evt.newValue));

        private void OnMusicChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetMusic(IndexToSoundValue(evt.newValue));

        private void OnNotificationsChanged(object sender, ChangeEvent<int> evt)
        {
            bool enabled = evt.newValue == 1;
            _viewModel?.SetPushNotifications(enabled);
            UpdateNotificationTimeVisibility(enabled);
        }

        private void OnNotificationTimeChanged(object sender, ChangeEvent<int> evt) =>
            _viewModel?.SetNotificationPreferredTime(IndexToNotificationTime(evt.newValue));

        private void UpdateNotificationTimeVisibility(bool visible)
        {
            if (_notificationTimeStepper != null)
            {
                _notificationTimeStepper.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static int NotificationTimeToIndex(string time)
        {
            if (string.IsNullOrEmpty(time)) return 10; // Default 10:00 AM local
            int idx = Array.IndexOf(k_NotificationTimeChoices, time);
            return idx >= 0 ? idx : 10;
        }

        private static string IndexToNotificationTime(int index)
        {
            if (index >= 0 && index < k_NotificationTimeChoices.Length)
            {
                return k_NotificationTimeChoices[index];
            }
            return "10:00";
        }

        private static int SoundValueToIndex(int volume) =>
            System.Math.Clamp(volume / 5, 0, 20);

        private static int IndexToSoundValue(int index) =>
            index * 5;

        private void OnEnvironmentChanged(object sender, ChangeEvent<int> evt)
        {
            if (_isSettingEnvironment || _viewModel == null) return;

            int oldValue = evt.previousValue;
            int newValue = evt.newValue;

            // When switching to Local, require a URL before showing confirmation
            if (newValue >= 0 && newValue < ApiConfig.Environments.Count
                && ApiConfig.Environments[newValue].Name == "Local"
                && string.IsNullOrEmpty(ApiConfig.LocalUrl))
            {
                _isSettingEnvironment = true;
                _environmentStepper.SelectedIndex = oldValue;
                _isSettingEnvironment = false;

                FMDialog.ShowAlert(
                    contentContainer,
                    "@UI:ENV_CHANGE_TITLE",
                    // Development-only message: never shown in distributed builds, so not localized
                    "Enter the local server URL before switching to the Local environment."
                );
                _localUrlInnerField?.Focus();
                return;
            }

            FMDialog.ShowConfirm(
                contentContainer,
                "@UI:ENV_CHANGE_TITLE",
                "@UI:ENV_CHANGE_MSG",
                onConfirm: () =>
                {
                    _isSettingEnvironment = true;
                    ApiConfig.SetActiveEnvironment(newValue);
                    _isSettingEnvironment = false;
                    _viewModel.Logout();
                },
                onCancel: () =>
                {
                    _isSettingEnvironment = true;
                    _environmentStepper.SelectedIndex = oldValue;
                    _isSettingEnvironment = false;
                }
            );
        }

        private void OnLocalUrlChanged(ChangeEvent<string> evt)
        {
            ApiConfig.LocalUrl = evt.newValue;
        }
    }
}
