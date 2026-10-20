using System.Collections.Generic;

using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform
{
    /// <summary>
    /// The sections that have a Nutri tutorial: stable ids (stored in settings.seenTutorials, never change them),
    /// the destination each one belongs to, and the UI keys of its messages.
    /// </summary>
    public static class SectionTutorials
    {
        public const string Home = "home";
        public const string MealLog = "meal_log";
        public const string QuickSearch = "quick_search";
        public const string ShoppingList = "shopping_list";
        public const string Pantry = "pantry";
        public const string Quests = "quests";
        public const string Missions = "missions";
        public const string Challenges = "challenges";
        public const string Knowledge = "knowledge";
        public const string Recipes = "recipes";
        public const string FoodWaste = "food_waste";

        public const string NextKey = "TXT_NEXT";
        public const string SkipKey = "TUTORIAL_SKIP";
        public const string StartKey = "TUTORIAL_START";
        public const string GotItKey = "txtGotIt";
        public const string HelpKey = "TUTORIAL_HELP";

        private static readonly Dictionary<string, string> s_IdByDestination = new Dictionary<string, string>
        {
            { Destinations.home, Home },
            { Destinations.meallog, MealLog },
            { Destinations.quick_search, QuickSearch },
            { Destinations.shoppinglist, ShoppingList },
            // The lists screen opens the last list at once, so users land on the detail: both share the tutorial
            { Destinations.shoppinglistdetail, ShoppingList },
            { Destinations.pantry, Pantry },
            { Destinations.quests, Quests },
            { Destinations.missions, Missions },
            { Destinations.challenges, Challenges },
            { Destinations.knowledge, Knowledge },
            { Destinations.recipebook, Recipes },
            { Destinations.foodwaste, FoodWaste }
        };

        private static readonly Dictionary<string, string[]> s_KeysById = new Dictionary<string, string[]>
        {
            { Home, new[] { "TUTORIAL_HOME_1", "TUTORIAL_HOME_2", "TUTORIAL_HOME_3", "TUTORIAL_HOME_4", "TUTORIAL_HOME_5" } },
            { MealLog, new[] { "TUTORIAL_MEAL_LOG" } },
            { QuickSearch, new[] { "TUTORIAL_QUICK_SEARCH" } },
            { ShoppingList, new[] { "TUTORIAL_SHOPPING_LIST" } },
            { Pantry, new[] { "TUTORIAL_PANTRY" } },
            { Quests, new[] { "TUTORIAL_QUESTS" } },
            { Missions, new[] { "TUTORIAL_MISSIONS" } },
            { Challenges, new[] { "TUTORIAL_CHALLENGES" } },
            { Knowledge, new[] { "TUTORIAL_KNOWLEDGE" } },
            { Recipes, new[] { "TUTORIAL_RECIPES" } },
            { FoodWaste, new[] { "TUTORIAL_FOOD_WASTE" } }
        };

        public static IReadOnlyList<string> All { get; } = new List<string>(s_KeysById.Keys);

        /// <summary>Tutorial id of a destination, or null when it has none.</summary>
        public static string IdForDestination(string destinationName)
        {
            if (string.IsNullOrEmpty(destinationName))
            {
                return null;
            }
            return s_IdByDestination.TryGetValue(destinationName, out string id) ? id : null;
        }

        /// <summary>UI keys of the tutorial's messages in order (one per bubble); empty for an unknown id.</summary>
        public static IReadOnlyList<string> MessageKeys(string id)
        {
            if (id != null && s_KeysById.TryGetValue(id, out string[] keys))
            {
                return keys;
            }
            return new string[0];
        }
    }
}
