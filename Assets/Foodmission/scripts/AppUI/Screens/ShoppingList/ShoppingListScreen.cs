using System.ComponentModel;

using eu.foodmission.platform.Components;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class ShoppingListScreen : NavigationScreenBase<ShoppingListViewModel>
    {
        private VisualElement _listsContainer;
        private CircularProgress _spinner;
        private Text _errorText;
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
            _spinner = contentContainer.Q<CircularProgress>("loading-spinner");
            _errorText = contentContainer.Q<Text>("error-message");
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

            _ = _viewModel.LoadListsAsync();
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
            }
        }

        private void RebuildLists()
        {
            _listsContainer.Clear();

            if (_viewModel.Lists == null)
            {
                return;
            }

            foreach (ShoppingList list in _viewModel.Lists)
            {
                ShoppingList captured = list;

                var row = new VisualElement();
                row.AddToClassList("fm-sl-row");

                var nameLabel = new Text { text = captured.name };
                nameLabel.AddToClassList("fm-sl-row-name");

                var deleteBtn = new IconButton { icon = "trash" };

                row.Add(nameLabel);
                row.Add(deleteBtn);

                row.RegisterCallback<ClickEvent>(_ =>
                    _navController?.Navigate(
                        Actions.shopping_list_to_detail,
                        new[] { new Argument("listId", captured.id) }));

                deleteBtn.clicked += () =>
                    FMDialog.ShowConfirm(
                        this,
                        "Delete list",
                        $"Delete \"{captured.name}\"?",
                        onConfirm: async () => await _viewModel.DeleteListAsync(captured.id),
                        semantic: AlertSemantic.Destructive);

                _listsContainer.Add(row);
            }
        }

        private void UpdateLoadingState()
        {
            _spinner?.EnableInClassList("visible", _viewModel.IsLoading);
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

        private void OnNewListClicked()
        {
            var nameField = new Unity.AppUI.UI.TextField { label = "List name" };

            FMDialog.ShowCustom(
                this,
                "New shopping list",
                nameField,
                new FMDialogAction("Cancel", null),
                new FMDialogAction("Create", async () =>
                {
                    string name = nameField.value?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        await _viewModel.CreateListAsync(name);
                    }
                }, isPrimary: true));
        }
    }
}
