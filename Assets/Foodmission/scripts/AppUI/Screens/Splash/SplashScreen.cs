using System.Threading.Tasks;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Preserve]
    class SplashScreen : NavigationScreenBase<SplashScreenViewModel>
    {
        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => true;

        private VisualElement _logo;
        private AccessibilityNode _logoNode;

        public SplashScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.SplashTemplate);
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            _logo = contentContainer.Q<VisualElement>("logofoodmission");
            _logo.RemoveFromClassList("visible");
            _logo.RemoveFromClassList("exit");
        }

        public override async void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);

            await Task.Delay(500);
            _logo.AddToClassList("visible");
            await Task.Delay(500);

            if (_viewModel != null)
            {
                string navigationAction = await _viewModel.InitializeAppAsync();

                if (navigationAction == Actions.loading_to_forceupdate && _viewModel.PendingUpdate != null)
                {
                    string jsonData = JsonUtility.ToJson(_viewModel.PendingUpdate);
                    string returnAction = _viewModel.ReturnActionOnSkip;

                    if (string.IsNullOrEmpty(returnAction))
                        returnAction = Actions.loading_to_auth;

                    _navController.Navigate(navigationAction, new[]
                    {
                        new Argument("updateData", jsonData),
                        new Argument("returnAction", returnAction)
                    });
                    return;
                }

                await ExitAnimation(navigationAction);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] ViewModel is null - cannot initialize");
            }
        }

        private async Task ExitAnimation(string navigationAction)
        {
            _logo.RemoveFromClassList("visible");
            _logo.AddToClassList("exit");

            await Task.Delay(500);
            _navController.Navigate(navigationAction);
        }

        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnExit(controller, destination, args);
        }

        // --------------------------------------------------------------------
        // Accessibility
        // --------------------------------------------------------------------

        protected override void SetupAccessibilityNodes()
        {
            base.SetupAccessibilityNodes();
            if (_accessibilityHierarchy == null) return;

            var h = _accessibilityHierarchy;

            _logoNode = h.AddNode("Foodmission");
            _logoNode.role = AccessibilityRole.Image;
            _logoNode.frameGetter = () =>
            {
                if (_logo == null || _logo.panel == null) return Rect.zero;
                var rect = _logo.worldBound;
                var scale = _logo.panel.scaledPixelsPerPoint;
                return new Rect(rect.position * scale, rect.size * scale);
            };
        }

        protected override void TeardownAccessibilityNodes()
        {
            _logoNode = null;
            base.TeardownAccessibilityNodes();
        }
    }
}
