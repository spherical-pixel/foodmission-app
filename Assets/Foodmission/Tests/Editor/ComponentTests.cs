using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMDialogActionTests
    {
        [Test]
        public void Constructor_SetsLabel()
        {
            var action = new FMDialogAction("OK", null, true);

            Assert.AreEqual("OK", action.Label);
            Assert.IsNull(action.Callback);
            Assert.IsTrue(action.IsPrimary);
        }

        [Test]
        public void Constructor_WithDefaultIsPrimary_SetsFalse()
        {
            var action = new FMDialogAction("Cancel", null);

            Assert.AreEqual("Cancel", action.Label);
            Assert.IsNull(action.Callback);
            Assert.IsFalse(action.IsPrimary);
        }
    }

    [TestFixture]
    public class FMDialogShowInfoTests
    {
        [Test]
        public void ShowInfo_ThrowsWhenActionsNull()
        {
            Assert.Throws<ArgumentException>(() =>
                FMDialog.ShowInfo(null, "title", "body", null));
        }

        [Test]
        public void ShowInfo_ThrowsWhenActionsEmpty()
        {
            Assert.Throws<ArgumentException>(() =>
                FMDialog.ShowInfo(null, "title", "body", new FMDialogAction[0]));
        }
    }

    [TestFixture]
    public class CategoryEmojisTests
    {
        [Test]
        public void CategoryEmojis_ContainsExpectedKeys()
        {
            var field = typeof(FMSearchOrCategoryField).GetField("CategoryEmojis",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "CategoryEmojis field should exist");

            var dict = field.GetValue(null) as Dictionary<string, string>;
            Assert.IsNotNull(dict);

            Assert.IsTrue(dict.ContainsKey("Fruits"));
            Assert.IsTrue(dict.ContainsKey("Vegetables"));
            Assert.IsTrue(dict.ContainsKey("Bread"));
            Assert.IsTrue(dict.ContainsKey("Eggs"));
            Assert.IsTrue(dict.ContainsKey("Cheese"));
            Assert.IsTrue(dict.ContainsKey("Meat and poultry"));
            Assert.IsTrue(dict.ContainsKey("Fish, crustacean and shellfish"));
            Assert.IsTrue(dict.ContainsKey("Legumes"));
            Assert.IsTrue(dict.ContainsKey("Nuts and seeds"));
            Assert.IsTrue(dict.ContainsKey("Milk and milk products"));
            Assert.IsTrue(dict.ContainsKey("Fats and oils"));
            Assert.IsTrue(dict.ContainsKey("Non-alcoholic beverages"));
            Assert.IsTrue(dict.ContainsKey("Alcoholic beverages"));
            Assert.IsTrue(dict.ContainsKey("Soups"));
            Assert.IsTrue(dict.ContainsKey("Savoury sauces"));
            Assert.IsTrue(dict.ContainsKey("Savoury snacks"));
            Assert.IsTrue(dict.ContainsKey("Herbs and spices"));
            Assert.IsTrue(dict.ContainsKey("Cereal products and types of flour"));
            Assert.IsTrue(dict.ContainsKey("Potatoes and tubers"));
            Assert.IsTrue(dict.ContainsKey("Sugar, sweets and sweet sauces"));
            Assert.IsTrue(dict.ContainsKey("Pastry and biscuits"));
            Assert.IsTrue(dict.ContainsKey("Miscellaneous foods"));
            Assert.IsTrue(dict.ContainsKey("Mixed dishes"));
            Assert.AreEqual(27, dict.Count);
        }
    }
}
