using System;
using System.ComponentModel;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Text;

namespace eu.foodmission.platform
{
    [Preserve]
    class GroupDetailScreen : NavigationScreenBase<GroupDetailViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;

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
        private Unity.AppUI.UI.Button _btnCopy;
        private Unity.AppUI.UI.Button _btnAddMember;
        private Unity.AppUI.UI.Button _btnEditGroup;

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
            _btnCopy = contentContainer.Q<Unity.AppUI.UI.Button>("btn-copy");
            _btnAddMember = contentContainer.Q<Unity.AppUI.UI.Button>("btn-add-member");
            _btnEditGroup = contentContainer.Q<Unity.AppUI.UI.Button>("btn-edit-group");

        }

        private void RegisterManualEvents()
        {
            if (_btnLeave != null)
                _btnLeave.clicked += OnLeaveClicked;

            if (_btnDelete != null)
                _btnDelete.clicked += OnDeleteClicked;

            if (_btnRegenerate != null)
                _btnRegenerate.clicked += OnRegenerateClicked;

            if (_btnCopy != null)
                _btnCopy.clicked += OnCopyClicked;

            if (_btnAddMember != null)
                _btnAddMember.clicked += OnAddMemberClicked;

            if (_btnEditGroup != null)
                _btnEditGroup.clicked += OnEditGroupClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btnLeave != null)
                _btnLeave.clicked -= OnLeaveClicked;

            if (_btnDelete != null)
                _btnDelete.clicked -= OnDeleteClicked;

            if (_btnRegenerate != null)
                _btnRegenerate.clicked -= OnRegenerateClicked;

            if (_btnCopy != null)
                _btnCopy.clicked -= OnCopyClicked;

            if (_btnAddMember != null)
                _btnAddMember.clicked -= OnAddMemberClicked;

            if (_btnEditGroup != null)
                _btnEditGroup.clicked -= OnEditGroupClicked;
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
                    UpdateGroupInfo();
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
                int count = _viewModel.Members != null && _viewModel.Members.Count > 0
                    ? _viewModel.Members.Count
                    : (_viewModel.Group?.members?.Length ?? 0);
                _groupCount.text = count.ToString();
            }
        }

        private void UpdateAdminVisibility()
        {
            bool isAdmin = _viewModel.IsAdmin;
            _inviteSection?.EnableInClassList("visible", isAdmin);
            _adminActions?.EnableInClassList("visible", isAdmin);

            if (_btnEditGroup != null)
            {
                _btnEditGroup.style.display = isAdmin ? DisplayStyle.Flex : DisplayStyle.None;
            }
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

            string adminLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADMIN_BADGE") ?? "ADMIN";
            string virtualLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "VIRTUAL_MEMBER_BADGE") ?? "VIRTUAL";

            foreach (GroupMember member in _viewModel.Members)
            {
                GroupMember capturedMember = member;
                string memberUsername = GetMemberUsername(capturedMember);
                string memberDetail = !string.IsNullOrEmpty(capturedMember.nickname) && capturedMember.nickname != memberUsername
                    ? capturedMember.nickname
                    : "";

                FMItemMember memberItem = new FMItemMember
                {
                    Text = memberUsername,
                    Detail = memberDetail
                };

                memberItem.SetAdminBadge(capturedMember.role == "ADMIN", adminLabel);
                memberItem.SetVirtualBadge(capturedMember.isVirtual, virtualLabel);

                bool isMemberAdmin = string.Equals(capturedMember.role, "ADMIN", StringComparison.OrdinalIgnoreCase);
                if (!isMemberAdmin && !capturedMember.isVirtual)
                {
                    memberItem.MakeAdminButton.style.display = DisplayStyle.Flex;
                    memberItem.MakeAdminButton.clicked += () =>
                    {
                        Debug.Log($"[GroupDetailScreen] MakeAdminButton clicked for member {capturedMember.id}");
                        OnMakeAdminClicked(capturedMember);
                    };
                }

                // if (capturedMember.isVirtual)
                // {
                //     memberItem.EditButton.style.display = DisplayStyle.Flex;
                //     memberItem.EditButton.clicked += () => OnEditVirtualMemberClicked(capturedMember);
                // }

                // if (_viewModel.IsAdmin || capturedMember.isVirtual)
                // {
                //     memberItem.RemoveButton.style.display = DisplayStyle.Flex;
                //     memberItem.RemoveButton.clicked += () => OnRemoveMemberClicked(capturedMember);
                // }

                _membersContainer?.Add(memberItem);
            }
        }

        private string GetMemberUsername(GroupMember m)
        {
            if (m == null) return "Unknown";
            if (!string.IsNullOrEmpty(m.username)) return m.username;
            if (!string.IsNullOrEmpty(m.nickname)) return m.nickname;
            if (!string.IsNullOrEmpty(m.name) && !m.name.Contains("@")) return m.name;
            if (!string.IsNullOrEmpty(m.email))
            {
                int atIndex = m.email.IndexOf('@');
                return atIndex > 0 ? m.email.Substring(0, atIndex) : m.email;
            }
            return !string.IsNullOrEmpty(m.name) ? m.name : "Unknown";
        }

        private void OnEditGroupClicked()
        {
            var container = new VisualElement();

            var nameField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GROUP_NAME"),
                value = _viewModel.Group?.name ?? ""
            };
            nameField.style.marginBottom = 8;
            container.Add(nameField);

            var descField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "GROUP_DESCRIPTION"),
                value = _viewModel.Group?.description ?? ""
            };
            container.Add(descField);

            FMDialog.ShowCustom(
                this,
                "@UI:EDIT_GROUP",
                container,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    string name = nameField.value?.Trim();
                    string desc = descField.value?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _ = SafeUpdateGroupAsync(name, desc);
                    }
                }, ButtonVariant.Accent));
        }

        private void OnEditVirtualMemberClicked(GroupMember member)
        {
            var container = new VisualElement();

            var nameField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEMBER_NAME"),
                value = member.nickname ?? member.name ?? ""
            };
            container.Add(nameField);

            FMDialog.ShowCustom(
                this,
                "@UI:EDIT_MEMBER",
                container,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:SAVE", () =>
                {
                    string name = nameField.value?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _ = SafeUpdateVirtualMemberAsync(member.id, name);
                    }
                }, ButtonVariant.Accent));
        }

        private void OnMakeAdminClicked(GroupMember member)
        {
            Debug.LogError("OnMakeAdminClicked CLICK ->>> ");
            string displayName = GetMemberUsername(member);
            string title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MAKE_ADMIN") ?? "Make Admin";
            string message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_MAKE_ADMIN", new object[] { displayName })
                ?? $"Are you sure you want to promote {displayName} to group administrator?";

            FMDialog.ShowConfirm(
                this,
                title,
                message,
                onConfirm: () => _ = SafeMakeAdminAsync(member.id, displayName));
        }

        private void OnRemoveMemberClicked(GroupMember member)
        {
            string displayName = !string.IsNullOrEmpty(member.name) ? member.name : (member.nickname ?? "member");

            FMDialog.ShowConfirm(
                this,
                "@UI:REMOVE_MEMBER",
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_REMOVE_MEMBER", new object[] { displayName }),
                onConfirm: () => _ = SafeRemoveMemberAsync(member.id),
                semantic: AlertSemantic.Destructive);
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

        private void OnCopyClicked()
        {
            string code = _viewModel?.InviteCode;
            if (string.IsNullOrEmpty(code))
            {
                code = _inviteCodeText?.text;
            }

            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            Platform.SetPasteboardData(PasteboardType.Text, Encoding.UTF8.GetBytes(code));

            string copiedMsg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "INVITE_CODE_COPIED");
            if (string.IsNullOrEmpty(copiedMsg))
            {
                copiedMsg = "Code copied to clipboard";
            }

            Toast.Build(this, copiedMsg, NotificationDuration.Short)
                .SetPosition(PopupNotificationPlacement.Bottom)
                .Show();
        }

        private void OnAddMemberClicked()
        {
            var container = new VisualElement();
            var nameField = new Unity.AppUI.UI.TextField
            {
                placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEMBER_NAME")
            };
            container.Add(nameField);

            FMDialog.ShowCustom(
                this,
                "@UI:ADD_VIRTUAL_MEMBER",
                container,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:ADD_MEMBER", () =>
                {
                    string name = nameField.value?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _ = SafeAddMemberAsync(name);
                    }
                }, ButtonVariant.Accent));
        }

        private async Task SafeUpdateGroupAsync(string name, string description)
        {
            try
            {
                await _viewModel.UpdateGroupAsync(name, description);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] UpdateGroupAsync failed: {ex.Message}");
            }
        }

        private async Task SafeUpdateVirtualMemberAsync(string memberId, string name)
        {
            try
            {
                await _viewModel.UpdateVirtualMemberAsync(memberId, name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] UpdateVirtualMemberAsync failed: {ex.Message}");
            }
        }

        private async Task SafeRemoveMemberAsync(string memberId)
        {
            try
            {
                await _viewModel.RemoveMemberAsync(memberId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] RemoveMemberAsync failed: {ex.Message}");
            }
        }

        private async Task SafeMakeAdminAsync(string memberId, string displayName)
        {
            try
            {
                await _viewModel.MakeAdminAsync(memberId);
                if (_viewModel.ErrorDetail == null)
                {
                    string toastMsg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MEMBER_PROMOTED_ADMIN", new object[] { displayName })
                        ?? $"{displayName} is now an administrator";
                    Toast.Build(this, toastMsg, NotificationDuration.Short)
                        .SetPosition(PopupNotificationPlacement.Bottom)
                        .Show();
                }
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
