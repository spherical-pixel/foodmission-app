using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeInteractionCatalogTests
    {
        private static readonly string[] AllBackendCodes =
        {
            "CH.A1.1", "CH.A1.2", "CH.A1.3", "CH.A1.4", "CH.A1.5", "CH.A2.1", "CH.A2.2", "CH.A2.3", "CH.A2.4", "CH.A2.5", "CH.A3.1", "CH.A3.2", "CH.A3.3", "CH.A3.4", "CH.A3.5", "CH.A4.1", "CH.A4.2", "CH.A4.3", "CH.A4.4", "CH.A4.5", "CH.A5.1", "CH.A5.2", "CH.A5.3", "CH.A5.4", "CH.A5.5", "CH.A6.1", "CH.A6.2", "CH.A6.3", "CH.A6.4", "CH.A6.5", "CH.B1.1", "CH.B1.2", "CH.B1.3", "CH.B1.4", "CH.B1.5", "CH.B2.1", "CH.B2.2", "CH.B2.3", "CH.B2.4", "CH.B2.5", "CH.B3.1", "CH.B3.2", "CH.B3.3", "CH.B3.4", "CH.B3.5", "CH.B4.1", "CH.B4.2", "CH.B4.3", "CH.B4.4", "CH.B4.5", "CH.B5.1", "CH.B5.2", "CH.B5.3", "CH.B5.4", "CH.B5.5", "CH.B6.1", "CH.B6.2", "CH.B6.3", "CH.B6.4", "CH.B6.5", "CH.I1.1", "CH.I1.2", "CH.I1.3", "CH.I1.4", "CH.I1.5", "CH.I2.1", "CH.I2.2", "CH.I2.3", "CH.I2.4", "CH.I2.5", "CH.I3.1", "CH.I3.2", "CH.I3.3", "CH.I3.4", "CH.I3.5", "CH.I4.1", "CH.I4.2", "CH.I4.3", "CH.I4.4", "CH.I4.5", "CH.I5.1", "CH.I5.2", "CH.I5.3", "CH.I5.4", "CH.I5.5", "CH.I6.1", "CH.I6.2", "CH.I6.3", "CH.I6.4", "CH.I6.5"
        };

        [Test]
        public void Get_UnknownCode_ReturnsConfirm()
        {
            var interaction = ChallengeInteractionCatalog.Get("CH.Z9.9");

            Assert.AreSame(ChallengeInteraction.Confirm, interaction);
            Assert.IsFalse(interaction.AutoCompletes);
        }

        [Test]
        public void Get_NullOrEmptyCode_ReturnsConfirm()
        {
            Assert.AreSame(ChallengeInteraction.Confirm, ChallengeInteractionCatalog.Get(null));
            Assert.AreSame(ChallengeInteraction.Confirm, ChallengeInteractionCatalog.Get(""));
        }

        [Test]
        public void Get_IsCaseAndWhitespaceInsensitive()
        {
            Assert.AreSame(ChallengeInteractionCatalog.Get("CH.B1.1"), ChallengeInteractionCatalog.Get(" ch.b1.1 "));
        }

        [Test]
        public void Catalog_CoversEveryBackendChallenge_WithExpectedTotals()
        {
            Assert.AreEqual(90, AllBackendCodes.Length);
            var interactions = AllBackendCodes.Select(ChallengeInteractionCatalog.Get).ToList();

            Assert.AreEqual(26, interactions.Count(i => i.Type == ChallengeInteractionType.Confirm));
            Assert.AreEqual(62, interactions.Count(i => i.Type == ChallengeInteractionType.ConfirmWithModule));
            Assert.AreEqual(2, interactions.Count(i => i.Type == ChallengeInteractionType.Comparator));
            Assert.AreEqual(35, interactions.Count(i => i.AutoCompletes));
            Assert.IsTrue(ChallengeInteractionCatalog.Entries.Keys.All(k => AllBackendCodes.Contains(k)), "Catalog has codes unknown to the backend");
        }

        [Test]
        public void Get_ProteinFootprint_IsComparatorCompletedByComparison()
        {
            var interaction = ChallengeInteractionCatalog.Get("CH.B1.1");

            Assert.AreEqual(ChallengeInteractionType.Comparator, interaction.Type);
            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_food_comparison, interaction.ModuleAction);
            Assert.AreEqual("proteins", interaction.ComparisonMode);
            Assert.AreEqual(ChallengeCompletionTrigger.ComparisonDone, interaction.Trigger);
            Assert.AreEqual("CHALLENGE_BTN_COMPARE_PROTEINS", interaction.ModuleButtonKey);
        }

        [Test]
        public void Get_FoodFactChallenge_OpensSpecificFact()
        {
            var interaction = ChallengeInteractionCatalog.Get("CH.A1.4");

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.open_food_fact, interaction.ModuleAction);
            Assert.AreEqual(ChallengeCompletionTrigger.FoodFactRead, interaction.Trigger);
            Assert.AreEqual("FF1.2.8", interaction.FoodFactCode);
        }

        [Test]
        public void Get_MultiProductChallenge_RequiresDistinctCount()
        {
            var interaction = ChallengeInteractionCatalog.Get("CH.B2.2");

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_quicksearch, interaction.ModuleAction);
            Assert.AreEqual(ChallengeCompletionTrigger.ProductViewed, interaction.Trigger);
            Assert.AreEqual(3, interaction.RequiredCount);
        }

        [Test]
        public void Get_JudgementChallenge_HasModuleButStaysManual()
        {
            var interaction = ChallengeInteractionCatalog.Get("CH.B2.4");

            Assert.AreEqual(ChallengeInteractionType.ConfirmWithModule, interaction.Type);
            Assert.IsFalse(interaction.AutoCompletes);
        }

        [Test]
        public void Get_WasteChallenges_UseAddScreenOnlyWhenAutoCompleting()
        {
            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_foodwaste_add, ChallengeInteractionCatalog.Get("CH.B5.3").ModuleAction);
            Assert.AreEqual(3, ChallengeInteractionCatalog.Get("CH.B5.3").RequiredCount);
            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_foodwaste, ChallengeInteractionCatalog.Get("CH.I5.1").ModuleAction);
            Assert.IsFalse(ChallengeInteractionCatalog.Get("CH.I5.1").AutoCompletes);
        }

        [Test]
        public void Entries_WithModule_AlwaysHaveButtonKey()
        {
            foreach (var pair in ChallengeInteractionCatalog.Entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(pair.Value.ModuleAction), pair.Key);
                Assert.IsFalse(string.IsNullOrEmpty(pair.Value.ModuleButtonKey), pair.Key);
            }
        }
    }
}
