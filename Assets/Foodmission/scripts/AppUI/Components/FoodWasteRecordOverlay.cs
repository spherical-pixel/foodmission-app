using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using eu.foodmission.platform.Components;

using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Fullscreen modal overlay (built with AppUI's Modal) to record food waste from a pantry item
    /// with detailed options: waste reason, quantity wasted, optional estimated cost, and optional notes.
    /// </summary>
    public static class FoodWasteRecordOverlay
    {
        private static VisualElement _overlay;
        private static Modal _modal;
        private static Action _onSavedCallback;
        private static Action _onCancelledCallback;

        private static PantryItemView _currentItemView;
        private static IFoodWasteService _foodWasteService;
        private static INotificationService _notificationService;
        private static IPantryService _pantryService;

        // UI references
        private static Dropdown _reasonDropdown;
        private static Unity.AppUI.UI.FloatField _quantityField;
        private static Unity.AppUI.UI.FloatField _costField;
        private static Unity.AppUI.UI.TextArea _notesField;
        private static Unity.AppUI.UI.Text _errorText;
        private static FMButton _btnSave;
        private static FMButton _btnCancel;

        private static readonly string[] WasteReasonKeys = new[]
        {
            WasteReason.Expired,
            WasteReason.Spoiled,
            WasteReason.PortionTooLarge,
            WasteReason.Overcooked,
            WasteReason.Unwanted,
            WasteReason.Other
        };

        public static void Show(
            VisualElement anchor,
            PantryItemView itemView,
            Action onSaved = null,
            Action onCancelled = null)
        {
            Dismiss();

            if (anchor == null || itemView?.Item == null)
            {
                Debug.LogWarning("[FoodWasteRecordOverlay] Anchor, ItemView or Item is null.");
                onCancelled?.Invoke();
                return;
            }

            _currentItemView = itemView;
            _onSavedCallback = onSaved;
            _onCancelledCallback = onCancelled;

            // Resolve Services
            _foodWasteService = App.current.services.GetService<IFoodWasteService>();
            _pantryService = App.current.services.GetService<IPantryService>();
            _notificationService = App.current.services.GetService<INotificationService>();
            var themeService = App.current.services.GetService<IThemeService>();

            // ── 1. Base Overlay Container ──────────────────────────────
            _overlay = new VisualElement
            {
                name = "food-waste-record-overlay"
            };
            _overlay.AddToClassList("fm-food-waste-record-overlay");


            // ── 2. Top AppBar ──────────────────────────────────────────
            var appBar = new VisualElement();
            appBar.AddToClassList("fm-food-waste-record-overlay__appbar");

            themeService?.ApplySafeAreaPadding(appBar, true, false, false, false);

            var backBtn = new IconButton { icon = "arrow-left", quiet = true };
            backBtn.AddToClassList("fm-food-waste-record-overlay__back-btn");
            backBtn.clicked += () =>
            {
                Dismiss();
                _onCancelledCallback?.Invoke();
            };
            appBar.Add(backBtn);

            string titleStr = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY_ACTION_WASTE");
            var titleHeading = new Heading { text = titleStr, size = HeadingSize.M };
            titleHeading.AddToClassList("fm-food-waste-record-overlay__title");
            appBar.Add(titleHeading);

            _overlay.Add(appBar);

            // ── 3. Scrollable Content ──────────────────────────────────
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.AddToClassList("fm-food-waste-record-overlay__content");

            // Summary Card
            var summaryCard = new VisualElement();
            summaryCard.AddToClassList("fm-food-waste-record-overlay__card");

            var cardHeaderRow = new VisualElement();
            cardHeaderRow.AddToClassList("fm-food-waste-record-overlay__card-header");

            var emojiLabel = new Unity.AppUI.UI.Text { text = "🗑️" };
            emojiLabel.AddToClassList("fm-food-waste-record-overlay__emoji");
            cardHeaderRow.Add(emojiLabel);

            var infoCol = new VisualElement();
            infoCol.style.flexGrow = 1;

            var nameHeading = new Heading { text = itemView.DisplayName, size = HeadingSize.S };
            nameHeading.AddToClassList("fm-food-waste-record-overlay__food-name");
            infoCol.Add(nameHeading);

            string qtyInfo = $"{itemView.Item.quantity} {itemView.Item.unit}";
            var qtySub = new Unity.AppUI.UI.Text { text = qtyInfo };
            qtySub.AddToClassList("fm-food-waste-record-overlay__food-qty");
            infoCol.Add(qtySub);

            cardHeaderRow.Add(infoCol);
            summaryCard.Add(cardHeaderRow);
            scroll.Add(summaryCard);

            // Error text
            _errorText = new Unity.AppUI.UI.Text();
            _errorText.AddToClassList("fm-food-waste-record-overlay__error");
            _errorText.style.display = DisplayStyle.None;
            scroll.Add(_errorText);

            // ── Field 1: Motivo del Desperdicio (Waste Reason) ─────────
            var reasonSection = CreateFormSection(
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "WASTE_REASON_LABEL"));

            var reasonLabels = new List<string>
            {
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_EXPIRED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_SPOILED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_PORTION_LARGE"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_OVERCOOKED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_UNWANTED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_OTHER")
            };

            _reasonDropdown = new Dropdown();
            _reasonDropdown.sourceItems = reasonLabels;
            _reasonDropdown.bindItem = (item, i) => item.label = reasonLabels[i];
            _reasonDropdown.selectedIndex = 0;
            _reasonDropdown.style.width = Length.Percent(100);
            reasonSection.Add(_reasonDropdown);
            scroll.Add(reasonSection);

            // ── Field 2: Cantidad desperdiciada ─────────────────────────
            string qtyLabelStr = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QUANTITY_WASTED");
            var qtySection = CreateFormSection($"{qtyLabelStr} ({itemView.Item.unit}) *");

            float maxAvailable = itemView.Item.quantity;
            _quantityField = new Unity.AppUI.UI.FloatField();
            _quantityField.value = itemView.Item.quantity > 0 ? itemView.Item.quantity : 1;
            _quantityField.style.width = Length.Percent(100);
            _quantityField.RegisterValueChangedCallback(evt =>
            {
                float newQty = evt.newValue;
                if (newQty <= 0)
                {
                    ShowError(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QTY_MUST_BE_POSITIVE"));
                    _btnSave?.SetEnabled(false);
                }
                else if (maxAvailable > 0 && newQty > maxAvailable)
                {
                    string maxErr = string.Format(
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QTY_EXCEEDS_MAX"),
                        maxAvailable,
                        itemView.Item.unit);
                    ShowError(maxErr);
                    _btnSave?.SetEnabled(false);
                }
                else
                {
                    ShowError(null);
                    _btnSave?.SetEnabled(true);
                }
            });
            qtySection.Add(_quantityField);

            var maxHelp = new Unity.AppUI.UI.Text { text = $"Máximo: {itemView.Item.quantity} {itemView.Item.unit}" };
            maxHelp.AddToClassList("fm-food-waste-record-overlay__helper");
            qtySection.Add(maxHelp);
            scroll.Add(qtySection);

            // ── Field 3: Coste estimado (€, opcional) ───────────────────
            string costLabelStr = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FOOD_WASTE_COST_LABEL");
            var costSection = CreateFormSection(costLabelStr);

            _costField = new Unity.AppUI.UI.FloatField();
            _costField.value = 0f;
            _costField.style.width = Length.Percent(100);
            costSection.Add(_costField);
            scroll.Add(costSection);

            // ── Field 4: Notas adicionales (opcional) ───────────────────
            string notesLabelStr = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FOOD_WASTE_NOTES_LABEL");
            var notesSection = CreateFormSection(notesLabelStr);

            _notesField = new Unity.AppUI.UI.TextArea();
            _notesField.style.width = Length.Percent(100);
            _notesField.style.minHeight = 300;
            notesSection.Add(_notesField);
            scroll.Add(notesSection);

            _overlay.Add(scroll);

            // ── 4. Bottom Action Bar ───────────────────────────────────
            var bottomBar = new VisualElement();
            bottomBar.AddToClassList("fm-food-waste-record-overlay__bottombar");

            themeService?.ApplySafeAreaPadding(bottomBar, applyTop: false, applyBottom: true, applyLeft: false, applyRight: false);

            string saveBtnTitle = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SAVE");
            _btnSave = new FMButton { title = saveBtnTitle, variant = ButtonVariant.Accent, size = Size.L };
            _btnSave.AddToClassList("fm-food-waste-record-overlay__save-btn");
            _btnSave.clicked += async () => await OnSaveClicked(anchor);
            bottomBar.Add(_btnSave);

            string cancelBtnTitle = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_CANCEL");
            _btnCancel = new FMButton { title = cancelBtnTitle, variant = ButtonVariant.Default, size = Size.L };
            _btnCancel.AddToClassList("fm-food-waste-record-overlay__cancel-btn");
            _btnCancel.clicked += () =>
            {
                Dismiss();
                _onCancelledCallback?.Invoke();
            };
            bottomBar.Add(_btnCancel);

            bottomBar.Add(new Spacer() { spacing = SpacerSpacing.XL });

            _overlay.Add(bottomBar);

            // ── 5. Present via AppUI Modal ─────────────────────────────
            _modal = Modal.Build(anchor, _overlay);
            _modal.SetFullScreenMode(ModalFullScreenMode.FullScreenTakeOver);

            if (themeService != null)
            {
                void ApplySafeArea()
                {
                    themeService.ApplySafeAreaPadding(appBar, applyTop: true, applyBottom: false, applyLeft: false, applyRight: false);
                    themeService.ApplySafeAreaPadding(bottomBar, applyTop: false, applyBottom: true, applyLeft: false, applyRight: false);
                }
                themeService.SafeAreaChanged += ApplySafeArea;
                _modal.dismissed += (_, _) =>
                {
                    themeService.SafeAreaChanged -= ApplySafeArea;
                };
            }

            _modal.dismissed += (_, dismissType) =>
            {
                if (dismissType == DismissType.Manual)
                {
                    _onCancelledCallback?.Invoke();
                }

                _overlay = null;
                _modal = null;
                _currentItemView = null;
                _onSavedCallback = null;
                _onCancelledCallback = null;
                _foodWasteService = null;
                _pantryService = null;
                _notificationService = null;
            };

            _modal.Show();
        }

        private static VisualElement CreateFormSection(string labelText)
        {
            var section = new VisualElement();
            section.AddToClassList("fm-food-waste-record-overlay__section");

            var label = new Heading { text = labelText, size = HeadingSize.XXS };
            label.AddToClassList("fm-food-waste-record-overlay__label");
            section.Add(label);

            return section;
        }

        private static async Task OnSaveClicked(VisualElement anchor)
        {
            if (_currentItemView?.Item == null) return;

            float qty = _quantityField.value;
            if (qty <= 0)
            {
                ShowError(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QTY_MUST_BE_POSITIVE")
                    ?? "La cantidad debe ser mayor que 0.");
                return;
            }

            float maxAvailable = _currentItemView.Item.quantity;
            if (maxAvailable > 0 && qty > maxAvailable)
            {
                string maxErr = string.Format(
                    LocalizationSettings.StringDatabase.GetLocalizedString("UI", "QTY_EXCEEDS_MAX"),
                    maxAvailable,
                    _currentItemView.Item.unit);
                ShowError(maxErr);
                return;
            }

            int selectedReasonIdx = _reasonDropdown.selectedIndex >= 0 && _reasonDropdown.selectedIndex < WasteReasonKeys.Length
                ? _reasonDropdown.selectedIndex
                : 0;
            string selectedReason = WasteReasonKeys[selectedReasonIdx];

            float? cost = _costField.value > 0 ? _costField.value : null;
            string notes = !string.IsNullOrWhiteSpace(_notesField.value) ? _notesField.value.Trim() : null;

            SetBusy(true);

            try
            {
                if (_foodWasteService != null)
                {
                    var req = new CreateFoodWasteRequest
                    {
                        pantryItemId = _currentItemView.Item.id,
                        quantity = qty,
                        unit = _currentItemView.Item.unit,
                        wasteReason = selectedReason,
                        detectionMethod = DetectionMethod.Manual,
                        costEstimate = cost,
                        notes = notes,
                        wastedAt = DateTime.UtcNow.ToString("o")
                    };

                    var (created, wasteErr) = await _foodWasteService.CreateAsync(req);
                    if (wasteErr != null)
                    {
                        SetBusy(false);
                        string errStr = !string.IsNullOrEmpty(wasteErr.message)
                            ? wasteErr.message
                            : (LocalizationSettings.StringDatabase.GetLocalizedString("UI", "COULD_NOT_SAVE_WASTE"));
                        ShowError(errStr);
                        return;
                    }
                }
                else if (_pantryService != null)
                {
                    var (deleted, delErr) = await _pantryService.DeleteItemAsync(_currentItemView.Item.id);
                    if (delErr != null)
                    {
                        SetBusy(false);
                        ShowError(delErr.message ?? "Error al eliminar de la despensa");
                        return;
                    }
                }

                _notificationService?.CancelPantryReminder(_currentItemView.Item.id);

                Action cb = _onSavedCallback;
                Dismiss();
                cb?.Invoke();

                if (anchor != null)
                {
                    string toastMsg = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY_ITEM_WASTE_SUCCESS");
                    Toast.Build(anchor, toastMsg, NotificationDuration.Short)
                        .SetStyle(NotificationStyle.Positive)
                        .SetPosition(PopupNotificationPlacement.Bottom)
                        .Show();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoodWasteRecordOverlay] OnSaveClicked failed: {ex}");
                SetBusy(false);
                ShowError(ex.Message);
            }
        }

        private static void ShowError(string msg)
        {
            if (_errorText != null)
            {
                _errorText.text = msg;
                _errorText.style.display = string.IsNullOrEmpty(msg) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private static void SetBusy(bool busy)
        {
            if (_btnSave != null) _btnSave.SetEnabled(!busy);
            if (_btnCancel != null) _btnCancel.SetEnabled(!busy);
            if (_quantityField != null) _quantityField.SetEnabled(!busy);
            if (_costField != null) _costField.SetEnabled(!busy);
            if (_notesField != null) _notesField.SetEnabled(!busy);
            if (_reasonDropdown != null) _reasonDropdown.SetEnabled(!busy);
        }

        public static void Dismiss()
        {
            if (_modal != null)
            {
                _modal.Dismiss();
                _modal = null;
            }

            if (_overlay != null)
            {
                _overlay.RemoveFromHierarchy();
                _overlay = null;
            }

            _currentItemView = null;
            _onSavedCallback = null;
            _onCancelledCallback = null;
            _foodWasteService = null;
            _pantryService = null;
            _notificationService = null;
        }
    }
}

