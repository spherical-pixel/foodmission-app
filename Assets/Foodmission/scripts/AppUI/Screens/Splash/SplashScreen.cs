using System.Threading.Tasks;
using Unity.AppUI.Navigation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Splash screen
    /// Navigates automatically to Home or Login based on authentication state.
    /// </summary>
    [Preserve]
    class SplashScreen : NavigationScreenBase<SplashScreenViewModel>
    {

        protected override bool ApplySafeAreaBottom => false;
        protected override bool ApplySafeAreaLeft => false;
        protected override bool ApplySafeAreaRight => false;
        protected override bool ApplySafeAreaTop => false;
        protected override bool IsFixedContent => true;

        private VisualElement _logo;

        public SplashScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.SplashTemplate);
            CacheUIElements();

        }

         /// <summary>
        /// Cache UI elements references
        /// </summary>
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
            
            // Async init 
            if (_viewModel != null)
            {
                string navigationAction = await _viewModel.InitializeAppAsync();

                await ExitAnimation(navigationAction);
                //_navController.Navigate(navigationAction);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] ViewModel is null - cannot initialize");
            }
        }


        private async Task ExitAnimation(string navigationAction)
        {
            //contentContainer.dataSource = null;
            //contentContainer.Unbind();
            // _logo.RemoveFromClassList("visible");
            // _logo.AddToClassList("exit");

            await Task.Delay(500);
            _navController.Navigate(navigationAction);

        }


        public override void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnExit(controller, destination, args);
        }

        
    }
}
