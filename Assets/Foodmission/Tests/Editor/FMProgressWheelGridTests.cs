using System.Collections.Generic;

using NUnit.Framework;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMProgressWheelGridTests
    {
        [TestCase(1000f, 2, 420f, false, 2)]
        [TestCase(700f, 2, 420f, false, 1)]
        [TestCase(1000f, 2, 420f, true, 1)]
        [TestCase(0f, 2, 420f, false, 2)]      // not laid out yet: keep requested
        [TestCase(float.NaN, 2, 420f, false, 2)]
        [TestCase(1000f, 0, 420f, false, 1)]
        [TestCase(1300f, 3, 420f, false, 3)]
        [TestCase(1000f, 3, 420f, false, 2)]
        public void ResolveColumns(float width, int columns, float min, bool force, int expected)
        {
            Assert.AreEqual(expected, FMProgressWheelGrid.ResolveColumns(width, columns, min, force));
        }

        [Test]
        public void ResolveColumns_ForceSingleColumn()
        {
            Assert.AreEqual(1, FMProgressWheelGrid.ResolveColumns(1080f, 2, 420f, true));
        }

        [Test]
        public void SetWheels_ReusesCardsAndRemovesExtras()
        {
            var grid = new FMProgressWheelGrid();
            grid.SetWheels(new List<ProgressWheel> { W("CO2_REDUCTION"), W("WATER_SAVINGS"), W("LAND_USE_REDUCTION") });
            FMProgressWheelCard first = grid.Cards[0];

            grid.SetWheels(new List<ProgressWheel> { W("ENERGY_REDUCTION"), W("WATER_SAVINGS") });

            Assert.AreEqual(2, grid.Cards.Count);
            Assert.AreEqual(2, grid.childCount);
            Assert.AreSame(first, grid.Cards[0]);
            Assert.AreEqual("ENERGY_REDUCTION", grid.Cards[0].Kind);
            Assert.IsTrue(grid.Cards[0].ClassListContains("fm-wheel-card--energy"));
            Assert.IsFalse(grid.Cards[0].ClassListContains("fm-wheel-card--co2"));
        }

        [Test]
        public void ForceSingleColumn_MakesCardsHorizontal()
        {
            var grid = new FMProgressWheelGrid();
            grid.SetWheels(new List<ProgressWheel> { W("CO2_REDUCTION") });

            grid.ForceSingleColumn = true;

            Assert.AreEqual(1, grid.EffectiveColumns);
            Assert.IsTrue(grid.ClassListContains("fm-wheel-grid--cols-1"));
            Assert.IsTrue(grid.Cards[0].ClassListContains("fm-wheel-card--horizontal"));

            grid.ForceSingleColumn = false;

            Assert.AreEqual(2, grid.EffectiveColumns);
            Assert.IsTrue(grid.ClassListContains("fm-wheel-grid--cols-2"));
            Assert.IsFalse(grid.Cards[0].ClassListContains("fm-wheel-card--horizontal"));
        }

        private static ProgressWheel W(string kind) => new ProgressWheel { kind = kind, stage = 1, profile = "BEGINNER", percentComplete = 20f };
    }
}
