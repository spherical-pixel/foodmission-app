using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Which bottom nav tab should be highlighted for a given destination.
    /// </summary>
    public enum NavTab
    {
        None,
        Home,
        Menu,
        Notifications,
        MealLog,
        Profile
    }

    /// <summary>
    /// Pure logic helpers for the bottom navigation bar.
    /// Determines visibility and active tab state from a destination name.
    /// </summary>
    public static class BottomNavBarHelper
    {
        static readonly HashSet<string> k_HiddenDestinations = new HashSet<string>
        {
            "Loading",
            "Login",
            "Register",
            "ForgotPassword",
            "OnboardingProfile",
            "OnboardingAvatar",
            "OnboardingGroups"
        };

        /// <summary>
        /// Returns true if the bottom nav bar should be visible for the given destination.
        /// Auth and onboarding screens hide the nav bar.
        /// </summary>
        public static bool IsNavBarVisible(string destinationName)
        {
            return !k_HiddenDestinations.Contains(destinationName);
        }

        /// <summary>
        /// Returns the tab that should appear active for the given destination.
        /// Child screens (e.g. Settings, Groups) highlight their parent tab.
        /// </summary>
        public static NavTab GetActiveTab(string destinationName)
        {
            switch (destinationName)
            {
                case "Home":
                    return NavTab.Home;

                case "MENU":
                case "ShoppingList":
                case "ShoppingListDetail":
                case "Pantry":
                case "PantryItemDetail":
                    return NavTab.Menu;

                case "Notifications":
                    return NavTab.Notifications;

                case "MealLog":
                case "MealLogAdd":
                    return NavTab.MealLog;

                case "Profile":
                case "Settings":
                case "Groups":
                case "GroupsCreate":
                case "GroupsJoin":
                case "GroupDetail":
                    return NavTab.Profile;

                default:
                    return NavTab.None;
            }
        }
    }
}
