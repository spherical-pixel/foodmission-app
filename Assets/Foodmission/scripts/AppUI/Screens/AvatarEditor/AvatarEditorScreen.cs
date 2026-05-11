
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine;
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

        private AvatarEditorPanelItem _avatarEditorPanelItem;

        public AvatarEditor()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.AvatarEditor));
            CacheUIElements();
            RegisterManualEvents();
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
        }

        private void OnExitClicked()
        {
            _viewModel?.AvatarService.LoadSavedConfig();
            CloseSelectorItemAvatar();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
        }

        protected override void OnViewModelUnbinding()
        {
            UnregisterManualEvents();
            base.OnViewModelUnbinding();
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
