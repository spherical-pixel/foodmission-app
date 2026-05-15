
using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public enum AvatarEditorItemEnum
    {
        Hair,
        Eyebrows,
        Eyes,
        Nose,
        Mouth,
        FacialHair,
        Skin,
        Tshirt,
        Trousers,
        Shoes
    }

    [Preserve]
    class AvatarEditor : NavigationScreenBase<AvatarEditorViewModel>
    {
        override protected bool IsFixedContent => true;
        override protected bool ApplySafeAreaTop => false;
        override protected bool ApplySafeAreaBottom => false;
        override protected bool ApplySafeAreaLeft => false;
        override protected bool ApplySafeAreaRight => false;

        private Unity.AppUI.UI.Button _btHair;
        private Unity.AppUI.UI.Button _btEyebrows;
        private Unity.AppUI.UI.Button _btEyes;
        private Unity.AppUI.UI.Button _btNose;
        private Unity.AppUI.UI.Button _btMouth;
        private Unity.AppUI.UI.Button _btFacialHair;
        private Unity.AppUI.UI.Button _btSkin;
        private Unity.AppUI.UI.Button _btTshirt;
        private Unity.AppUI.UI.Button _btTrousers;
        private Unity.AppUI.UI.Button _btShoes;
        private Unity.AppUI.UI.Button _btSave;
        private Unity.AppUI.UI.Button _btExit;

        private AccessibilityNode _btHairNode;
        private AccessibilityNode _btEyebrowsNode;
        private AccessibilityNode _btEyesNode;
        private AccessibilityNode _btNoseNode;
        private AccessibilityNode _btMouthNode;
        private AccessibilityNode _btFacialHairNode;
        private AccessibilityNode _btSkinNode;
        private AccessibilityNode _btTshirtNode;
        private AccessibilityNode _btTrousersNode;
        private AccessibilityNode _btShoesNode;
        private AccessibilityNode _btSaveNode;
        private AccessibilityNode _btExitNode;

        private bool _isFromOnboarding;
        private AvatarEditorPanelItem _avatarEditorPanelItem;

        public AvatarEditor()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.AvatarEditor));
            CacheUIElements();
            RegisterManualEvents();
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            _isFromOnboarding = false;
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.name == "fromOnboarding")
                        _isFromOnboarding = arg.value?.ToString() == "true";
                }
            }
            base.OnEnter(controller, destination, args);

            _viewModel?.AvatarService.SetFullBodyCameraActive(true);

            if (_isFromOnboarding && appBar != null)
            {
                appBar.style.display = DisplayStyle.None;
            }
        }

        private void CacheUIElements()
        {
            _btHair = contentContainer.Q<Unity.AppUI.UI.Button>("btHair");
            _btEyebrows = contentContainer.Q<Unity.AppUI.UI.Button>("btEyebrows");
            _btEyes = contentContainer.Q<Unity.AppUI.UI.Button>("btEyes");
            _btNose = contentContainer.Q<Unity.AppUI.UI.Button>("btNose");
            _btMouth = contentContainer.Q<Unity.AppUI.UI.Button>("btMouth");
            _btFacialHair = contentContainer.Q<Unity.AppUI.UI.Button>("btFacialHair");
            _btSkin = contentContainer.Q<Unity.AppUI.UI.Button>("btSkin");
            _btTshirt = contentContainer.Q<Unity.AppUI.UI.Button>("btTshirt");
            _btTrousers = contentContainer.Q<Unity.AppUI.UI.Button>("btTrousers");
            _btShoes = contentContainer.Q<Unity.AppUI.UI.Button>("btShoes");
            _btSave = contentContainer.Q<Unity.AppUI.UI.Button>("btSave");
            _btExit = contentContainer.Q<Unity.AppUI.UI.Button>("btExit");
        }

        private void RegisterManualEvents()
        {
            if (_btHair != null) _btHair.clicked += OnHairClicked;
            if (_btEyebrows != null) _btEyebrows.clicked += OnEyebrowsClicked;
            if (_btEyes != null) _btEyes.clicked += OnEyesClicked;
            if (_btNose != null) _btNose.clicked += OnNoseClicked;
            if (_btMouth != null) _btMouth.clicked += OnMouthClicked;
            if (_btFacialHair != null) _btFacialHair.clicked += OnFacialHairClicked;
            if (_btSkin != null) _btSkin.clicked += OnSkinClicked;
            if (_btTshirt != null) _btTshirt.clicked += OnTshirtClicked;
            if (_btTrousers != null) _btTrousers.clicked += OnTrousersClicked;
            if (_btShoes != null) _btShoes.clicked += OnShoesClicked;
            if (_btSave != null) _btSave.clicked += OnSaveClicked;
            if (_btExit != null) _btExit.clicked += OnExitClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btHair != null) _btHair.clicked -= OnHairClicked;
            if (_btEyebrows != null) _btEyebrows.clicked -= OnEyebrowsClicked;
            if (_btEyes != null) _btEyes.clicked -= OnEyesClicked;
            if (_btNose != null) _btNose.clicked -= OnNoseClicked;
            if (_btMouth != null) _btMouth.clicked -= OnMouthClicked;
            if (_btFacialHair != null) _btFacialHair.clicked -= OnFacialHairClicked;
            if (_btSkin != null) _btSkin.clicked -= OnSkinClicked;
            if (_btTshirt != null) _btTshirt.clicked -= OnTshirtClicked;
            if (_btTrousers != null) _btTrousers.clicked -= OnTrousersClicked;
            if (_btShoes != null) _btShoes.clicked -= OnShoesClicked;
            if (_btSave != null) _btSave.clicked -= OnSaveClicked;
            if (_btExit != null) _btExit.clicked -= OnExitClicked;
        }

        private void OnHairClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Hair);
        private void OnEyebrowsClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Eyebrows);
        private void OnEyesClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Eyes);
        private void OnNoseClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Nose);
        private void OnMouthClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Mouth);
        private void OnFacialHairClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.FacialHair);
        private void OnSkinClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Skin);
        private void OnTshirtClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Tshirt);
        private void OnTrousersClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Trousers);
        private void OnShoesClicked() => OpenSelectorItemAvatar(AvatarEditorItemEnum.Shoes);

        private void OnSaveClicked()
        {
            _viewModel?.AvatarService.SaveCurrentConfig();
            CloseSelectorItemAvatar();
            OnNavigationRequested(Actions.go_to_home,null);
        }

        private void OnExitClicked()
        {
            _viewModel?.AvatarService.LoadSavedConfig();
            CloseSelectorItemAvatar();
            OnNavigationRequested(Actions.go_to_home,null);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            if (_btSave != null)
                _btSave.style.display = _isFromOnboarding ? DisplayStyle.Flex : DisplayStyle.None;
            if (_btExit != null)
                _btExit.style.display = _isFromOnboarding ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void OnViewModelUnbinding()
        {
            if (!_isFromOnboarding)
            {
                _viewModel?.AvatarService.SaveCurrentConfig();
            }

            _viewModel?.AvatarService.SetFullBodyCameraActive(false);
            _viewModel?.AvatarService.SetAvatarCameraActive(false);

            UnregisterManualEvents();
            base.OnViewModelUnbinding();
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _btHairNode = CreateButtonNode(h, _btHair, "Hair");
            _btEyebrowsNode = CreateButtonNode(h, _btEyebrows, "Eyebrows");
            _btEyesNode = CreateButtonNode(h, _btEyes, "Eyes");
            _btNoseNode = CreateButtonNode(h, _btNose, "Nose");
            _btMouthNode = CreateButtonNode(h, _btMouth, "Mouth");
            _btFacialHairNode = CreateButtonNode(h, _btFacialHair, "Facial hair");
            _btSkinNode = CreateButtonNode(h, _btSkin, "Skin");
            _btTshirtNode = CreateButtonNode(h, _btTshirt, "T-shirt");
            _btTrousersNode = CreateButtonNode(h, _btTrousers, "Trousers");
            _btShoesNode = CreateButtonNode(h, _btShoes, "Shoes");
            _btSaveNode = CreateButtonNode(h, _btSave, "Save avatar");
            _btExitNode = CreateButtonNode(h, _btExit, "Exit without saving");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _btHairNode = null;
            _btEyebrowsNode = null;
            _btEyesNode = null;
            _btNoseNode = null;
            _btMouthNode = null;
            _btFacialHairNode = null;
            _btSkinNode = null;
            _btTshirtNode = null;
            _btTrousersNode = null;
            _btShoesNode = null;
            _btSaveNode = null;
            _btExitNode = null;
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

        private void OpenSelectorItemAvatar(AvatarEditorItemEnum itemEnum)
        {
            _avatarEditorPanelItem = new AvatarEditorPanelItem();
            _avatarEditorPanelItem.Init(CloseSelectorItemAvatar, itemEnum, _viewModel.AvatarService);
            Add(_avatarEditorPanelItem);
        }

        private void CloseSelectorItemAvatar()
        {
            Debug.Log("CloseSelectorItemAvatar");
            if (_avatarEditorPanelItem != null)
            {
                _avatarEditorPanelItem.Dispose();
                Remove(_avatarEditorPanelItem);
                _avatarEditorPanelItem = null;
            }
        }
    }
}
