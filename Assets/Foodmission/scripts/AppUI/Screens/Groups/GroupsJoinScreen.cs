using System;
using System.ComponentModel;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
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
    class GroupsJoinScreen : NavigationScreenBase<GroupsJoinViewModel>
    {
        private Unity.AppUI.UI.TextField _inviteCodeField;
        private Unity.AppUI.UI.Button _btnJoin;

        public GroupsJoinScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.GroupsJoin));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _inviteCodeField = contentContainer.Q<Unity.AppUI.UI.TextField>("invite-code-field");
            _btnJoin = contentContainer.Q<Unity.AppUI.UI.Button>("btn-join");
        }

        private void RegisterManualEvents()
        {
            if (_btnJoin != null)
                _btnJoin.clicked += OnJoinClicked;

            if (_inviteCodeField != null)
                _inviteCodeField.RegisterValueChangedCallback(OnInviteCodeChanged);
        }

        private void UnregisterManualEvents()
        {
            if (_btnJoin != null)
                _btnJoin.clicked -= OnJoinClicked;

            if (_inviteCodeField != null)
                _inviteCodeField.UnregisterValueChangedCallback(OnInviteCodeChanged);
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

        private void OnInviteCodeChanged(ChangeEvent<string> evt)
        {
            if (_viewModel != null)
                _viewModel.InviteCode = evt.newValue;
        }

        protected override void OnNavigationRequested(string navigationAction, Argument[] arguments)
        {
            if (navigationAction == Unity.AppUI.Navigation.Generated.Actions.groups_to_detail
                && _viewModel.JoinedGroup != null)
            {
                _navController?.Navigate(
                    navigationAction,
                    new[] { new Argument("groupId", _viewModel.JoinedGroup.id) });
            }
            else
            {
                base.OnNavigationRequested(navigationAction, arguments);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsJoining):
                    UpdateJoiningState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void UpdateJoiningState()
        {
            bool isJoining = _viewModel.IsJoining;
            if (isJoining)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);

            if (_btnJoin != null) _btnJoin.SetEnabled(!isJoining);
            if (_inviteCodeField != null) _inviteCodeField.SetEnabled(!isJoining);
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

        private async void OnJoinClicked()
        {
            if (_viewModel == null) return;

            if (_inviteCodeField != null)
                _viewModel.InviteCode = _inviteCodeField.value;

            await _viewModel.JoinGroupAsync();
        }
    }
}
