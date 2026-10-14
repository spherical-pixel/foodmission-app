using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using Unity.AppUI.Navigation.Generated;

using UnityEngine;

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

        internal static readonly string[] PendingRuleCodes = { };

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
            Assert.AreEqual(45, auto);
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
                        case MissionStepType.ProductChecklist:
                        case MissionStepType.DayChecklist:
                            var spec = step.Checklist;
                            Assert.IsNotNull(spec, code);
                            Assert.Greater(spec.Options.Count, 0, code);
                            Assert.That(spec.MinDistinct, Is.InRange(1, spec.Options.Count), code);
                            Assert.IsTrue(spec.Required.All(r => r >= 0 && r < spec.Options.Count), code);
                            if (spec.IsArray)
                            {
                                Assert.IsFalse(string.IsNullOrEmpty(step.EventType), $"{code} array checklist needs its event");
                                Assert.IsTrue(Enumerable.Range(0, spec.Options.Count).All(i => spec.ValueOf(i) != null), $"{code} array options need values");
                            }
                            else
                            {
                                Assert.IsTrue(spec.Options.All(o => !string.IsNullOrEmpty(o.EventType)), $"{code} chips need event types");
                            }
                            Assert.IsTrue(step.Type == MissionStepType.ProductChecklist || spec.IsArray, $"{code} day checklists send arrays");
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
        public void OriginPlusOne_IsProductChecklistWithOriginRequired()
        {
            var step = MissionInteractionCatalog.Get("M.A2.1").Steps.Single();

            Assert.AreEqual(MissionStepType.ProductChecklist, step.Type);
            Assert.AreEqual(3, step.MaxCount);
            Assert.AreEqual(2, step.Checklist.MinDistinct);
            Assert.AreEqual(ClientEventTypes.ShoppingOriginChecked, step.Checklist.Options[step.Checklist.Required.Single()].EventType);
            Assert.IsNull(step.DistinctField, "the builder sets one productId per product");
            Assert.AreEqual(0, MissionInteractionCatalog.Get("M.A2.1").AutoModules.Count);
        }

        [Test]
        public void DoubleLabel_SendsLabelsArray()
        {
            var step = MissionInteractionCatalog.Get("M.A2.2").Steps.Single();

            Assert.AreEqual(MissionStepType.ProductChecklist, step.Type);
            Assert.AreEqual(ClientEventTypes.ShoppingDoubleLabelChosen, step.EventType);
            Assert.AreEqual("labels", step.Checklist.ArrayField);
            Assert.AreEqual(1, step.MaxCount);
            CollectionAssert.AreEqual(new[] { "EU_ORGANIC", "FAIRTRADE", "RAINFOREST_ALLIANCE", "MSC", "ASC", "EU_ECOLABEL", "OTHER" },
                Enumerable.Range(0, step.Checklist.Options.Count).Select(step.Checklist.ValueOf).ToArray());
            Assert.AreEqual("MISSION_OPT_LABEL_EU_ORGANIC", step.Checklist.Options[0].LabelKey);
        }

        [Test]
        public void Rainbow_IsDayChecklistOfSixColoursNeedingFive()
        {
            var step = MissionInteractionCatalog.Get("M.A6.1").Steps.Single();

            Assert.AreEqual(MissionStepType.DayChecklist, step.Type);
            Assert.AreEqual(ClientEventTypes.NutritionRainbowColoursLogged, step.EventType);
            Assert.AreEqual("colours", step.Checklist.ArrayField);
            Assert.AreEqual(5, step.Checklist.MinDistinct);
            Assert.AreEqual(7, step.MaxCount);
            Assert.AreEqual("MISSION_QD_RAINBOW_COLOURS", step.PromptKey);
        }

        [Test]
        public void HighFibreBreakfast_IsOnePerDayBreakfastMealReport()
        {
            var step = MissionInteractionCatalog.Get("M.A6.2").Steps.Single();

            Assert.AreEqual(MissionStepType.MealReport, step.Type);
            Assert.IsTrue(step.OnePerDay);
            CollectionAssert.AreEqual(new[] { "BREAKFAST" }, step.MealTypes.ToArray());
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

        [Test]
        public void ChecklistTexts_ExistInUiCsv()
        {
            string csv = File.ReadAllText(Path.Combine(Application.dataPath, "Foodmission/localization/CSV/UI.csv"));
            var keys = new List<string>
            {
                "MISSION_CHECKLIST_PRODUCT", "MISSION_CHECKLIST_ADD_PRODUCT", "MISSION_CHECKLIST_COUNTS",
                "MISSION_CHECKLIST_NEED_MORE", "MISSION_CHECKLIST_NEED_REQUIRED", "MISSION_CHECKLIST_PROGRESS"
            };
            foreach (string code in AllMissionCodes)
            {
                foreach (var step in MissionInteractionCatalog.Get(code).Steps.Where(s => s.Checklist != null))
                {
                    keys.Add(step.PromptKey);
                    keys.AddRange(step.Checklist.Options.Select(o => o.LabelKey));
                    keys.Add(step.Checklist.ItemLabelKey);
                    keys.Add(step.Checklist.AddLabelKey);
                }
            }

            var missing = keys.Distinct().Where(k => !csv.Contains("\"" + k + "\",")).ToList();
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        [TestCase("M.B2.1", true)]   // Count with synthetic productIds
        [TestCase("M.B2.3", true)]   // Count of occasions
        [TestCase("M.A3.5", true)]   // OptionPicker per product
        [TestCase("M.A2.1", true)]   // ProductChecklist
        [TestCase("M.A6.5", true)]   // Count of plant foods
        [TestCase("M.I1.5", false)]  // protein sources: the rule counts distinct sources
        [TestCase("M.I5.1", false)]  // YesNo: one is enough
        [TestCase("M.A5.4", false)]  // DayPicker: covered days aren't offered again
        [TestCase("M.A6.1", false)]  // DayChecklist: recorded colours are locked
        public void AddsUpAcrossReports_OnlyForStepsThatWouldCountRepeatsTwice(string code, bool expected)
        {
            var step = MissionInteractionCatalog.Get(code).Steps.First(s => s.Type != MissionStepType.MealReport);

            Assert.AreEqual(expected, step.AddsUpAcrossReports, code);
        }

        [TestCase("M.A2.1", "MISSION_CHECKLIST_MEAL", "MISSION_CHECKLIST_ADD_MEAL")]
        [TestCase("M.A2.5", "MISSION_CHECKLIST_PURCHASE", "MISSION_CHECKLIST_ADD_PURCHASE")]
        [TestCase("M.A4.1", "MISSION_CHECKLIST_PURCHASE", "MISSION_CHECKLIST_ADD_PURCHASE")]
        [TestCase("M.I2.4", "MISSION_CHECKLIST_PRODUCT", "MISSION_CHECKLIST_ADD_PRODUCT")]
        [TestCase("M.A2.2", "MISSION_CHECKLIST_PRODUCT", "MISSION_CHECKLIST_ADD_PRODUCT")]
        public void ProductChecklist_NamesItemsLikeTheMissionGoal(string code, string itemKey, string addKey)
        {
            var spec = MissionInteractionCatalog.Get(code).Steps.Single().Checklist;

            Assert.AreEqual(itemKey, spec.ItemLabelKey, code);
            Assert.AreEqual(addKey, spec.AddLabelKey, code);
        }
    }
}
