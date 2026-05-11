using System;
using System.Collections.Generic;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [UxmlElement]
    partial class AvatarEditorPanelItem : VisualElement, IDisposable
    {
        private Unity.AppUI.UI.Button _btLeftParts;
        private Unity.AppUI.UI.Button _btRightParts;
        private Unity.AppUI.UI.Button _btLeftColor;
        private Unity.AppUI.UI.Button _btRightColor;
        private Unity.AppUI.UI.Button _btOk;
        private Unity.AppUI.UI.Button _btKo;

        private ScrollView _scrollParts;
        private ScrollView _scrollColor;

        private UnityAction _onClose;
        private AvatarEditorItemEnum _itemEnum;
        private IAvatarService _avatarService;
        private AvatarConfig _configSnapshot;
        private AvatarConfig _editingConfig;

        private VisualElement _selectorParts;
        private VisualElement _selectorColor;

        private const float SCROLL_STEP = 120f;

        public AvatarEditorPanelItem()
        {
            VisualTreeAsset template = App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.AvatarEditorPanelItem);
            Add(template.Instantiate());

            CacheUIElements();
            RegisterManualEvents();
        }

        public void Init(UnityAction onClose, AvatarEditorItemEnum itemEnum, IAvatarService avatarService)
        {
            _onClose = onClose;
            _itemEnum = itemEnum;
            _avatarService = avatarService;
            _configSnapshot = avatarService.GetCurrentAvatarConfig?.Copy();
            _editingConfig = avatarService.GetCurrentAvatarConfig?.Copy();

            PrepareButtonsForItem();
        }

        private void CacheUIElements()
        {
            _btLeftParts = contentContainer.Q<Unity.AppUI.UI.Button>("btLeftParts");
            _btRightParts = contentContainer.Q<Unity.AppUI.UI.Button>("btRightParts");
            _btLeftColor = contentContainer.Q<Unity.AppUI.UI.Button>("btLeftColor");
            _btRightColor = contentContainer.Q<Unity.AppUI.UI.Button>("btRightColor");
            _scrollParts = contentContainer.Q<ScrollView>("scrollParts");
            _scrollColor = contentContainer.Q<ScrollView>("scrollColor");
            _btOk = contentContainer.Q<Unity.AppUI.UI.Button>("btOk");
            _btKo = contentContainer.Q<Unity.AppUI.UI.Button>("btKo");
            _selectorParts = contentContainer.Q<VisualElement>("selectorParts");
            _selectorColor = contentContainer.Q<VisualElement>("selectorColor");
        }

        private void RegisterManualEvents()
        {
            if (_btLeftParts != null) _btLeftParts.clicked += OnLeftPartsClicked;
            if (_btRightParts != null) _btRightParts.clicked += OnRightPartsClicked;
            if (_btLeftColor != null) _btLeftColor.clicked += OnLeftColorClicked;
            if (_btRightColor != null) _btRightColor.clicked += OnRightColorClicked;
            if (_btOk != null) _btOk.clicked += OnOkClicked;
            if (_btKo != null) _btKo.clicked += OnKoClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btLeftParts != null) _btLeftParts.clicked -= OnLeftPartsClicked;
            if (_btRightParts != null) _btRightParts.clicked -= OnRightPartsClicked;
            if (_btLeftColor != null) _btLeftColor.clicked -= OnLeftColorClicked;
            if (_btRightColor != null) _btRightColor.clicked -= OnRightColorClicked;
            if (_btOk != null) _btOk.clicked -= OnOkClicked;
            if (_btKo != null) _btKo.clicked -= OnKoClicked;
        }

        public void Dispose()
        {
            UnregisterManualEvents();
        }

        private void OnLeftPartsClicked()
        {
            if (_scrollParts != null)
                _scrollParts.scrollOffset -= new Vector2(SCROLL_STEP, 0);
        }

        private void OnRightPartsClicked()
        {
            if (_scrollParts != null)
                _scrollParts.scrollOffset += new Vector2(SCROLL_STEP, 0);
        }

        private void OnLeftColorClicked()
        {
            if (_scrollColor != null)
                _scrollColor.scrollOffset -= new Vector2(SCROLL_STEP, 0);
        }

        private void OnRightColorClicked()
        {
            if (_scrollColor != null)
                _scrollColor.scrollOffset += new Vector2(SCROLL_STEP, 0);
        }

        private void OnOkClicked()
        {
            _onClose?.Invoke();
        }

        private void OnKoClicked()
        {
            if (_configSnapshot != null)
                _avatarService.SetAvatarConfig(_configSnapshot);
            _onClose?.Invoke();
        }

        private void PrepareButtonsForItem()
        {
            _scrollParts?.Clear();
            _scrollColor?.Clear();

            int maxParts = _avatarService.GetMaxPartCount(_itemEnum);
            List<Color> palette = _avatarService.GetColorPalette(_itemEnum);
            AvatarPartConfig targetPart = GetPartConfigForItem(_itemEnum, _editingConfig);

            for (int i = 0; i < maxParts; i++)
            {
                int partIndex = i;
                bool isSelected = partIndex == targetPart.idPart;

                var partBtn = CreatePartOption(partIndex, isSelected,_itemEnum);
                partBtn.clicked += () => OnPartSelected(partIndex);
                _scrollParts?.Add(partBtn);
            }

            for (int i = 0; i < palette.Count; i++)
            {
                int colorId = i + 1;
                bool isSelected = colorId == targetPart.idColor;

                var colorBtn = CreateColorSwatch(palette[i], isSelected);
                colorBtn.clicked += () => OnColorSelected(colorId);
                _scrollColor?.Add(colorBtn);
            }

            if (maxParts <= 1)
            {
                if (_btLeftParts != null) _btLeftParts.style.display = DisplayStyle.None;
                if (_btRightParts != null) _btRightParts.style.display = DisplayStyle.None;
                if( _selectorParts != null) _selectorParts.style.display = DisplayStyle.None;
            }
        }

        private void RefreshSelectionVisuals()
        {
            AvatarPartConfig targetPart = GetPartConfigForItem(_itemEnum, _editingConfig);

            var parts = _scrollParts?.Children().GetEnumerator();
            if (parts != null)
            {
                int idx = 0;
                while (parts.MoveNext())
                {
                    if (parts.Current is Unity.AppUI.UI.Button btn)
                    {
                        bool selected = idx == targetPart.idPart;
                        btn.EnableInClassList("option-selected", selected);
                    }
                    idx++;
                }
            }

            var colors = _scrollColor?.Children().GetEnumerator();
            if (colors != null)
            {
                int idx = 0;
                while (colors.MoveNext())
                {
                    if (colors.Current is Unity.AppUI.UI.Button btn)
                    {
                        bool selected = (idx + 1) == targetPart.idColor;
                        btn.EnableInClassList("option-selected", selected);
                    }
                    idx++;
                }
            }
        }

        private void OnPartSelected(int partIndex)
        {
            SetPartForItem(_itemEnum, _editingConfig, partIndex);
            _avatarService.SetAvatarConfig(_editingConfig);
            RefreshSelectionVisuals();
        }

        private void OnColorSelected(int colorId)
        {
            SetColorForItem(_itemEnum, _editingConfig, colorId);
            _avatarService.SetAvatarConfig(_editingConfig);
            RefreshSelectionVisuals();
        }

        // --- Part/Color accessor helpers ---

        private static AvatarPartConfig GetPartConfigForItem(AvatarEditorItemEnum item, AvatarConfig config)
        {
            if (config == null) return null;
            switch (item)
            {
                case AvatarEditorItemEnum.Hair: return config.hair;
                case AvatarEditorItemEnum.Eyebrows: return config.eyebrows;
                case AvatarEditorItemEnum.Eyes: return config.eyes;
                case AvatarEditorItemEnum.Nose: return config.nose;
                case AvatarEditorItemEnum.Mouth: return config.mouth;
                case AvatarEditorItemEnum.FacialHair: return config.facialHair;
                case AvatarEditorItemEnum.Skin: return config.skin;
                case AvatarEditorItemEnum.Tshirt: return config.tshirt;
                case AvatarEditorItemEnum.Trousers: return config.trousers;
                case AvatarEditorItemEnum.Shoes: return config.shoes;
                default: return null;
            }
        }

        private static void SetPartForItem(AvatarEditorItemEnum item, AvatarConfig config, int partIndex)
        {
            var part = GetPartConfigForItem(item, config);
            if (part != null) part.idPart = partIndex;
        }

        private static void SetColorForItem(AvatarEditorItemEnum item, AvatarConfig config, int colorId)
        {
            var part = GetPartConfigForItem(item, config);
            if (part != null) part.idColor = colorId;
        }

        // --- UI element builders ---

        private static Unity.AppUI.UI.Button CreatePartOption(int index, bool selected,AvatarEditorItemEnum itemEnum)
        {
            var btn = new Unity.AppUI.UI.Button();
            // {
            //     title = index == 0 ? "✕" :  index.ToString()
            // };

            if ( index == 0)
            {
                btn.leadingIcon = "fm-item-none";
            }
            else
            {
                btn.leadingIcon = "fm-icon-"+ itemEnum.ToString().ToLowerInvariant()+"-"+index.ToString();
            }
            btn.variant = Unity.AppUI.UI.ButtonVariant.Accent;
            
            btn.AddToClassList("fm-button-avatar-scroll");
            if (selected) btn.AddToClassList("option-selected");
            return btn;
        }

        private static Unity.AppUI.UI.Button CreateColorSwatch(Color color, bool selected)
        {
            var btn = new Unity.AppUI.UI.Button
            {
                style =
                {
                    /*width = 80,
                    height = 80,
                    marginLeft = 4,
                    marginRight = 4,*/
                    backgroundColor = color
                }
            };
            btn.variant = Unity.AppUI.UI.ButtonVariant.Accent;
            btn.AddToClassList("fm-button-avatar-scroll");
            if (selected) btn.AddToClassList("option-selected");
            return btn;
        }
    }
}
