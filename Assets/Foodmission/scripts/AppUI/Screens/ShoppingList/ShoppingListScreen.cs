using System;
using System.ComponentModel;
using System.Threading.Tasks;

using UnityEngine;

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
    class ShoppingListScreen : NavigationScreenBase<ShoppingListViewModel>
    {
        private VisualElement _listsContainer;
        private Text _errorText;
        private Text _emptyState;
        private Unity.AppUI.UI.Button _btnNewList;

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
            _errorText = contentContainer.Q<Text>("error-message");
            _emptyState = contentContainer.Q<Text>("empty-state");
            _btnNewList = contentContainer.Q<Unity.AppUI.UI.Button>("btn-new-list");
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();

            _btnNewList.clicked += OnNewListClicked;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

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
            _btnNewList.clicked -= OnNewListClicked;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
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
            }
        }

        private void RebuildLists()
        {
            _listsContainer.Clear();

            if (_viewModel.Lists == null || _viewModel.Lists.Count == 0)
            {
                _emptyState?.EnableInClassList("visible", true);
                return;
            }

            _emptyState?.EnableInClassList("visible", false);

            foreach (ShoppingList list in _viewModel.Lists)
            {
                ShoppingList captured = list;

                var row = new VisualElement();
                row.AddToClassList("fm-sl-row");

                var nameLabel = new Text { text = captured.title };
                nameLabel.AddToClassList("fm-sl-row-name");

                var deleteBtn = new IconButton { icon = "trash" };

                row.Add(nameLabel);
                row.Add(deleteBtn);

                row.RegisterCallback<ClickEvent>(_ =>
                    _navController?.Navigate(
                        Actions.shopping_list_to_detail,
                        new[]
                        {
                            new Argument("listId", captured.id),
                            new Argument("listTitle", captured.title)
                        }));

                string capturedId = captured.id;
                string capturedTitle = captured.title;
                deleteBtn.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    FMDialog.ShowConfirm(
                        this,
                        "@UI:DELETE_LIST",
                        LocalizationSettings.StringDatabase.GetLocalizedString("UI", "CONFIRM_DELETE_MSG", new object[] { capturedTitle }),
                        onConfirm: () => _ = SafeDeleteListAsync(capturedId),
                        semantic: AlertSemantic.Destructive);
                });

                _listsContainer.Add(row);
            }
        }

        private void UpdateLoadingState()
        {
            bool isLoading = _viewModel.IsLoading;
            if (isLoading)
                FMLoadingOverlay.Show(contentContainer);
            else
                FMLoadingOverlay.Hide(contentContainer);
            if (_btnNewList != null)
            {
                _btnNewList.SetEnabled(!isLoading);
            }
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
            var nameField = new Unity.AppUI.UI.TextField { placeholder = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "LIST_NAME_PLACEHOLDER") };

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
