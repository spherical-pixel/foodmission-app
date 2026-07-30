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

            Assert.IsTrue(dict.ContainsKey("alcoholic-beverages"));
            Assert.IsTrue(dict.ContainsKey("vegetables"));
            Assert.IsTrue(dict.ContainsKey("potatoes-and-tubers"));

            Assert.AreEqual("🍺", FMSearchOrCategoryField.GetCategoryEmoji("alcoholic-beverages"));
            Assert.AreEqual("🥦", FMSearchOrCategoryField.GetCategoryEmoji("Vegetables"));
            Assert.AreEqual("🥔", FMSearchOrCategoryField.GetCategoryEmoji("potatoes-and-tubers"));
            Assert.AreEqual("🥛", FMSearchOrCategoryField.GetCategoryEmoji("Milk and milk products"));
        }
    }
}
