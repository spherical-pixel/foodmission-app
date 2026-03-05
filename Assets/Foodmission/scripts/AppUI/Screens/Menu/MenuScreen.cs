using UnityEngine.Scripting;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Menu Screen
    /// </summary>
    [Preserve]
    class MenuScreen : NavigationScreenBase<MenuScreenViewModel>
    {
        public MenuScreen()
        {
            InitializeComponent(FoodmissionAppBuilder.instance.MenuTemplate);
        }
    }
}
