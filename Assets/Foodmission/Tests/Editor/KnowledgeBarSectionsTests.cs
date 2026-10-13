using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeBarSectionsTests
    {
        private static KnowledgeItem Item(string type, string code, bool finished, string title = "t") =>
            new KnowledgeItem { contentType = type, contentCode = code, finished = finished, title = title };

        [Test]
        public void Build_GroupsInFixedOrder_SkippingEmptyTypes()
        {
            var sections = KnowledgeBarSections.Build(new[]
            {
                Item(QuestContentType.FoodFact, "F1", false),
                Item(QuestContentType.Mission, "M1", true),
                Item(QuestContentType.Quiz, "Q1", false),
            });

            CollectionAssert.AreEqual(
                new[] { QuestContentType.Mission, QuestContentType.Quiz, QuestContentType.FoodFact },
                sections.Select(s => s.ContentType).ToArray());
        }

        [Test]
        public void Build_OpenRowsFirst_KeepingBackendOrder()
        {
            var section = KnowledgeBarSections.Build(new[]
            {
                Item(QuestContentType.Quiz, "Q1", true),
                Item(QuestContentType.Quiz, "Q2", false),
                Item(QuestContentType.Quiz, "Q3", true),
                Item(QuestContentType.Quiz, "Q4", false),
            }).Single();

            CollectionAssert.AreEqual(new[] { "Q2", "Q4", "Q1", "Q3" }, section.Rows.Select(r => r.Code).ToArray());
            Assert.AreEqual(2, section.Finished);
            Assert.AreEqual(4, section.Total);
        }

        [Test]
        public void Build_OnlyQuizzesAndFactsCanOpen()
        {
            var rows = KnowledgeBarSections.Build(new[]
            {
                Item(QuestContentType.Mission, "M1", false),
                Item(QuestContentType.Challenge, "C1", false),
                Item(QuestContentType.Quiz, "Q1", false),
                Item(QuestContentType.FoodFact, "F1", false),
            }).SelectMany(s => s.Rows).ToDictionary(r => r.Code, r => r.CanOpen);

            Assert.IsFalse(rows["M1"]);
            Assert.IsFalse(rows["C1"]);
            Assert.IsTrue(rows["Q1"]);
            Assert.IsTrue(rows["F1"]);
        }

        [Test]
        public void Build_EmptyTitle_UsesCode_AndUnknownTypesAreSkipped()
        {
            var sections = KnowledgeBarSections.Build(new[]
            {
                Item(QuestContentType.Quiz, "Q1", false, title: ""),
                Item(QuestContentType.MicroLearning, "ML1", false),
                null,
            });

            Assert.AreEqual(1, sections.Count);
            Assert.AreEqual("Q1", sections[0].Rows[0].Title);
        }

        [Test]
        public void Build_NullItems_ReturnsEmpty()
        {
            Assert.AreEqual(0, KnowledgeBarSections.Build(null).Count);
        }

        [Test]
        public void FirstExpandedIndex_FirstSectionWithOpenRows()
        {
            var sections = KnowledgeBarSections.Build(new[]
            {
                Item(QuestContentType.Mission, "M1", true),
                Item(QuestContentType.Quiz, "Q1", false),
            });

            Assert.AreEqual(1, KnowledgeBarSections.FirstExpandedIndex(sections));
            Assert.AreEqual(0, KnowledgeBarSections.FirstExpandedIndex(KnowledgeBarSections.Build(new[] { Item(QuestContentType.Mission, "M1", true) })));
            Assert.AreEqual(-1, KnowledgeBarSections.FirstExpandedIndex(KnowledgeBarSections.Build(null)));
        }

        [Test]
        public void KnowledgeBarText_KeysForEveryKind()
        {
            foreach (string kind in KnowledgeKind.All)
            {
                Assert.IsNotNull(KnowledgeBarText.NameKey(kind));
                Assert.IsNotEmpty(KnowledgeBarText.Emoji(kind));
            }
            Assert.IsNull(KnowledgeBarText.NameKey("X"));
        }
    }
}
