using System;
using System.ComponentModel;
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
    public static class PantryItemDetailOverlay
    {
        private static VisualElement _overlay;
        private static PantryItemDetailViewModel _viewModel;
        private static Action _onSavedCallback;
        private static Action _onDeletedCallback;

        // UI references
        private static Heading _overlayTitle;
        private static Unity.AppUI.UI.Text _errorText;
        private static Unity.AppUI.UI.FloatField _quantityField;
        private static Dropdown _unitDropdown;
        private static DateField _expiryDateField;
        private static VisualElement _quickDateRow;
        private static Unity.AppUI.UI.TextArea _notesField;
        private static Unity.AppUI.UI.Button _btnSave;
        private static Unity.AppUI.UI.Button _btnDelete;

        public static void Show(
            VisualElement anchor,
            string itemId,
            Action onSaved = null,
            Action onDeleted = null)
        {
            Dismiss();

            var root = anchor?.panel?.visualTree;
            if (root == null)
            {
                Debug.LogError("[PantryItemDetailOverlay] Cannot find panel root to attach overlay.");
                return;
            }

            VisualElement container = root.Q<Unity.AppUI.UI.Panel>() ?? root;

            _onSavedCallback = onSaved;
            _onDeletedCallback = onDeleted;

            // Resolve ViewModel & Services
            _viewModel = App.current.services.GetRequiredService<PantryItemDetailViewModel>();
            var themeService = App.current.services.GetRequiredService<IThemeService>();
            var templateService = App.current.services.GetRequiredService<ITemplateService>();

            VisualTreeAsset template = templateService?.Get(TemplateAddresses.PantryItemDetail);

            if (template == null)
            {
#if UNITY_EDITOR
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Foodmission/scripts/AppUI/Screens/Pantry/PantryItemDetailScreen.uxml");
#endif
            }

            if (template == null)
            {
                Debug.LogError("[PantryItemDetailOverlay] PantryItemDetailScreen.uxml template not found.");
                _viewModel.Dispose();
                _viewModel = null;
                return;
            }

            // ── 1. Build Overlay Base Container ─────────────────────────
            _overlay = new VisualElement();
            _overlay.name = "pantry-item-detail-overlay";
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.pickingMode = PickingMode.Position;
            _overlay.AddToClassList("fm-pantry-item-detail-overlay");

            // ── 2. AppBar (Matching FoodInfoOverlay) ───────────────────
            var appBar = new VisualElement();
            appBar.AddToClassList("fm-pantry-item-detail-overlay__appbar");

            // Apply safe area top padding to the appbar
            themeService?.ApplySafeAreaPadding(appBar, true, false, false, false);

            var backBtn = new IconButton { icon = "arrow-left", quiet = true };
            backBtn.AddToClassList("fm-pantry-item-detail-overlay__back-btn");
            backBtn.clicked += Dismiss;
            appBar.Add(backBtn);

            _overlayTitle = new Heading();
            _overlayTitle.name = "overlay-title";
            _overlayTitle.size = HeadingSize.M;
            _overlayTitle.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ITEM_DETAILS") ?? "Detalles del producto";
            _overlayTitle.AddToClassList("fm-pantry-item-detail-overlay__title");
            appBar.Add(_overlayTitle);

            _overlay.Add(appBar);

            // ── 3. Scrollable Form Content ──────────────────────────────
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            scrollView.contentContainer.style.backgroundColor = new StyleColor(UnityEngine.Color.clear);
            _overlay.Add(scrollView);

            var content = new VisualElement();
            content.style.flexGrow = 1;
            template.CloneTree(content);
            scrollView.Add(content);

            // ── 4. Cache Element References ─────────────────────────────
            CacheUIElements(content);
            BindUIEvents();

            container.Add(_overlay);

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (!string.IsNullOrEmpty(itemId))
            {
                _ = _viewModel.LoadAsync(itemId).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"[PantryItemDetailOverlay] LoadAsync failed: {t.Exception}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            UpdateLoadingState();
        }

        public static void Dismiss()
        {
            if (_overlay == null) return;

            if (_btnSave != null)
            {
                _btnSave.clicked -= OnSaveClicked;
            }

            if (_btnDelete != null)
            {
                _btnDelete.clicked -= OnDeleteClicked;
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.Dispose();
                _viewModel = null;
            }

            if (_overlay.parent != null)
            {
                _overlay.RemoveFromHierarchy();
            }
            _overlay = null;

            _overlayTitle = null;
            _errorText = null;
            _quantityField = null;
            _unitDropdown = null;
            _expiryDateField = null;
            _quickDateRow = null;
            _notesField = null;
            _btnSave = null;
            _btnDelete = null;
            _onSavedCallback = null;
            _onDeletedCallback = null;
        }

        private static void CacheUIElements(VisualElement root)
        {
            _errorText = root.Q<Unity.AppUI.UI.Text>("error-message");
            _quantityField = root.Q<Unity.AppUI.UI.FloatField>("quantity-field");
            _unitDropdown = root.Q<Dropdown>("unit-field");
            _expiryDateField = root.Q<DateField>("expiry-date-field");
            _quickDateRow = root.Q<VisualElement>("quick-date-row");
            _notesField = root.Q<Unity.AppUI.UI.TextArea>("notes-field");
            _btnSave = root.Q<Unity.AppUI.UI.Button>("btn-save");
            _btnDelete = root.Q<Unity.AppUI.UI.Button>("btn-delete");

            if (_unitDropdown != null)
            {
                _unitDropdown.sourceItems = FMQuantityUnitPanel.UnitChoices;
                _unitDropdown.bindItem = (item, i) => item.label = FMQuantityUnitPanel.UnitChoices[i];
            }

            SetupQuickDateButtons();
        }

        private static void BindUIEvents()
        {
            if (_btnSave != null)
            {
                _btnSave.clicked += OnSaveClicked;
            }

            if (_btnDelete != null)
            {
                _btnDelete.clicked += OnDeleteClicked;
            }
        }

        private static void PopulateUI()
        {
            if (_viewModel?.ItemView == null) return;

            if (_overlayTitle != null && !string.IsNullOrEmpty(_viewModel.ItemView.DisplayName))
            {
                _overlayTitle.text = _viewModel.ItemView.DisplayName;
            }

            if (_quantityField != null)
            {
                _quantityField.SetValueWithoutNotify(_viewModel.Quantity);
            }

            if (_unitDropdown != null)
            {
                int unitIdx = FMQuantityUnitPanel.UnitValues.IndexOf(_viewModel.Unit);
                _unitDropdown.SetValueWithoutNotify(unitIdx >= 0 ? new[] { unitIdx } : new int[0]);
            }

            if (_notesField != null)
            {
                _notesField.SetValueWithoutNotify(_viewModel.Notes ?? "");
            }

            if (_expiryDateField != null)
            {
                if (DateTime.TryParse(_viewModel.ExpiryDate, out DateTime dt))
                {
                    _expiryDateField.SetValueWithoutNotify(new Date(dt));
                }
                else
                {
                    _expiryDateField.SetValueWithoutNotify(new Date(DateTime.Now.Date.AddDays(30)));
                }
            }
        }

        private static void UpdateLoadingState()
        {
            if (_overlay == null || _viewModel == null) return;
            if (_viewModel.IsLoading || _viewModel.IsSaving)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
            _btnSave?.SetEnabled(!_viewModel.IsLoading && !_viewModel.IsSaving);
            _btnDelete?.SetEnabled(!_viewModel.IsLoading && !_viewModel.IsSaving);
        }

        private static void SetupQuickDateButtons()
        {
            if (_quickDateRow == null) return;
            _quickDateRow.Clear();

            string[] keys = { "TODAY", "DATE_PLUS_3_DAYS", "DATE_PLUS_1_WEEK", "DATE_PLUS_1_MONTH" };
            int[] dayOffsets = { 0, 3, 7, 30 };

            for (int i = 0; i < keys.Length; i++)
            {
                string capturedKey = keys[i];
                int capturedDays = dayOffsets[i];

                string label = LocalizationSettings.StringDatabase.GetLocalizedString("UI", capturedKey);
                if (string.IsNullOrEmpty(label) || label == capturedKey) continue;

                var btn = new Unity.AppUI.UI.Button
                {
                    title = label,
                    size = Size.S
                };
                btn.AddToClassList("fm-pido-quick-btn");
                btn.clicked += () =>
                {
                    DateTime targetDate = DateTime.Now.Date.AddDays(capturedDays);
                    if (_expiryDateField != null)
                    {
                        _expiryDateField.value = new Date(targetDate);
                    }
                };

                _quickDateRow.Add(btn);
            }
        }

        private static async void OnSaveClicked()
        {
            if (_viewModel == null) return;

            try
            {
                if (_quantityField != null)
                {
                    _viewModel.Quantity = _quantityField.value;
                }

                if (_unitDropdown != null && _unitDropdown.selectedIndex >= 0 && _unitDropdown.selectedIndex < FMQuantityUnitPanel.UnitValues.Count)
                {
                    _viewModel.Unit = FMQuantityUnitPanel.UnitValues[_unitDropdown.selectedIndex];
                }

                if (_notesField != null)
                {
                    _viewModel.Notes = _notesField.value;
                }

                _viewModel.Location = "";

                if (_expiryDateField != null)
                {
                    var dateValue = _expiryDateField.value;
                    _viewModel.ExpiryDate = ((DateTime)dateValue).ToString("yyyy-MM-dd");
                }

                await _viewModel.SaveAsync();

                if (string.IsNullOrEmpty(_viewModel.ErrorMessage) && _viewModel.ErrorDetail == null)
                {
                    Action cb = _onSavedCallback;
                    Dismiss();
                    cb?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PantryItemDetailOverlay] OnSaveClicked failed: {ex}");
            }
        }

        private static void OnDeleteClicked()
        {
            if (_viewModel == null) return;

            string displayName = _viewModel.ItemView?.DisplayName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "THIS_ITEM");
            string message = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY_DELETE_OPTIONS_MSG", new object[] { displayName });
            if (string.IsNullOrEmpty(message))
            {
                message = $"¿Qué ocurrió con {displayName}?";
            }

            string eatenLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY_ACTION_EATEN") ?? "Registrar como comido";
            string wasteLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY_ACTION_WASTE") ?? "Registrar como desperdicio";
            string deleteLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "PANTRY_ACTION_DELETE") ?? "Solo eliminar de la despensa";
            string cancelLabel = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_CANCEL") ?? "Cancelar";

            NutriMessageDialog.Show(
                message,
                new FMDialogAction(eatenLabel, async () =>
                {
                    if (_viewModel == null) return;
                    bool ok = await _viewModel.ConsumeAsync();
                    if (ok)
                    {
                        Action cb = _onDeletedCallback;
                        Dismiss();
                        cb?.Invoke();
                    }
                }, isPrimary: true),
                new FMDialogAction(wasteLabel, () =>
                {
                    if (_viewModel?.ItemView == null) return;
                    VisualElement anchor = _overlay;
                    PantryItemView view = _viewModel.ItemView;
                    FoodWasteRecordOverlay.Show(
                        anchor,
                        view,
                        onSaved: () =>
                        {
                            Action cb = _onDeletedCallback;
                            Dismiss();
                            cb?.Invoke();
                        });
                }),
                new FMDialogAction(deleteLabel, () =>
                {
                    if (_viewModel == null || _overlay == null) return;
                    VisualElement anchorElement = _overlay;
                    FMDialog.ShowConfirm(
                        anchorElement,
                        "@UI:DELETE_ITEM",
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { displayName }),
                        onConfirm: async () =>
                        {
                            if (_viewModel == null) return;
                            await _viewModel.DeleteAsync();
                            Action cb = _onDeletedCallback;
                            Dismiss();
                            cb?.Invoke();
                        },
                        semantic: AlertSemantic.Destructive);
                }),
                new FMDialogAction(cancelLabel, () => { }));
        }

        private static void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = _viewModel;
            if (vm == null || _overlay == null) return;

            switch (e.PropertyName)
            {
                case nameof(vm.ItemView):
                    PopulateUI();
                    break;
                case nameof(vm.IsLoading):
                case nameof(vm.IsSaving):
                    UpdateLoadingState();
                    break;
                case nameof(vm.ErrorMessage):
                    if (_errorText != null)
                    {
                        bool hasError = !string.IsNullOrEmpty(vm.ErrorMessage);
                        _errorText.EnableInClassList("visible", hasError);
                        _errorText.text = vm.ErrorMessage;
                    }
                    break;
                case nameof(vm.ErrorDetail):
                    if (vm.ErrorDetail != null && _overlay != null)
                    {
                        FMDialog.ShowApiError(_overlay, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), vm.ErrorDetail);
                        vm.ErrorDetail = null;
                    }
                    break;
            }
        }
    }
}
