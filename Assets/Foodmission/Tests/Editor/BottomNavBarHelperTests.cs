using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class BottomNavBarHelperTests
    {
        // ── IsNavBarVisible ─────────────────────────────────────────────────

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForSplashScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("Loading"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForLoginScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("Login"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForRegisterScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("Register"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForForgotPasswordScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("ForgotPassword"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForOnboardingProfileScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("OnboardingProfile"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForOnboardingAvatarScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("OnboardingAvatar"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsFalse_ForOnboardingGroupsScreen()
        {
            Assert.IsFalse(BottomNavBarHelper.IsNavBarVisible("OnboardingGroups"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForHomeScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("Home"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForNotificationsScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("Notifications"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForMealLogScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("MealLog"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForProfileScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("Profile"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForSettingsScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("Settings"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForShoppingListScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("ShoppingList"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForPantryScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("Pantry"));
        }

        [Test]
        public void IsNavBarVisible_ReturnsTrue_ForGroupsScreen()
        {
            Assert.IsTrue(BottomNavBarHelper.IsNavBarVisible("Groups"));
        }

        // ── GetActiveTab ─────────────────────────────────────────────────────

        [Test]
        public void GetActiveTab_ReturnsHome_ForHomeScreen()
        {
            Assert.AreEqual(NavTab.Home, BottomNavBarHelper.GetActiveTab("Home"));
        }

        [Test]
        public void GetActiveTab_ReturnsNotifications_ForNotificationsScreen()
        {
            Assert.AreEqual(NavTab.Notifications, BottomNavBarHelper.GetActiveTab("Notifications"));
        }

        [Test]
        public void GetActiveTab_ReturnsMealLog_ForMealLogScreen()
        {
            Assert.AreEqual(NavTab.MealLog, BottomNavBarHelper.GetActiveTab("MealLog"));
        }

        [Test]
        public void GetActiveTab_ReturnsProfile_ForProfileScreen()
        {
            Assert.AreEqual(NavTab.Profile, BottomNavBarHelper.GetActiveTab("Profile"));
        }

        [Test]
        public void GetActiveTab_ReturnsProfile_ForSettingsScreen()
        {
            Assert.AreEqual(NavTab.Profile, BottomNavBarHelper.GetActiveTab("Settings"));
        }

        [Test]
        public void GetActiveTab_ReturnsProfile_ForGroupsScreen()
        {
            Assert.AreEqual(NavTab.Profile, BottomNavBarHelper.GetActiveTab("Groups"));
        }

        [Test]
        public void GetActiveTab_ReturnsProfile_ForGroupsCreateScreen()
        {
            Assert.AreEqual(NavTab.Profile, BottomNavBarHelper.GetActiveTab("GroupsCreate"));
        }

        [Test]
        public void GetActiveTab_ReturnsProfile_ForGroupDetailScreen()
        {
            Assert.AreEqual(NavTab.Profile, BottomNavBarHelper.GetActiveTab("GroupDetail"));
        }

        [Test]
        public void GetActiveTab_ReturnsMenu_ForShoppingListScreen()
        {
            Assert.AreEqual(NavTab.Menu, BottomNavBarHelper.GetActiveTab("ShoppingList"));
        }

        [Test]
        public void GetActiveTab_ReturnsMenu_ForShoppingListDetailScreen()
        {
            Assert.AreEqual(NavTab.Menu, BottomNavBarHelper.GetActiveTab("ShoppingListDetail"));
        }

        [Test]
        public void GetActiveTab_ReturnsMenu_ForPantryScreen()
        {
            Assert.AreEqual(NavTab.Menu, BottomNavBarHelper.GetActiveTab("Pantry"));
        }

        [Test]
        public void GetActiveTab_ReturnsMenu_ForPantryItemDetailScreen()
        {
            Assert.AreEqual(NavTab.Menu, BottomNavBarHelper.GetActiveTab("PantryItemDetail"));
        }

        [Test]
        public void GetActiveTab_ReturnsMealLog_ForMealLogAddScreen()
        {
            Assert.AreEqual(NavTab.MealLog, BottomNavBarHelper.GetActiveTab("MealLogAdd"));
        }

        [Test]
        public void GetActiveTab_ReturnsNone_ForAuthScreens()
        {
            Assert.AreEqual(NavTab.None, BottomNavBarHelper.GetActiveTab("Login"));
            Assert.AreEqual(NavTab.None, BottomNavBarHelper.GetActiveTab("Register"));
            Assert.AreEqual(NavTab.None, BottomNavBarHelper.GetActiveTab("Loading"));
        }
    }
}
