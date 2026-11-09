using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Nobody awaits an async void method: an exception in it is only logged, and whatever the method had started
    /// (a loading overlay, a disabled button, the splash) stays that way. Each one needs its own catch to restore the screen.
    /// </summary>
    [TestFixture]
    public class AsyncVoidHandlingTests
    {
        private const string ScriptsRoot = StaticAssetScan.Root + "/scripts/";

        private static readonly Regex k_AsyncVoidMethod = new Regex(@"\basync\s+void\s+(\w+)\s*\([^)]*\)\s*\{");
        private static readonly Regex k_CommentOrLiteral = new Regex(
            @"//[^\n]*|/\*.*?\*/|@""(?:""""|[^""])*""|""(?:\\.|[^""\\\n])*""|'(?:\\.|[^'\\\n])+'",
            RegexOptions.Singleline);
        private static readonly Regex k_Catch = new Regex(@"\bcatch\b");

        // Written before this check existed. Fix them (catch, then restore the screen) and remove them from here;
        // this list can only shrink.
        private static readonly HashSet<string> k_WithoutCatchBaseline = new HashSet<string>
        {
            "AppUI/Components/FMAvatarView.cs:AttachMode",
            "AppUI/Core/FoodmissionAppBuilder.cs:InitializeNotificationSystem",
            "AppUI/Screens/AvatarEditor/AvatarEditorScreen.cs:OnSaveClicked",
            "AppUI/Screens/EditProfile/EditProfileScreen.cs:OnConsentClicked",
            "AppUI/Screens/EditProfile/EditProfileScreen.cs:OnEnter",
            "AppUI/Screens/EditProfile/EditProfileScreen.cs:OnPrivacyClicked",
            "AppUI/Screens/EditProfile/EditProfileScreen.cs:OnSubmitClicked",
            "AppUI/Screens/EditProfile/EditProfileScreen.cs:OnTermsClicked",
            "AppUI/Screens/FoodFact/FoodFactScreen.cs:OnContinueClicked",
            "AppUI/Screens/FoodFact/FoodFactScreen.cs:OnShareClicked",
            "AppUI/Screens/FoodFact/FoodFactScreen.cs:OnShareLongPressed",
            "AppUI/Screens/FoodFact/FoodFactScreen.cs:RequestLoadFoodFact",
            "AppUI/Screens/Groups/GroupDetailScreen.cs:OnEnter",
            "AppUI/Screens/Groups/GroupsCreateScreen.cs:OnCreateClicked",
            "AppUI/Screens/Groups/GroupsJoinScreen.cs:OnJoinClicked",
            "AppUI/Screens/Home/HomeScreen.cs:SetupPilotDebugPanel",
            "AppUI/Screens/MealLog/MealLogViewModel.cs:OnLanguageChanged",
            "AppUI/Screens/Missions/MissionCheckInScreen.cs:OnEnter",
            "AppUI/Screens/NutriEditor/NutriEditorScreen.cs:OnEnter",
            "AppUI/Screens/NutriEditor/NutriEditorScreen.cs:OnSaveClicked",
            "AppUI/Screens/Onboarding/OnboardingGroupsScreen.cs:OnCreateClicked",
            "AppUI/Screens/Onboarding/OnboardingGroupsScreen.cs:OnJoinClicked",
            "AppUI/Screens/Onboarding/OnboardingProfileScreen.cs:OnEnter",
            "AppUI/Screens/Onboarding/OnboardingProfileViewModel.cs:Skip",
            "AppUI/Screens/OnboardingSurvey/OnboardingSurveyViewModel.cs:OnLanguageChanged",
            "AppUI/Screens/Pantry/PantryItemDetailScreen.cs:OnEnter",
            "AppUI/Screens/Pantry/PantryItemDetailScreen.cs:OnSaveClicked",
            "AppUI/Screens/PilotSurvey/PilotSurveyScreen.cs:OnEnter",
            "AppUI/Screens/QuickSearch/QuickSearchScreen.cs:ShowMealLogQuickOptionsModal",
            "AppUI/Screens/Quiz/QuizScreen.cs:OnShareClicked",
            "AppUI/Screens/Recipes/RecipeDetailScreen.cs:ShowQuickMealLogOptionsDialog",
            "AppUI/Screens/Recipes/RecipeEditorScreen.cs:OnEnter",
            "AppUI/Screens/Register/RegisterScreen.cs:OnEnter",
            "AppUI/Screens/Settings/SettingsViewModel.cs:SetPushNotifications",
        };

        [Test]
        public void AsyncVoidMethods_CatchTheirExceptions()
        {
            var problems = new List<string>();
            foreach (var (key, line) in AsyncVoidMethodsWithoutCatch())
            {
                if (!k_WithoutCatchBaseline.Contains(key))
                {
                    problems.Add($"{key} (line {line}): wrap the body in try/catch and restore the screen in the catch, or return a Task");
                }
            }

            StaticAssetScan.AssertNoProblems(problems, "async void method(s) without a catch");
        }

        [Test]
        public void Baseline_OnlyListsMethodsStillWithoutCatch()
        {
            HashSet<string> current = new HashSet<string>(AsyncVoidMethodsWithoutCatch().Select(m => m.Key));

            List<string> stale = k_WithoutCatchBaseline
                .Where(key => !current.Contains(key))
                .OrderBy(key => key)
                .Select(key => $"{key}: fixed, renamed or removed — take it out of the baseline")
                .ToList();

            StaticAssetScan.AssertNoProblems(stale, "stale baseline entr(y/ies)");
        }

        private static IEnumerable<(string Key, int Line)> AsyncVoidMethodsWithoutCatch()
        {
            foreach (string path in StaticAssetScan.FilesUnderRoot(".cs").Where(p => p.StartsWith(ScriptsRoot)))
            {
                // Blank comments and literals (keeping line breaks) so braces and words inside them don't count
                string code = k_CommentOrLiteral.Replace(File.ReadAllText(path),
                    m => new string(m.Value.Where(c => c == '\n').ToArray()));

                foreach (Match method in k_AsyncVoidMethod.Matches(code))
                {
                    string body = Body(code, method.Index + method.Length);
                    if (!k_Catch.IsMatch(body))
                    {
                        int line = code.Take(method.Index).Count(c => c == '\n') + 1;
                        yield return ($"{path.Substring(ScriptsRoot.Length)}:{method.Groups[1].Value}", line);
                    }
                }
            }
        }

        /// <summary>Text from just after the opening brace up to its matching closing brace.</summary>
        private static string Body(string code, int start)
        {
            int depth = 1;
            int i = start;
            while (i < code.Length && depth > 0)
            {
                if (code[i] == '{')
                {
                    depth++;
                }
                else if (code[i] == '}')
                {
                    depth--;
                }
                i++;
            }
            return code.Substring(start, i - start - 1);
        }
    }
}
