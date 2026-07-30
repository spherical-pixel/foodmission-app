using System;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Accessibility;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class FoodWasteAddScreen : NavigationScreenBase<FoodWasteAddViewModel>
    {
        private Dropdown _pantryDropdown;
        private Dropdown _reasonDropdown;
        private Dropdown _methodDropdown;
        private Unity.AppUI.UI.FloatField _quantityField;
        private Unity.AppUI.UI.FloatField _costField;
        private Unity.AppUI.UI.TextField _notesField;
        private Text _maxQuantityLabel;
        private Text _foodNameLabel;
        private Unity.AppUI.UI.Button _btnSave;
        private Text _errorText;

        private AccessibilityNode _saveButtonNode;
        private AccessibilityNode _pantryDropdownNode;
        private AccessibilityNode _reasonDropdownNode;
        private AccessibilityNode _methodDropdownNode;
        private AccessibilityNode _quantityFieldNode;
        private AccessibilityNode _costFieldNode;
        private AccessibilityNode _notesFieldNode;

        private EventCallback<ChangeEvent<int>> _onPantryChanged;
        private EventCallback<ChangeEvent<int>> _onReasonChanged;
        private EventCallback<ChangeEvent<int>> _onMethodChanged;

        public FoodWasteAddScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.FoodWasteAdd));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _pantryDropdown = contentContainer.Q<Dropdown>("pantry-item-dropdown");
            _reasonDropdown = contentContainer.Q<Dropdown>("waste-reason-dropdown");
            _methodDropdown = contentContainer.Q<Dropdown>("detection-method-dropdown");
            _quantityField = contentContainer.Q<Unity.AppUI.UI.FloatField>("quantity-field");
            _maxQuantityLabel = contentContainer.Q<Text>("max-quantity-label");
            _costField = contentContainer.Q<Unity.AppUI.UI.FloatField>("cost-field");
            _notesField = contentContainer.Q<Unity.AppUI.UI.TextField>("notes-field");
            _foodNameLabel = contentContainer.Q<Text>("food-name-label");
            _btnSave = contentContainer.Q<Unity.AppUI.UI.Button>("btn-save");
            _errorText = contentContainer.Q<Text>("error-message");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _pantryDropdown.sourceItems = _viewModel.PantryItemOptions;
            _pantryDropdown.bindItem = (item, i) => item.label = _viewModel.PantryItemOptions[i];
            _onPantryChanged = evt => _viewModel.OnPantryItemSelected(evt.newValue);
            _pantryDropdown.RegisterCallback(_onPantryChanged);

            var reasonOptions = new System.Collections.Generic.List<string>
            {
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_EXPIRED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_SPOILED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_OVERCOOKED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_UNWANTED"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_PORTION_LARGE"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "REASON_OTHER"),
            };
            _reasonDropdown.sourceItems = reasonOptions;
            _reasonDropdown.bindItem = (item, i) => item.label = reasonOptions[i];
            _onReasonChanged = evt =>
            {
                if (evt.newValue >= 0)
                    _viewModel.WasteReason = WasteReason.All[evt.newValue];
            };
            _reasonDropdown.RegisterCallback(_onReasonChanged);

            var methodOptions = new System.Collections.Generic.List<string>
            {
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DETECT_AUTOMATIC"),
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "DETECT_MANUAL"),
            };
            _methodDropdown.sourceItems = methodOptions;
            _methodDropdown.bindItem = (item, i) => item.label = methodOptions[i];
            _onMethodChanged = evt =>
            {
                if (evt.newValue >= 0)
                    _viewModel.DetectionMethod = DetectionMethod.All[evt.newValue];
            };
            _methodDropdown.RegisterCallback(_onMethodChanged);

            _quantityField.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                _viewModel.Quantity = evt.newValue;
            });

            _costField.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                _viewModel.CostEstimate = evt.newValue > 0 ? evt.newValue.ToString("F2") : "";
            });

            _btnSave.clicked += OnSaveClicked;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            UpdateFoodName();
            UpdateMaxQuantity();
            UpdateLoadingState();
            UpdateErrorState();

            _ = _viewModel.LoadPantryItemsAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadPantryItemsAsync failed: {t.Exception}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            if (_onPantryChanged != null)
                _pantryDropdown.UnregisterCallback(_onPantryChanged);
            if (_onReasonChanged != null)
                _reasonDropdown.UnregisterCallback(_onReasonChanged);
            if (_onMethodChanged != null)
                _methodDropdown.UnregisterCallback(_onMethodChanged);
            _btnSave.clicked -= OnSaveClicked;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
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

            _saveButtonNode = CreateButtonNode(h, _btnSave, "Save food waste record");

            if (_pantryDropdown != null)
            {
                _pantryDropdownNode = h.AddNode("Pantry item");
                _pantryDropdownNode.role = AccessibilityRole.Dropdown;
                _pantryDropdownNode.frameGetter = MakeElementFrameGetter(_pantryDropdown);
            }

            if (_reasonDropdown != null)
            {
                _reasonDropdownNode = h.AddNode("Waste reason");
                _reasonDropdownNode.role = AccessibilityRole.Dropdown;
                _reasonDropdownNode.frameGetter = MakeElementFrameGetter(_reasonDropdown);
            }

            if (_methodDropdown != null)
            {
                _methodDropdownNode = h.AddNode("Detection method");
                _methodDropdownNode.role = AccessibilityRole.Dropdown;
                _methodDropdownNode.frameGetter = MakeElementFrameGetter(_methodDropdown);
            }

            if (_quantityField != null)
            {
                _quantityFieldNode = h.AddNode("Quantity");
                _quantityFieldNode.role = AccessibilityRole.TextField;
                _quantityFieldNode.frameGetter = MakeElementFrameGetter(_quantityField);
            }

            if (_costField != null)
            {
                _costFieldNode = h.AddNode("Cost estimate");
                _costFieldNode.role = AccessibilityRole.TextField;
                _costFieldNode.frameGetter = MakeElementFrameGetter(_costField);
            }

            if (_notesField != null)
            {
                _notesFieldNode = h.AddNode("Notes");
                _notesFieldNode.role = AccessibilityRole.TextField;
                _notesFieldNode.frameGetter = MakeElementFrameGetter(_notesField);
            }
        }

        protected override void TeardownAccessibilityNodes()
        {
            _saveButtonNode = null;
            _pantryDropdownNode = null;
            _reasonDropdownNode = null;
            _methodDropdownNode = null;
            _quantityFieldNode = null;
            _costFieldNode = null;
            _notesFieldNode = null;
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

        private static Func<Rect> MakeElementFrameGetter(VisualElement element)
        {
            return () =>
            {
                if (element == null || element.panel == null) return Rect.zero;
                var r = element.worldBound;
                var s = element.panel.scaledPixelsPerPoint;
                return new Rect(r.position * s, r.size * s);
            };
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.PantryItemOptions):
                    _pantryDropdown.sourceItems = _viewModel.PantryItemOptions;
                    _pantryDropdown.bindItem = (item, i) => item.label = _viewModel.PantryItemOptions[i];
                    break;
                case nameof(_viewModel.SelectedFoodName):
                    UpdateFoodName();
                    break;
                case nameof(_viewModel.MaxQuantity):
                    UpdateMaxQuantity();
                    break;
                case nameof(_viewModel.Quantity):
                    if (_quantityField != null)
                        _quantityField.SetValueWithoutNotify(_viewModel.Quantity);
                    break;
                case nameof(_viewModel.IsLoading):
                case nameof(_viewModel.IsSaving):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private async void OnSaveClicked()
        {
            try
            {
                bool success = await _viewModel.SaveAsync();
                if (success)
                {
                    _viewModel.Reset();
                    _navController?.PopBackStack();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Save failed: {ex.Message}");
                if (_errorText != null)
                {
                    _errorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNEXPECTED_ERROR_OCCURRED");
                    _errorText.EnableInClassList("visible", true);
                }
            }
        }

        private void UpdateFoodName()
        {
            if (_foodNameLabel != null)
                _foodNameLabel.text = _viewModel.SelectedFoodName;
        }

        private void UpdateMaxQuantity()
        {
            if (_maxQuantityLabel != null)
                _maxQuantityLabel.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "MAX_QTY_LABEL", new object[] { _viewModel.MaxQuantity });
        }

        private void UpdateLoadingState()
        {
            bool busy = _viewModel.IsLoading || _viewModel.IsSaving;
            if (busy)
                FMLoadingOverlay.Show();
            else
                FMLoadingOverlay.Hide();
            _btnSave?.SetEnabled(!busy);
            _pantryDropdown?.SetEnabled(!busy);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);
            if (_errorText != null)
                _errorText.text = _viewModel.ErrorMessage;
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }
    }
}
