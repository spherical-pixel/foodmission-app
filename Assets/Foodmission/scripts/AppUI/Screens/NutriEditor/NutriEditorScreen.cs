using System;
using eu.foodmission.platform.Components;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    public class NutriEditorScreen : NavigationScreenBase<NutriEditorViewModel>
    {
        override protected bool IsFixedContent => true;
        override protected bool ApplySafeAreaTop => false;
        override protected bool ApplySafeAreaBottom => false;
        override protected bool ApplySafeAreaLeft => false;
        override protected bool ApplySafeAreaRight => false;

        private Unity.AppUI.UI.Button _btAntennas;
        private Unity.AppUI.UI.Button _btGlasses;
        private Unity.AppUI.UI.Button _btEars;
        private Unity.AppUI.UI.Button _btSave;
        private Unity.AppUI.UI.Button _btExit;
        private Components.FMNutriView _nutriView;

        private AccessibilityNode _btAntennasNode;
        private AccessibilityNode _btGlassesNode;
        private AccessibilityNode _btEarsNode;
        private AccessibilityNode _btSaveNode;
        private AccessibilityNode _btExitNode;

        private NutriEditorPanelItem _panelItem;
        private string _currentCategory;
        private bool _isTransitioning;

        public NutriEditorScreen()
        {
            var templateService = App.current?.services?.GetService<ITemplateService>();
            VisualTreeAsset template = templateService?.Get(TemplateAddresses.NutriEditor);

#if UNITY_EDITOR
            if (template == null)
            {
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Foodmission/scripts/AppUI/Screens/NutriEditor/NutriEditorScreen.uxml");
            }
#endif

            if (template != null)
            {
                InitializeComponent(template);
                CacheUIElements();
                RegisterManualEvents();
            }
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            if (_viewModel != null)
            {
                _viewModel.NutriService?.SetActive(true);
                _viewModel.NutriService?.SetCameraActive(true);
                _viewModel.NutriService?.SetMood(NutriMood.Neutral);
                _viewModel.NutriService?.SetAction(NutriAction.Idle);

                await _viewModel.InitializeAsync();
            }
        }

        private void CacheUIElements()
        {
            _btAntennas = contentContainer.Q<Unity.AppUI.UI.Button>("btAntennas");
            _btGlasses = contentContainer.Q<Unity.AppUI.UI.Button>("btGlasses");
            _btEars = contentContainer.Q<Unity.AppUI.UI.Button>("btEars");
            _btSave = contentContainer.Q<Unity.AppUI.UI.Button>("btSave");
            _btExit = contentContainer.Q<Unity.AppUI.UI.Button>("btExit");
            _nutriView = contentContainer.Q<Components.FMNutriView>("nutri-view");
        }

        private void RegisterManualEvents()
        {
            if (_btAntennas != null) _btAntennas.clicked += OnAntennasClicked;
            if (_btGlasses != null) _btGlasses.clicked += OnGlassesClicked;
            if (_btEars != null) _btEars.clicked += OnEarsClicked;
            if (_btSave != null) _btSave.clicked += OnSaveClicked;
            if (_btExit != null) _btExit.clicked += OnExitClicked;
        }

        private void UnregisterManualEvents()
        {
            if (_btAntennas != null) _btAntennas.clicked -= OnAntennasClicked;
            if (_btGlasses != null) _btGlasses.clicked -= OnGlassesClicked;
            if (_btEars != null) _btEars.clicked -= OnEarsClicked;
            if (_btSave != null) _btSave.clicked -= OnSaveClicked;
            if (_btExit != null) _btExit.clicked -= OnExitClicked;
        }

        private void OnAntennasClicked() => OpenCategoryPanel(FoodyItemType.Antennas);
        private void OnGlassesClicked() => OpenCategoryPanel(FoodyItemType.Glasses);
        private void OnEarsClicked() => OpenCategoryPanel(FoodyItemType.Ears);

        private void OpenCategoryPanel(string category)
        {
            if (_isTransitioning) return;
            if (_panelItem != null && _panelItem.IsClosing) return;

            if (_panelItem != null)
            {
                if (_currentCategory == category)
                {
                    // Tapping the active category button closes it
                    CloseCategoryPanel(true);
                    return;
                }

                // Switching to a different category: close old one immediately
                CloseCategoryPanel(false);
            }

            _currentCategory = category;
            _panelItem = new NutriEditorPanelItem();
            _panelItem.Init(OnPanelClosed, category, _viewModel, contentContainer);
            contentContainer.Add(_panelItem);
            _panelItem.AnimateIn();
        }

        private void OnPanelClosed()
        {
            if (_panelItem != null)
            {
                _panelItem.Dispose();
                contentContainer.Remove(_panelItem);
                _panelItem = null;
                _currentCategory = null;
            }
            _isTransitioning = false;
        }

        private void CloseCategoryPanel(bool animate = false)
        {
            if (_panelItem != null)
            {
                var item = _panelItem;
                _panelItem = null;
                _currentCategory = null;

                if (animate)
                {
                    _isTransitioning = true;
                    item.AnimateOut(() =>
                    {
                        item.Dispose();
                        contentContainer.Remove(item);
                        _isTransitioning = false;
                    });
                }
                else
                {
                    item.Dispose();
                    contentContainer.Remove(item);
                    _isTransitioning = false;
                }
            }
        }

        private async void OnSaveClicked()
        {
            CloseCategoryPanel();
            ShowLoadingOverlay();
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.SaveAsync();
                }
            }
            finally
            {
                HideLoadingOverlay();
            }

            OnNavigationRequested(Actions.go_to_home, null);
        }

        private void OnExitClicked()
        {
            CloseCategoryPanel();
            _viewModel?.ExitWithoutSaving();
            OnNavigationRequested(Actions.go_to_home, null);
        }

        protected override void OnViewModelUnbinding()
        {
            CloseCategoryPanel();

            if (_viewModel != null)
            {
                _viewModel.ExitWithoutSaving();
            }

            UnregisterManualEvents();
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
            _btAntennasNode = CreateButtonNode(h, _btAntennas, "Antennas category");
            _btGlassesNode = CreateButtonNode(h, _btGlasses, "Glasses category");
            _btEarsNode = CreateButtonNode(h, _btEars, "Ears category");
            _btSaveNode = CreateButtonNode(h, _btSave, "Save and continue");
            _btExitNode = CreateButtonNode(h, _btExit, "Exit without saving");
        }

        protected override void TeardownAccessibilityNodes()
        {
            _btAntennasNode = null;
            _btGlassesNode = null;
            _btEarsNode = null;
            _btSaveNode = null;
            _btExitNode = null;
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
    }
}
