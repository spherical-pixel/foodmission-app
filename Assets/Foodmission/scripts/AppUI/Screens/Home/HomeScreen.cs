using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Home Screen
    /// </summary>
    [Preserve]
    class HomeScreen : NavigationScreenBase<HomeScreenViewModel>
    {
        public HomeScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.HomeTemplate);
        }

        protected override void OnViewModelBound()
        {
            base.OnViewModelBound();
            
        }

        protected override void OnViewModelUnbinding()
        {
            

            base.OnViewModelUnbinding();
        }
    }
}
