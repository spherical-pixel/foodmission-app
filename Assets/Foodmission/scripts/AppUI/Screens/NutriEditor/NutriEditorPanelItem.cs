using System;
using System.Collections.Generic;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [UxmlElement]
    public partial class NutriEditorPanelItem : VisualElement, IDisposable
    {
        private VisualElement _selectorItemNutri;
        private Unity.AppUI.UI.Button _btLeftParts;
        private Unity.AppUI.UI.Button _btRightParts;
        private Unity.AppUI.UI.Button _btConfirm;
        private Unity.AppUI.UI.Button _btCancel;
        private ScrollView _scrollParts;
        private Unity.AppUI.UI.Heading _categoryHeading;

        private UnityAction _onClose;
        private string _category;
        private NutriEditorViewModel _viewModel;
        private VisualElement _dialogAnchor;
        private int _initialSlot;
        private int _currentSelectedSlot;
        private bool _isClosing;

        public bool IsClosing => _isClosing;

        private readonly List<(int slot, Unity.AppUI.UI.Button button, VisualElement costBadge)> _buttonEntries = new();
        private const float SCROLL_STEP = 150f;

        public NutriEditorPanelItem()
        {
            var templateService = App.current?.services?.GetService<ITemplateService>();
            VisualTreeAsset template = templateService?.Get(TemplateAddresses.NutriEditorPanelItem);

#if UNITY_EDITOR
            if (template == null)
            {
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Foodmission/scripts/AppUI/Screens/NutriEditor/NutriEditorPanelItem.uxml");
            }
#endif

            if (template != null)
            {
                Add(template.Instantiate());
                CacheUIElements();
                RegisterManualEvents();
            }
        }

        public void Init(UnityAction onClose, string category, NutriEditorViewModel viewModel, VisualElement dialogAnchor)
        {
            _onClose = onClose;
            _category = category;
            _viewModel = viewModel;
            _dialogAnchor = dialogAnchor ?? this;

            _initialSlot = _viewModel?.GetEquippedSlot(_category) ?? 0;
            _currentSelectedSlot = _initialSlot;

            string categoryKey = _category switch
            {
                FoodyItemType.Antennas => "FOODY_CATEGORY_ANTENNAS",
                FoodyItemType.Glasses => "FOODY_CATEGORY_GLASSES",
                FoodyItemType.Ears => "FOODY_CATEGORY_EARS",
                _ => _category
            };

            if (_categoryHeading != null)
            {
                _categoryHeading.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", categoryKey);
            }

            this.style.position = Position.Absolute;
            this.style.bottom = 0;
            this.style.left = 0;
            this.style.right = 0;

            PrepareButtons();
            RefreshSelectionVisuals();
        }

        private void CacheUIElements()
        {
            _selectorItemNutri = contentContainer.Q<VisualElement>("SelectorItemNutri");
            _btLeftParts = contentContainer.Q<Unity.AppUI.UI.Button>("btLeftParts");
            _btRightParts = contentContainer.Q<Unity.AppUI.UI.Button>("btRightParts");
            _scrollParts = contentContainer.Q<ScrollView>("scrollParts");
            _btConfirm = contentContainer.Q<Unity.AppUI.UI.Button>("btConfirm");
            _btCancel = contentContainer.Q<Unity.AppUI.UI.Button>("btCancel");
            _categoryHeading = contentContainer.Q<Unity.AppUI.UI.Heading>("category-heading");
        }

        private void RegisterManualEvents()
        {
            if (_btLeftParts != null) _btLeftParts.clicked += OnLeftPartsClicked;
            if (_btRightParts != null) _btRightParts.clicked += OnRightPartsClicked;
            if (_btConfirm != null) _btConfirm.clicked += OnConfirmClicked;
            if (_btCancel != null) _btCancel.clicked += OnCancelClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btLeftParts != null) _btLeftParts.clicked -= OnLeftPartsClicked;
            if (_btRightParts != null) _btRightParts.clicked -= OnRightPartsClicked;
            if (_btConfirm != null) _btConfirm.clicked -= OnConfirmClicked;
            if (_btCancel != null) _btCancel.clicked -= OnCancelClicked;
        }

        public void Dispose()
        {
            _isClosing = true;
            UnregisterManualEvents();
        }

        public void AnimateIn()
        {
            if (_selectorItemNutri == null) return;
            _selectorItemNutri.RemoveFromClassList("fm-nutri-panel-anim--visible");
            _selectorItemNutri.RemoveFromClassList("fm-nutri-panel-anim--exit");
            _selectorItemNutri.AddToClassList("fm-nutri-panel-anim");

            _selectorItemNutri.schedule.Execute(() =>
            {
                _selectorItemNutri.AddToClassList("fm-nutri-panel-anim--visible");
            }).StartingIn(16);
        }

        public void AnimateOut(Action onComplete)
        {
            if (_selectorItemNutri == null)
            {
                onComplete?.Invoke();
                return;
            }

            _selectorItemNutri.RemoveFromClassList("fm-nutri-panel-anim--visible");
            _selectorItemNutri.AddToClassList("fm-nutri-panel-anim--exit");

            _selectorItemNutri.schedule.Execute(() =>
            {
                onComplete?.Invoke();
            }).StartingIn(220);
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

        private void OnConfirmClicked()
        {
            if (_isClosing) return;
            _isClosing = true;

            var saveTask = _viewModel != null ? _viewModel.SaveAsync() : System.Threading.Tasks.Task.CompletedTask;
            AnimateOut(async () =>
            {
                try
                {
                    await saveTask;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Error saving loadout on confirm: {ex.Message}");
                }
                _onClose?.Invoke();
            });
        }

        private void OnCancelClicked()
        {
            if (_isClosing) return;
            _isClosing = true;
            if (_viewModel != null)
            {
                if (_initialSlot > 0)
                {
                    _viewModel.EquipItem(_category, _initialSlot);
                }
                else
                {
                    _viewModel.UnequipCategory(_category);
                }
            }
            AnimateOut(() => _onClose?.Invoke());
        }

        private void PrepareButtons()
        {
            _scrollParts?.Clear();
            _buttonEntries.Clear();

            // 1. "None" / Unequip Option (slot 0)
            var noneBtn = new Unity.AppUI.UI.Button
            {
                leadingIcon = "fm-item-none",
                variant = Unity.AppUI.UI.ButtonVariant.Accent
            };
            noneBtn.AddToClassList("fm-button-avatar-scroll");
            noneBtn.clicked += () => OnPartSelected(0, null);
            _scrollParts?.Add(noneBtn);
            _buttonEntries.Add((0, noneBtn, null));

            // 2. Category accessory items (slots 1..6)
            var items = _viewModel?.GetItemsForCategory(_category) ?? new List<FoodyItem>();
            string normCategory = _category.ToLowerInvariant();

            foreach (var item in items)
            {
                var btn = new Unity.AppUI.UI.Button
                {
                    leadingIcon = $"fm-icon-{normCategory}-{item.slot}",
                    variant = Unity.AppUI.UI.ButtonVariant.Accent
                };
                btn.AddToClassList("fm-button-avatar-scroll");

                VisualElement badge = null;
                if (!item.owned)
                {
                    badge = CreateCostBadge(item.cost);
                    btn.Add(badge);
                }

                var currentItem = item;
                btn.clicked += () => OnPartSelected(currentItem.slot, currentItem);

                _scrollParts?.Add(btn);
                _buttonEntries.Add((item.slot, btn, badge));
            }
        }

        private VisualElement CreateCostBadge(int cost)
        {
            var badge = new VisualElement();
            badge.AddToClassList("fm-nutri-cost-badge");

            var coinIcon = new Unity.AppUI.UI.Icon
            {
                iconName = "fm-icon-coin"
            };
            coinIcon.AddToClassList("fm-nutri-cost-badge-image-coin");
            badge.Add(coinIcon);

            var costLabel = new Label(cost.ToString());
            costLabel.AddToClassList("fm-nutri-cost-badge__text");
            coinIcon.Add(costLabel);

            return badge;
        }

        private void OnPartSelected(int slot, FoodyItem item)
        {
            if (slot == 0)
            {
                _currentSelectedSlot = 0;
                _viewModel?.UnequipCategory(_category);
                RefreshSelectionVisuals();
                return;
            }

            if (item == null) return;

            if (item.owned)
            {
                _currentSelectedSlot = slot;
                _viewModel?.EquipItem(_category, slot);
                RefreshSelectionVisuals();
            }
            else
            {
                // Item requires points purchase
                int userPoints = _viewModel?.UserPoints ?? 0;
                if (userPoints < item.cost)
                {
                    string notEnoughMsg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FOODY_INSUFFICIENT_POINTS");
                    FMDialog.ShowAlert(
                        _dialogAnchor,
                        "@UI:FOODY_BUY_CONFIRM_TITLE",
                        notEnoughMsg
                    );
                }
                else
                {
                    string confirmMsg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FOODY_BUY_CONFIRM_MSG", new object[] { item.cost });

                    FMDialog.ShowConfirm(
                        _dialogAnchor,
                        "@UI:FOODY_BUY_CONFIRM_TITLE",
                        confirmMsg,
                        onConfirm: async () =>
                        {
                            bool success = await _viewModel.PurchaseItemAsync(item);
                            if (success)
                            {
                                // Remove cost badge from button
                                var entry = _buttonEntries.Find(e => e.slot == slot);
                                if (entry.costBadge != null && entry.button != null)
                                {
                                    entry.button.Remove(entry.costBadge);
                                }

                                _currentSelectedSlot = slot;
                                _viewModel?.EquipItem(_category, slot);
                                RefreshSelectionVisuals();
                            }
                        }
                    );
                }
            }
        }

        private void RefreshSelectionVisuals()
        {
            foreach (var (slot, button, _) in _buttonEntries)
            {
                bool isSelected = slot == _currentSelectedSlot;
                button.EnableInClassList("option-selected", isSelected);
            }
        }
    }
}
