using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProductMissionPlannerTests
    {
        private static CheckInMission M(string code) =>
            new CheckInMission(code, "T " + code, MissionInteractionCatalog.Get(code), DateTime.Now.AddDays(-1));

        private static ProductMissionInputs Inputs(FoodInfoType type, params string[] codes)
        {
            var inputs = new ProductMissionInputs { FoodType = type, FoodId = "p-1" };
            foreach (string code in codes)
            {
                inputs.Missions.Add(M(code));
                inputs.Progress[code] = new MissionProgress { missionCode = code, progress = 40 };
            }
            return inputs;
        }

        [Test]
        public void OnlyProductSteps_GiveCards()
        {
            // M.B2.1 origin count (product), M.A1.3 meals (no card), M.A5.5 confirms (no card)
            var cards = ProductMissionPlanner.Plan(Inputs(FoodInfoType.Product, "M.B2.1", "M.A1.3", "M.A5.5"));

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("M.B2.1", cards[0].MissionCode);
            Assert.AreEqual(ProductMissionCardKind.Single, cards[0].Kind);
            Assert.AreEqual(40, cards[0].ProgressPercent);
            Assert.AreEqual(ProductMissionPlanner.ProductIdField, cards[0].IdField);
        }

        [Test]
        public void FoodType_SelectsIdField()
        {
            Assert.IsEmpty(ProductMissionPlanner.Plan(Inputs(FoodInfoType.Generic, "M.B2.1")));
            var generic = ProductMissionPlanner.Plan(Inputs(FoodInfoType.Generic, "M.A6.5"));
            Assert.AreEqual(ProductMissionPlanner.GenericFoodIdField, generic.Single().IdField);
            Assert.IsEmpty(ProductMissionPlanner.Plan(Inputs(FoodInfoType.Product, "M.A6.5")));
        }

        [Test]
        public void NoFoodId_NoCards()
        {
            var inputs = Inputs(FoodInfoType.Product, "M.B2.1");
            inputs.FoodId = "";
            Assert.IsEmpty(ProductMissionPlanner.Plan(inputs));
        }

        [Test]
        public void Checklist_Options_LockedAndSuggested()
        {
            var inputs = Inputs(FoodInfoType.Product, "M.A2.1"); // origin required + 2 of 4
            inputs.Sent[ClientEventTypes.ShoppingOriginChecked] = new Dictionary<string, DateTime> { { "", DateTime.Now } };
            inputs.AvailableKeys = new[] { "META_LABELS" };

            ProductMissionCard card = ProductMissionPlanner.Plan(inputs).Single();

            Assert.AreEqual(ProductMissionCardKind.Checklist, card.Kind);
            int origin = card.Options.ToList().FindIndex(o => o.EventType == ClientEventTypes.ShoppingOriginChecked);
            int cert = card.Options.ToList().FindIndex(o => o.EventType == ClientEventTypes.ShoppingCertificationChosen);
            CollectionAssert.AreEqual(new[] { origin }, card.Locked.ToArray());
            CollectionAssert.Contains(card.Suggested.ToArray(), cert);
            Assert.IsFalse(card.AllSent);
        }

        [Test]
        public void ArrayChecklist_LocksSentValues()
        {
            var inputs = Inputs(FoodInfoType.Product, "M.A2.2");
            inputs.Sent[ClientEventTypes.ShoppingDoubleLabelChosen] = new Dictionary<string, DateTime> { { "EU_ORGANIC", DateTime.Now } };

            ProductMissionCard card = ProductMissionPlanner.Plan(inputs).Single();

            Assert.AreEqual(1, card.Locked.Count);
            Assert.AreEqual("EU_ORGANIC", card.Options[card.Locked.Single()].Value);
        }

        [Test]
        public void SharedEvent_LockedInBothMissions()
        {
            var inputs = Inputs(FoodInfoType.Product, "M.B2.5", "M.A4.5"); // both SHOPPING_PACKAGING_INFO_CHECKED
            inputs.Sent[ClientEventTypes.ShoppingPackagingInfoChecked] = new Dictionary<string, DateTime> { { "", DateTime.Now } };

            var cards = ProductMissionPlanner.Plan(inputs);

            Assert.AreEqual(2, cards.Count);
            Assert.IsTrue(cards.All(c => c.AllSent));
        }

        [Test]
        public void FocusOf_MapsEventTypes()
        {
            Assert.AreEqual(ProductFocus.Origin, ProductMissionPlanner.FocusOf(ClientEventTypes.ShoppingOriginChecked));
            Assert.AreEqual(ProductFocus.Nova, ProductMissionPlanner.FocusOf(ClientEventTypes.ProcessingNovaChecked));
            Assert.AreEqual(ProductFocus.Ingredients, ProductMissionPlanner.FocusOf(ClientEventTypes.ProcessingIngredientsReviewed));
            Assert.AreEqual(ProductFocus.Labels, ProductMissionPlanner.FocusOf(ClientEventTypes.ShoppingCertificationChosen));
            Assert.AreEqual(ProductFocus.None, ProductMissionPlanner.FocusOf(ClientEventTypes.PackagingMaterialObserved));
        }

        [Test]
        public void SentBeforeTheMissionStart_IsNotLocked()
        {
            // Rules only count events since the mission started: an older report must not lock the option
            var inputs = Inputs(FoodInfoType.Product, "M.B2.1"); // started yesterday
            inputs.Sent[ClientEventTypes.ShoppingOriginChecked] = new Dictionary<string, DateTime> { { "", DateTime.Now.AddDays(-5) } };

            Assert.IsFalse(ProductMissionPlanner.Plan(inputs).Single().AllSent);
        }

        [Test]
        public void Cards_CarryFoodIdAndMissionStart()
        {
            ProductMissionCard card = ProductMissionPlanner.Plan(Inputs(FoodInfoType.Product, "M.B2.1")).Single();

            Assert.AreEqual("p-1", card.FoodId);
            Assert.IsNotNull(card.MissionStartLocal);
        }

        [Test]
        public void MissionPastItsDeadline_GivesNoCard()
        {
            // Same rule as the check-in: past the deadline nothing counts (the backend fails it)
            var inputs = new ProductMissionInputs { FoodType = FoodInfoType.Product, FoodId = "p-1", NowLocal = DateTime.Now };
            inputs.Missions.Add(new CheckInMission("M.B2.1", "Origin", MissionInteractionCatalog.Get("M.B2.1"), DateTime.Now.AddDays(-60)));

            Assert.IsEmpty(ProductMissionPlanner.Plan(inputs));
        }
    }
}
