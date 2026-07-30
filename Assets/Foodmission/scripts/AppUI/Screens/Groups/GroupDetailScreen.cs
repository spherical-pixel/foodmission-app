using System;
using System.ComponentModel;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class GroupDetailScreen : NavigationScreenBase<GroupDetailViewModel>
    {
        private Heading _groupTitle;
        private Text _groupDesc;
        private Text _groupCount;
        private Text _inviteCodeText;
        private VisualElement _inviteSection;
        private VisualElement _adminActions;
        private VisualElement _membersContainer;
        private Unity.AppUI.UI.Button _btnLeave;
        private Unity.AppUI.UI.Button _btnDelete;
        private Unity.AppUI.UI.Button _btnRegenerate;
        private Unity.AppUI.UI.Button _btnAddMember;
        private Text _errorText;

        private AccessibilityNode _leaveButtonNode;
        private AccessibilityNode _deleteButtonNode;
        private AccessibilityNode _regenerateButtonNode;
        private AccessibilityNode _addMemberButtonNode;

        public GroupDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.GroupDetail));
            CacheUIElements();
            RegisterManualEvents();
        }

        private void CacheUIElements()
        {
            _groupTitle = contentContainer.Q<Heading>("group-title");
            _groupDesc = contentContainer.Q<Text>("group-desc");
            _groupCount = contentContainer.Q<Text>("group-count");
            _inviteCodeText = contentContainer.Q<Text>("invite-code-text");
            _inviteSection = contentContainer.Q<VisualElement>("invite-section");
            _adminActions = contentContainer.Q<VisualElement>("admin-actions");
            _membersContainer = contentContainer.Q<VisualElement>("members-container");
            _btnLeave = contentContainer.Q<Unity.AppUI.UI.Button>("btn-leave");
            _btnDelete = contentContainer.Q<Unity.AppUI.UI.Button>("btn-delete");
            _btnRegenerate = contentContainer.Q<Unity.AppUI.UI.Button>("btn-regenerate");
            _btnAddMember = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add-member");
            _errorText = contentContainer.Q<Text>("error-message");
        }

        private void RegisterManualEvents()
        {
            if (_btnLeave != null)
                _btnLeave.clicked += OnLeaveClicked;

            if (_btnDelete != null)
                _btnDelete.clicked += OnDeleteClicked;

            if (_btnRegenerate != null)
                _btnRegenerate.clicked += OnRegenerateClicked;

            if (_btnAddMember != null)
                _btnAddMember.clicked += OnAddMemberClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btnLeave != null)
                _btnLeave.clicked -= OnLeaveClicked;

            if (_btnDelete != null)
                _btnDelete.clicked -= OnDeleteClicked;

            if (_btnRegenerate != null)
                _btnRegenerate.clicked -= OnRegenerateClicked;

            if (_btnAddMember != null)
                _btnAddMember.clicked -= OnAddMemberClicked;
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

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string groupId = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "groupId")
                    {
                        groupId = arg.value?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(groupId))
            {
                await _viewModel.LoadAsync(groupId);

                if (_viewModel.IsAdmin)
                {
                    await _viewModel.LoadInviteCodeAsync();
                }
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Group):
                    UpdateGroupInfo();
                    break;
                case nameof(_viewModel.Members):
                    RebuildMembers();
                    break;
                case nameof(_viewModel.IsAdmin):
                    UpdateAdminVisibility();
                    break;
                case nameof(_viewModel.InviteCode):
                    UpdateInviteCode();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void UpdateGroupInfo()
        {
            if (_viewModel.Group == null) return;

            if (_groupTitle != null)
                _groupTitle.text = _viewModel.Group.name;

            if (_groupDesc != null)
            {
                _groupDesc.text = !string.IsNullOrEmpty(_viewModel.Group.description)
                    ? _viewModel.Group.description
                    : "";
                _groupDesc.EnableInClassList("visible", !string.IsNullOrEmpty(_viewModel.Group.description));
            }

            if (_groupCount != null)
            {
                int count = _viewModel.Members?.Count ?? 0;
                _groupCount.text = count.ToString();
            }
        }

        private void UpdateAdminVisibility()
        {
            bool isAdmin = _viewModel.IsAdmin;
            _inviteSection?.EnableInClassList("visible", isAdmin);
            _adminActions?.EnableInClassList("visible", isAdmin);
        }

        private void UpdateInviteCode()
        {
            if (_inviteCodeText != null && !string.IsNullOrEmpty(_viewModel.InviteCode))
                _inviteCodeText.text = _viewModel.InviteCode;
        }

        private void UpdateLoadingState()
        {
            bool loading = _viewModel.IsLoading;
            if (loading)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _leaveButtonNode = CreateButtonNode(h, _btnLeave, "Leave group");
            _deleteButtonNode = CreateButtonNode(h, _btnDelete, "Delete group");
            _regenerateButtonNode = CreateButtonNode(h, _btnRegenerate, "Regenerate invite code");
            _addMemberButtonNode = CreateButtonNode(h, _btnAddMember, "Add virtual member");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _leaveButtonNode = null;
            _deleteButtonNode = null;
            _regenerateButtonNode = null;
            _addMemberButtonNode = null;
            base.TeardownAccessibilityNodes();
        }

        private AccessibilityNode CreateButtonNode(AccessibilityHierarchy hierarchy, VisualElement button, string label)
        {
            if (button == null) return null;
            var node = hierarchy.AddNode(label);
            node.role = AccessibilityRole.Button;
            if (!button.enabledSelf) node.state = AccessibilityState.Disabled;
            node.frameGetter = () =>
            {
                if (button.panel == null) return Rect.zero;
                var r = button.worldBound;
                var s = button.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
            node.invoked += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
                return true;
            };
            return node;
        }

        private void RebuildMembers()
        {
            _membersContainer?.Clear();

            if (_viewModel.Members == null) return;

            foreach (GroupMember member in _viewModel.Members)
            {
                var row = new VisualElement();
                row.AddToClassList("fm-gd-member-row");

                var info = new VisualElement();
                info.AddToClassList("fm-gd-member-info");

                var nameLabel = new Text
                {
                    text = !string.IsNullOrEmpty(member.name)
                        ? member.name
                        : (!string.IsNullOrEmpty(member.email) ? member.email : "Unknown")
                };
                nameLabel.AddToClassList("fm-gd-member-name");

                info.Add(nameLabel);

                if (!string.IsNullOrEmpty(member.nickname))
                {
                    var nickLabel = new Text { text = member.nickname };
                    nickLabel.AddToClassList("fm-gd-member-email");
                    info.Add(nickLabel);
                }

                row.Add(info);

                if (member.role == "ADMIN")
                {
                    var badge = new Text
                    {
                        text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADMIN_BADGE")
                    };
                    badge.AddToClassList("fm-gd-member-badge");
                    badge.AddToClassList("fm-gd-member-badge--admin");
                    row.Add(badge);
                }

                if (member.isVirtual)
                {
                    var badge = new Text
                    {
                        text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "VIRTUAL_MEMBER_BADGE")
                    };
                    badge.AddToClassList("fm-gd-member-badge");
                    badge.AddToClassList("fm-gd-member-badge--virtual");
                    row.Add(badge);
                }

                if (_viewModel.IsAdmin && member.role != "ADMIN")
                {
                    string capturedMemberId = member.id;
                    var makeAdminBtn = new Unity.AppUI.UI.Button
                    {
                        title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MAKE_ADMIN")
                    };
                    makeAdminBtn.clicked += () => _ = SafeMakeAdminAsync(capturedMemberId);
                    makeAdminBtn.AddToClassList("fm-gd-member-actions");
                    row.Add(makeAdminBtn);
                }

                _membersContainer?.Add(row);
            }
        }

        private void OnLeaveClicked()
        {
            FMDialog.ShowConfirm(
                this,
                "@UI:LEAVE_GROUP",
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_LEAVE_GROUP"),
                onConfirm: () => _ = SafeLeaveAsync(),
                semantic: AlertSemantic.Destructive);
        }

        private void OnDeleteClicked()
        {
            FMDialog.ShowConfirm(
                this,
                "@UI:DELETE_GROUP",
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_GROUP"),
                onConfirm: () => _ = SafeDeleteAsync(),
                semantic: AlertSemantic.Destructive);
        }

        private void OnRegenerateClicked()
        {
            _ = SafeRegenerateCodeAsync();
        }

        private void OnAddMemberClicked()
        {
            var nameField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEMBER_NAME")
            };

            FMDialog.ShowCustom(
                this,
                "@UI:ADD_VIRTUAL_MEMBER",
                nameField,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:ADD_MEMBER", () =>
                {
                    string name = nameField.value?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _ = SafeAddMemberAsync(name);
                    }
                }, isPrimary: true));
        }

        private async Task SafeMakeAdminAsync(string memberId)
        {
            try
            {
                await _viewModel.MakeAdminAsync(memberId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] MakeAdminAsync failed: {ex.Message}");
            }
        }

        private async Task SafeLeaveAsync()
        {
            try
            {
                await _viewModel.LeaveGroupAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LeaveGroupAsync failed: {ex.Message}");
            }
        }

        private async Task SafeDeleteAsync()
        {
            try
            {
                await _viewModel.DeleteGroupAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] DeleteGroupAsync failed: {ex.Message}");
            }
        }

        private async Task SafeRegenerateCodeAsync()
        {
            try
            {
                await _viewModel.RegenerateInviteCodeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] RegenerateInviteCodeAsync failed: {ex.Message}");
            }
        }

        private async Task SafeAddMemberAsync(string name)
        {
            try
            {
                await _viewModel.AddVirtualMemberAsync(name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] AddVirtualMemberAsync failed: {ex.Message}");
            }
        }
    }
}
