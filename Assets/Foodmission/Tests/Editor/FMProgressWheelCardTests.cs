using System.Collections.Generic;

using NUnit.Framework;

using Unity.AppUI.UI;

using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMProgressWheelCardTests
    {
        private static Icon InfoIcon(FMProgressWheelCard card)
        {
            return card.Q<Icon>(className: FMProgressWheelCard.InfoIconUssClassName);
        }

        [Test]
        public void Card_HasInfoIcon_WhenInteractive()
        {
            var card = new FMProgressWheelCard();
            card.Bind(new ProgressWheel { kind = "CO2_REDUCTION" });

            Icon icon = InfoIcon(card);
            Assert.IsNotNull(icon);
            Assert.AreEqual("info", icon.iconName);
            Assert.IsTrue(card.Interactive);
            Assert.IsFalse(icon.ClassListContains(FMProgressWheelCard.InfoIconHiddenUssClassName));
            Assert.IsTrue(card.ClassListContains(FMProgressWheelCard.ClickableUssClassName));

            card.Interactive = false;

            Assert.IsTrue(icon.ClassListContains(FMProgressWheelCard.InfoIconHiddenUssClassName));
            Assert.IsFalse(card.ClassListContains(FMProgressWheelCard.ClickableUssClassName));
        }

        [Test]
        public void Grid_RaisesWheelClicked_WithCardKind()
        {
            var grid = new FMProgressWheelGrid();
            var clicked = new List<string>();
            grid.WheelClicked += kind => clicked.Add(kind);

            grid.SetWheels(new[]
            {
                new ProgressWheel { kind = "CO2_REDUCTION" },
                new ProgressWheel { kind = "LAND_USE_REDUCTION" },
            });
            grid.Cards[1].Activate();

            CollectionAssert.AreEqual(new[] { "LAND_USE_REDUCTION" }, clicked);
        }

        [Test]
        public void Grid_ReusedCards_RaiseOnce()
        {
            var grid = new FMProgressWheelGrid();
            int count = 0;
            grid.WheelClicked += _ => count++;

            grid.SetWheels(new[] { new ProgressWheel { kind = "CO2_REDUCTION" } });
            grid.SetWheels(new[] { new ProgressWheel { kind = "WATER_SAVINGS" } });
            grid.Cards[0].Activate();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Card_UnknownKind_IsNotTappable()
        {
            var card = new FMProgressWheelCard();
            int count = 0;
            card.clicked += _ => count++;

            card.Bind(new ProgressWheel { kind = "SOMETHING_NEW" });
            card.Activate();

            Assert.AreEqual(0, count);
            Assert.IsFalse(card.CanOpen);
            Assert.IsTrue(InfoIcon(card).ClassListContains(FMProgressWheelCard.InfoIconHiddenUssClassName));
            Assert.IsFalse(card.ClassListContains(FMProgressWheelCard.ClickableUssClassName));
        }

        [Test]
        public void Card_KnownKind_CanOpen_UnlessHostDisablesIt()
        {
            var card = new FMProgressWheelCard();
            card.Bind(new ProgressWheel { kind = "WATER_SAVINGS" });
            Assert.IsTrue(card.CanOpen);

            card.Interactive = false;
            card.Bind(new ProgressWheel { kind = "CO2_REDUCTION" });

            Assert.IsFalse(card.CanOpen);
            Assert.IsTrue(InfoIcon(card).ClassListContains(FMProgressWheelCard.InfoIconHiddenUssClassName));
        }

        [Test]
        public void Card_NotInteractive_DoesNotRaise()
        {
            var card = new FMProgressWheelCard();
            card.Bind(new ProgressWheel { kind = "CO2_REDUCTION" });
            int count = 0;
            card.clicked += _ => count++;

            card.Interactive = false;
            card.Activate();

            Assert.AreEqual(0, count);
        }
    }
}
