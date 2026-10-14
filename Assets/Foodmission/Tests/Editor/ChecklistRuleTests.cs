using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChecklistRuleTests
    {
        private static MissionStepOption EventOption(string eventType) =>
            new MissionStepOption("MISSION_OPT_" + eventType, eventType, null);

        private static MissionStepOption ValueOption(string value) =>
            new MissionStepOption("MISSION_OPT_COLOUR_" + value, null, new Dictionary<string, object> { { ChecklistSpec.ValueKey, value } });

        // Origin required + at least 2 of 4 (M.A2.1 shape)
        private static readonly ChecklistSpec OriginPlusOne = new ChecklistSpec(
            new[] { "ORIGIN", "CERT", "METHOD", "SEASON" }.Select(EventOption).ToArray(), null, 2, new[] { 0 });

        private static readonly ChecklistSpec Colours = new ChecklistSpec(
            new[] { "GREEN", "RED", "ORANGE", "YELLOW", "PURPLE", "WHITE" }.Select(ValueOption).ToArray(), "colours", 5, null);

        [Test]
        public void Counts_NeedsMinDistinctAndRequired()
        {
            Assert.IsTrue(ChecklistRule.Counts(OriginPlusOne, new[] { 0, 2 }));
            Assert.IsFalse(ChecklistRule.Counts(OriginPlusOne, new[] { 1, 2 }), "origin missing");
            Assert.IsFalse(ChecklistRule.Counts(OriginPlusOne, new[] { 0 }), "only one");
            Assert.IsFalse(ChecklistRule.Counts(OriginPlusOne, new int[0]));
        }

        [Test]
        public void Missing_ReportsCountAndRequired()
        {
            Assert.AreEqual(1, ChecklistRule.MissingCount(OriginPlusOne, new[] { 1 }));
            CollectionAssert.AreEqual(new[] { 0 }, ChecklistRule.MissingRequired(OriginPlusOne, new[] { 1 }).ToArray());
            Assert.AreEqual(0, ChecklistRule.MissingCount(OriginPlusOne, new[] { 1, 2 }));
        }

        [Test]
        public void OutOfRangeAndRepeatedIndexes_DoNotCount()
        {
            Assert.AreEqual(1, ChecklistRule.MissingCount(OriginPlusOne, new[] { 0, 0, 7, -1 }));
        }

        [Test]
        public void IndexesOf_MatchesValuesCaseInsensitively()
        {
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, ChecklistRule.IndexesOf(Colours, new[] { "green", "RED", "BLUE" }).ToArray());
        }

        [Test]
        public void ValuesOf_ReturnsValuesInOptionOrder()
        {
            CollectionAssert.AreEqual(new[] { "GREEN", "YELLOW" }, ChecklistRule.ValuesOf(Colours, new[] { 3, 0, 9 }));
        }

        [Test]
        public void Spec_ArrayMode_IsArray_AndValueOf()
        {
            Assert.IsTrue(Colours.IsArray);
            Assert.AreEqual("PURPLE", Colours.ValueOf(4));
            Assert.IsFalse(OriginPlusOne.IsArray);
            Assert.IsNull(OriginPlusOne.ValueOf(0));
            Assert.AreEqual(0, Colours.Required.Count);
        }
    }
}
