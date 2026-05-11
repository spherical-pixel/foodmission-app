using System;
using System.ComponentModel;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class GroupsCreateScreen : NavigationScreenBase<GroupsCreateViewModel>
    {
        private Unity.AppUI.UI.TextField _nameField;
        private Unity.AppUI.UI.TextField _descField;
        private Unity.AppUI.UI.Button _btnCreate;

        public GroupsCreateScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.GroupsCreate));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _nameField = contentContainer.Q<Unity.AppUI.UI.TextField>("name-field");
            _descField = contentContainer.Q<Unity.AppUI.UI.TextField>("desc-field");
            _btnCreate = contentContainer.Q<Unity.AppUI.UI.Button>("btn-create");
        }

        private void RegisterManualEvents()
        {
            if (_btnCreate != null)
                _btnCreate.clicked += OnCreateClicked;

            if (_nameField != null)
                _nameField.RegisterValueChangedCallback(OnNameChanged);
        }

        private void UnregisterManualEvents()
        {
            if (_btnCreate != null)
                _btnCreate.clicked -= OnCreateClicked;

            if (_nameField != null)
                _nameField.UnregisterValueChangedCallback(OnNameChanged);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnPropertyChanged;
                _viewModel.NavigationRequested += OnNavigationRequested;
            }
        }

        protected override void OnViewModelUnbinding()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnPropertyChanged;
                _viewModel.NavigationRequested -= OnNavigationRequested;
            }

            UnregisterManualEvents();
            base.OnViewModelUnbinding();
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            if (_viewModel != null)
                _viewModel.Name = evt.newValue;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsCreating):
                    UpdateCreatingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void UpdateCreatingState()
        {
            bool isCreating = _viewModel.IsCreating;
            if (isCreating)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);

            if (_btnCreate != null) _btnCreate.SetEnabled(!isCreating);
            if (_nameField != null) _nameField.SetEnabled(!isCreating);
            if (_descField != null) _descField.SetEnabled(!isCreating);
        }

        private void UpdateErrorState()
        {
            if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                Toast.Build(this, _viewModel.ErrorMessage, NotificationDuration.Long)
                    .SetStyle(NotificationStyle.Negative)
                    .SetPosition(PopupNotificationPlacement.Bottom)
                    .Show();
            }
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private async void OnCreateClicked()
        {
            if (_viewModel == null) return;

            if (_nameField != null)
                _viewModel.Name = _nameField.value;

            if (_descField != null)
                _viewModel.Description = _descField.value;

            await _viewModel.CreateGroupAsync();
        }
    }
}
