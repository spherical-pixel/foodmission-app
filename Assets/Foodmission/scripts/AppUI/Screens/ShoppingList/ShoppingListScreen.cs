using System;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Accessibility;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Unity.AppUI.Core;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListScreen : NavigationScreenBase<ShoppingListViewModel>
    {

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => false;


        private FMSearchOrCreateField _searchOrCreateField;
        private VisualElement _listsContainer;
        private Unity.AppUI.UI.Text _emptyState;
        private UnityEngine.UIElements.TextField _searchField;
        
        

        public ShoppingListScreen()
        {
            InitializeComponent(App.current.services
                .GetRequiredService<ITemplateService>()
                .Get(TemplateAddresses.ShoppingList));
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _listsContainer = contentContainer.Q<VisualElement>("lists-container");
            _emptyState = contentContainer.Q<Unity.AppUI.UI.Text>("empty-state");
            _searchOrCreateField = contentContainer.Q<FMSearchOrCreateField>("search-or-create-field");
            _searchField = _searchOrCreateField.Q<UnityEngine.UIElements.TextField>();
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _searchOrCreateField.ActionButton.clicked += OnNewListClicked;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _searchField.RegisterValueChangedCallback(OnSearchTextChanged);

            RebuildLists();
            UpdateLoadingState();
            UpdateErrorState();

            _ = _viewModel.LoadListsAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[{GetType().Name}] LoadListsAsync failed: {t.Exception}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected override void OnViewModelUnbinding()
        {
            _searchOrCreateField.ActionButton.clicked -= OnNewListClicked;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _searchField.UnregisterValueChangedCallback(OnSearchTextChanged);
            base.OnViewModelUnbinding();
        }


        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Lists):
                    RebuildLists();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.ErrorMessage):
                    UpdateErrorState();
                    break;
                case nameof(_viewModel.ErrorDetail):
                    UpdateApiErrorState();
                    break;
                case nameof(_viewModel.SearchText):
                    _viewModel.ApplyFilter();
                    break;
            }
        }

        private void RebuildLists()
        {
            _listsContainer.Clear();

            if (_viewModel.Lists == null || _viewModel.Lists.Count == 0)
            {
                _emptyState.style.visibility = Visibility.Visible;
                return;
            }

            _emptyState.style.visibility = Visibility.Hidden;

            foreach (ShoppingList list in _viewModel.Lists)
            {
                ShoppingList captured = list;

                FMItemListShoppingList item = new FMItemListShoppingList { Text = captured.title };

                item.RemoveButton.clicked += () => OnItemRemoveClicked(captured);
                item.OpenButton.clicked += () => OnItemOpenClicked(captured);

                _listsContainer.Add(item);
            }
        }

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            _viewModel.SearchText = evt.newValue;
        }

        private void OnItemOpenClicked(ShoppingList list)
        {
            _navController?.Navigate(
                Actions.shopping_list_to_detail,
                new[]
                {
                    new Argument("listId", list.id),
                    new Argument("listTitle", list.title)
                });
        }

        private void OnItemRemoveClicked(ShoppingList list)
        {
            FMDialog.ShowConfirm(
                        this,
                        "@UI:DELETE_LIST",
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { list.title }),
                        onConfirm: () => _ = SafeDeleteListAsync(list.id),
                        semantic: AlertSemantic.Destructive);
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
            {
                FMLoadingOverlay.Show(contentContainer);
            }
            else
            {
                FMLoadingOverlay.Hide(contentContainer);
            }
        }

        private void UpdateErrorState()
        {
            if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                Toast.Build(this, _viewModel.ErrorMessage, NotificationDuration.Long)
                    .SetStyle(NotificationStyle.Negative)
                    .SetPosition(PopupNotificationPlacement.Bottom)
                    .Show();
            }
        }

        private void UpdateApiErrorState()
        {
            if (_viewModel.ErrorDetail != null)
            {
                FMDialog.ShowApiError(this, LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ERROR_TITLE"), _viewModel.ErrorDetail);
                _viewModel.ErrorDetail = null;
            }
        }

        private void OnNewListClicked()
        {
            var nameField = new Unity.AppUI.UI.TextField { placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LIST_NAME_PLACEHOLDER"),value = _searchOrCreateField.TextFieldValue };

            FMDialog.ShowCustom(
                this,
                "@UI:NEW_SHOPPING_LIST",
                nameField,
                new FMDialogAction("@UI:TXT_CANCEL", null),
                new FMDialogAction("@UI:CREATE", () =>
                {
                    string name = nameField.value?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _ = SafeCreateListAsync(name);
                    }
                }, isPrimary: true));
        }
        private async Task SafeDeleteListAsync(string id)
        {
            try
            {
                await _viewModel.DeleteListAsync(id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] DeleteListAsync failed: {ex.Message}");
            }
        }

        private async Task SafeCreateListAsync(string name)
        {
            try
            {
                await _viewModel.CreateListAsync(name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CreateListAsync failed: {ex.Message}");
            }
        }
    }
}
