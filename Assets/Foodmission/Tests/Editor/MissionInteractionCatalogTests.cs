using System.Linq;

using NUnit.Framework;

using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionInteractionCatalogTests
    {
        internal static readonly string[] AllMissionCodes =
        {
            "M.A1.1", "M.A1.2", "M.A1.3", "M.A1.4", "M.A1.5", "M.A2.1", "M.A2.2", "M.A2.3", "M.A2.4", "M.A2.5",
            "M.A3.1", "M.A3.2", "M.A3.3", "M.A3.4", "M.A3.5", "M.A4.1", "M.A4.2", "M.A4.3", "M.A4.4", "M.A4.5",
            "M.A5.1", "M.A5.2", "M.A5.3", "M.A5.4", "M.A5.5", "M.A6.1", "M.A6.2", "M.A6.3", "M.A6.4", "M.A6.5",
            "M.B1.1", "M.B1.2", "M.B1.3", "M.B1.4", "M.B1.5", "M.B2.1", "M.B2.2", "M.B2.3", "M.B2.4", "M.B2.5",
            "M.B3.1", "M.B3.2", "M.B3.3", "M.B3.4", "M.B3.5", "M.B4.1", "M.B4.2", "M.B4.3", "M.B4.4", "M.B4.5",
            "M.B5.1", "M.B5.2", "M.B5.3", "M.B5.4", "M.B5.5", "M.B6.1", "M.B6.2", "M.B6.3", "M.B6.4", "M.B6.5",
            "M.I1.1", "M.I1.2", "M.I1.3", "M.I1.4", "M.I1.5", "M.I2.1", "M.I2.2", "M.I2.3", "M.I2.4", "M.I2.5",
            "M.I3.1", "M.I3.2", "M.I3.3", "M.I3.4", "M.I3.5", "M.I4.1", "M.I4.2", "M.I4.3", "M.I4.4", "M.I4.5",
            "M.I5.1", "M.I5.2", "M.I5.3", "M.I5.4", "M.I5.5", "M.I6.1", "M.I6.2", "M.I6.3", "M.I6.4", "M.I6.5"
        };

        internal static readonly string[] PendingRuleCodes =
        {
            "M.A2.1", "M.A2.2", "M.A2.5", "M.A4.1", "M.A4.3", "M.A6.1", "M.A6.4", "M.I2.4", "M.I5.4", "M.I6.2"
        };

        [Test]
        public void Get_UnknownCode_ReturnsUnknownPendingRule()
        {
            var interaction = MissionInteractionCatalog.Get("M.Z9.9");

            Assert.AreSame(MissionInteraction.Unknown, interaction);
            Assert.AreEqual(MissionInteractionStatus.PendingRule, interaction.Status);
            Assert.IsFalse(interaction.CanReport);
            Assert.AreSame(MissionInteraction.Unknown, MissionInteractionCatalog.Get(null));
        }

        [Test]
        public void Get_IsCaseAndWhitespaceInsensitive()
        {
            Assert.AreSame(MissionInteractionCatalog.Get("M.B1.4"), MissionInteractionCatalog.Get(" m.b1.4 "));
        }

        [Test]
        public void Catalog_CoversEveryMission_WithExpectedTotals()
        {
            Assert.AreEqual(90, MissionInteractionCatalog.Entries.Count);
            CollectionAssert.AreEquivalent(AllMissionCodes, MissionInteractionCatalog.Entries.Keys.ToArray());

            var pending = AllMissionCodes.Where(c => MissionInteractionCatalog.Get(c).Status == MissionInteractionStatus.PendingRule).ToArray();
            CollectionAssert.AreEquivalent(PendingRuleCodes, pending);

            int auto = AllMissionCodes.Count(c => MissionInteractionCatalog.Get(c).AutoModules.Count > 0);
            Assert.AreEqual(42, auto);
        }

        [Test]
        public void Missions_OnlyLinkModulesThatCountAutomatically()
        {
            // Search, pantry or waste screens record nothing a mission rule counts: a button there would only be a shortcut
            foreach (string code in AllMissionCodes)
            {
                CollectionAssert.IsSubsetOf(MissionInteractionCatalog.Get(code).AutoModules, new[] { MissionInteractionCatalog.QuickMealLog }, code);
            }
        }

        [Test]
        public void AvailableEntries_HaveSteps_AndPendingHaveNone()
        {
            foreach (string code in AllMissionCodes)
            {
                var interaction = MissionInteractionCatalog.Get(code);
                if (interaction.Status == MissionInteractionStatus.Available)
                {
                    Assert.IsTrue(interaction.CanReport, code);
                }
                else
                {
                    Assert.AreEqual(0, interaction.Steps.Count, code);
                }
            }
        }

        [Test]
        public void Steps_AreWellFormed()
        {
            foreach (string code in AllMissionCodes)
            {
                foreach (var step in MissionInteractionCatalog.Get(code).Steps)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(step.PromptKey), code);
                    Assert.Greater(step.MaxCount, 0, code);
                    switch (step.Type)
                    {
                        case MissionStepType.OptionPicker:
                            Assert.Greater(step.Options.Count, 0, code);
                            break;
                        case MissionStepType.MealReport:
                            Assert.IsTrue(step.EventType != null || step.Options.Count > 0, $"{code} meal step needs a flag or swap");
                            Assert.IsTrue(step.Options.All(o => o.IsSwap) || step.Options.All(o => !o.IsSwap), $"{code} meal options must be all swaps or all flags");
                            break;
                        default:
                            Assert.IsFalse(string.IsNullOrEmpty(step.EventType), code);
                            break;
                    }
                }
            }
        }

        [Test]
        public void ProteinSwapMission_IsSwapMealReportWithQuickMealLog()
        {
            var interaction = MissionInteractionCatalog.Get("M.B1.4");

            Assert.AreSame(MissionInteractionCatalog.QuickMealLog, interaction.AutoModules.Single());
            Assert.AreEqual(Actions.open_quick_meal_log, interaction.AutoModules[0].Action);
            var step = interaction.Steps.Single();
            Assert.AreEqual(MissionStepType.MealReport, step.Type);
            Assert.AreEqual(2, step.MaxCount);
            Assert.AreEqual(6, step.Options.Count);
            Assert.IsTrue(step.Options.All(o => o.IsSwap));
            Assert.AreEqual("SWAP_BEEF_TO_LEGUMES", step.Options.Single(o => o.EventType == ClientEventTypes.SwapBeefToLegumes).LabelKey);
        }

        [Test]
        public void HighFibreBreakfast_IsOnePerDayBreakfastMealReport()
        {
            var step = MissionInteractionCatalog.Get("M.A6.2").Steps.Single();

            Assert.AreEqual(MissionStepType.MealReport, step.Type);
            Assert.IsTrue(step.OnePerDay);
            Assert.AreEqual("BREAKFAST", step.FixedMealType);
            Assert.AreEqual(ClientEventTypes.NutritionHighFibreMeal, step.EventType);
            Assert.AreEqual("MISSION_QD_NUTRITION_HIGH_FIBRE_MEAL", step.PromptKey);
        }

        [Test]
        public void GreenScoreComparison_FixesComparedWithMetadata()
        {
            var step = MissionInteractionCatalog.Get("M.A3.1").Steps.Single();

            Assert.AreEqual(MissionStepType.Count, step.Type);
            Assert.AreEqual("productId", step.DistinctField);
            Assert.AreEqual("GREEN_SCORE", step.FixedMetadata["comparedWith"]);
        }
    }
}
