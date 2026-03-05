using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class FoodmissionVisualController : INavVisualController
    {
        public void SetupBottomNavBar(BottomNavBar bottomNavBar, NavDestination destination, NavController navController)
        {
            // Clean existing buttons
            bottomNavBar.Clear();

            AddItem(bottomNavBar, "fm-home", "@UI:NAV_HOME", Destinations.home, destination, navController);
            AddItem(bottomNavBar, "fm-menu", "@UI:NAV_MENU", Destinations.menu, destination, navController);
            AddItem(bottomNavBar, "fm-notifications", "@UI:NAV_NOTIFICATIONS", Destinations.home, destination, navController);
            AddItem(bottomNavBar, "fm-meal-log", "@UI:NAV_MEAL_LOG", Destinations.home, destination, navController);
            AddItem(bottomNavBar, "fm-profile", "@UI:NAV_PROFILE", Destinations.home, destination, navController,true);
        }

        public void SetupAppBar(AppBar appBar, NavDestination destination, NavController navController)
        {
            if (appBar == null || destination == null || navController == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SetupAppBar - null parameters: appBar={appBar != null}, destination={destination != null}, navController={navController != null}");
                return;
            }

            appBar.title = destination.label;
            appBar.stretch = true;

            // Apply safe area padding to AppBar
            var themeService = App.current?.services.GetService<IThemeService>();
            if (themeService != null)
            {
                // Apply padding to the internal bar element
                var bar = appBar.Q<UnityEngine.UIElements.VisualElement>(className: "appui-appbar__bar");
                if (bar != null)
                {
                    var safeAreaTop = themeService.safeAreaTop;
                    bar.style.paddingTop = safeAreaTop;

                    // Add proportional bottom padding to keep content visually balanced
                    // without creating excessive empty space
                    if (safeAreaTop > 0)
                    {
                        bar.style.paddingBottom = safeAreaTop * 0.30f;
                    }
                }
            }
            
        }

        public void SetupDrawer(Drawer drawer, NavDestination destination, NavController navController)
        {
            // Empty by now
        }

        public void SetupNavigationRail(NavigationRail navigationRail, NavDestination destination, NavController navController)
        {
            // Empty by now
        }

        /// <summary>
        /// Helper to add new items to bottom nav bar
        /// </summary>
        /// <param name="bottomNavBar"></param>
        /// <param name="icon"></param>
        /// <param name="label"></param>
        /// <param name="route"></param>
        /// <param name="current"></param>
        /// <param name="navController"></param>
        /// <param name="isNoTint"></param>
        static void AddItem(BottomNavBar bottomNavBar,string icon,string label,string route,NavDestination current,NavController navController,bool isNoTint=false)
        {
            var item = new BottomNavBarItem(icon, label, () => navController.Navigate(route));
            var selected = current.name == route;
            item.isSelected = selected;
            item.EnableInClassList("active", selected);

            if(isNoTint)
            {
                item.AddToClassList("no-tint");
            }
            bottomNavBar.Add(item);
        }
    }
}