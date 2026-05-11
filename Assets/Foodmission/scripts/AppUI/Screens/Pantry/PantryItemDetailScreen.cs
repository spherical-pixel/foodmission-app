using System;
using System.Collections.Generic;
using System.ComponentModel;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [Preserve]
    class PantryItemDetailScreen : NavigationScreenBase<PantryItemDetailViewModel>
    {
        private Heading _itemTitle;
        private Text _errorText;
        private Text _nameLabel;
        private Unity.AppUI.UI.FloatField _quantityField;
        private Dropdown _unitDropdown;
        private Dropdown _locationDropdown;
        private Unity.AppUI.UI.TextField _notesField;
        private Unity.AppUI.UI.TextField _expiryField;
        private Unity.AppUI.UI.Button _btnSave;
        private Unity.AppUI.UI.Button _btnDelete;

        private static readonly List<string> UnitValues = new() { "PIECES", "G", "KG", "ML", "L", "CUPS" };

        private static List<string> _unitChoices;
        private static List<string> UnitChoices
        {
            get
            {
                if (_unitChoices == null)
                {
                    _unitChoices = new List<string>
                    {
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_PIECES"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_G"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_KG"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_ML"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_L"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "UNIT_CUPS"),
                    };
                }
                return _unitChoices;
            }
        }
        private static readonly List<string> LocationValues = new() { "", "Fridge", "Freezer", "Pantry shelf", "Countertop", "Cupboard" };

        private static List<string> _locationChoices;
        private static List<string> LocationChoices
        {
            get
            {
                if (_locationChoices == null)
                {
                    _locationChoices = new List<string>
                    {
                        "",
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOC_FRIDGE"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOC_FREEZER"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOC_PANTRY_SHELF"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOC_COUNTERTOP"),
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LOC_CUPBOARD"),
                    };
                }
                return _locationChoices;
            }
        }

        public PantryItemDetailScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.PantryItemDetail));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _itemTitle = contentContainer.Q<Heading>("item-title");
            _errorText = contentContainer.Q<Text>("error-message");
            _nameLabel = contentContainer.Q<Text>("lbl-name");
            _quantityField = contentContainer.Q<Unity.AppUI.UI.FloatField>("quantity-field");
            _unitDropdown = contentContainer.Q<Dropdown>("unit-field");
            _locationDropdown = contentContainer.Q<Dropdown>("location-field");
            _notesField = contentContainer.Q<Unity.AppUI.UI.TextField>("notes-field");
            _expiryField = contentContainer.Q<Unity.AppUI.UI.TextField>("expiry-field");
            _btnSave = contentContainer.Q<Unity.AppUI.UI.Button>("btn-save");
            _btnDelete = contentContainer.Q<Unity.AppUI.UI.Button>("btn-delete");
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            string itemId = null;

            if (args != null)
            {
                foreach (Argument arg in args)
                {
                    if (arg.name == "itemId")
                    {
                        itemId = arg.value?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(itemId))
            {
                await _viewModel.LoadAsync(itemId);
            }
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _unitDropdown.sourceItems = UnitChoices;
            _unitDropdown.bindItem = (item, i) => item.label = UnitChoices[i];
            _locationDropdown.sourceItems = LocationChoices;
            _locationDropdown.bindItem = (item, i) => item.label = LocationChoices[i];

            _btnSave.clicked += OnSaveClicked;
            _btnDelete.clicked += OnDeleteClicked;

            SetupExpiryQuickButtons();

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateItemTitle();
            UpdateLoadingState();
            UpdateErrorState();
        }

        protected override void OnViewModelUnbinding()
        {
            _btnSave.clicked -= OnSaveClicked;
            _btnDelete.clicked -= OnDeleteClicked;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnViewModelUnbinding();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.ItemView):
                    UpdateItemTitle();
                    UpdateFormFields();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.IsSaving):
                    bool saving = _viewModel.IsSaving;
                    _btnSave.SetEnabled(!saving);
                    if (saving)
                        FMLoadingOverlay.Show(contentContainer);
                    else
                        FMLoadingOverlay.Hide(contentContainer);
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
            }
        }

        private void UpdateItemTitle()
        {
            if (_itemTitle != null && _viewModel.ItemView != null)
            {
                _itemTitle.text = _viewModel.ItemView.DisplayName;
            }

            if (_nameLabel != null && _viewModel.ItemView != null)
            {
                _nameLabel.text = _viewModel.ItemView.DisplayName;
            }
        }

        private void UpdateFormFields()
        {
            if (_viewModel.ItemView == null) return;

            _quantityField.SetValueWithoutNotify(_viewModel.Quantity);

            int unitIdx = UnitValues.IndexOf(_viewModel.Unit);
            _unitDropdown.SetValueWithoutNotify(unitIdx >= 0 ? new[] { unitIdx } : new int[0]);

            int locIdx = LocationValues.IndexOf(_viewModel.Location);
            _locationDropdown.SetValueWithoutNotify(locIdx >= 0 ? new[] { locIdx } : new int[0]);

            _notesField.SetValueWithoutNotify(_viewModel.Notes);
            _expiryField.SetValueWithoutNotify(_viewModel.ExpiryDate);
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
            _btnSave?.SetEnabled(!isLoading);
            _btnDelete?.SetEnabled(!isLoading);
        }

        private void UpdateErrorState()
        {
            bool hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
            _errorText?.EnableInClassList("visible", hasError);
            if (_errorText != null)
            {
                _errorText.text = _viewModel.ErrorMessage;
            }
        }

        private void SetupExpiryQuickButtons()
        {
            if (_expiryField == null) return;

            VisualElement parent = _expiryField.parent;
            if (parent == null) return;

            var quickRow = new VisualElement();
            quickRow.AddToClassList("fm-pid-expiry-quick");

            string[] labels = { "Today", "+3d", "+1w", "+1m" };
            int[] dayOffsets = { 0, 3, 7, 30 };

            for (int i = 0; i < labels.Length; i++)
            {
                string capturedLabel = labels[i];
                int capturedDays = dayOffsets[i];
                var btn = new Unity.AppUI.UI.Button
                {
                    title = capturedLabel,
                    size = Size.S
                };
                btn.AddToClassList("fm-pid-quick-btn");
                btn.clicked += () =>
                {
                    DateTime dt = DateTime.Now.Date.AddDays(capturedDays);
                    _expiryField.value = dt.ToString("yyyy-MM-dd");
                };
                quickRow.Add(btn);
            }

            parent.Add(quickRow);
        }

        private async void OnSaveClicked()
        {
            _viewModel.Quantity = _quantityField.value;
            _viewModel.Unit = UnitValues[_unitDropdown.selectedIndex];
            _viewModel.Location = LocationValues[_locationDropdown.selectedIndex];
            _viewModel.Notes = _notesField.value;
            _viewModel.ExpiryDate = _expiryField.value;

            await _viewModel.SaveAsync();
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private void OnDeleteClicked()
        {
            string displayName = _viewModel.ItemView?.DisplayName ?? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "THIS_ITEM");

            FMDialog.ShowConfirm(
                this,
                "@UI:DELETE_ITEM",
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { displayName }),
                onConfirm: async () => await _viewModel.DeleteAsync(),
                semantic: AlertSemantic.Destructive);
        }
    }
}
