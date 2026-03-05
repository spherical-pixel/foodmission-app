// UxmlNavigationScreen.cs
using UnityEngine.UIElements;
using Unity.AppUI.Navigation;

namespace eu.foodmission.platform
{
    public class UxmlNavigationScreen : VisualElement, INavigationScreen
    {
        readonly VisualTreeAsset _uxmlAsset;

        public UxmlNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
        {
            _uxmlAsset = uxmlAsset;
            _uxmlAsset.CloneTree(this);
        }

        public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            // Called when the screen is entered.
            // You can use this method to initialize the screen or to perform any action when the screen is displayed.
        }

        public void OnExit(NavController controller, NavDestination destination, Argument[] args)
        {
            // Called when the screen is exited.
            // You can use this method to clean up the screen or to perform any action when the screen is hidden.
        }
    }
}
