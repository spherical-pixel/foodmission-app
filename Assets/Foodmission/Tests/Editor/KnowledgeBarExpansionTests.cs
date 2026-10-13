using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeBarExpansionTests
    {
        private static readonly KnowledgeItem[] Items =
        {
            new KnowledgeItem { contentType = QuestContentType.Mission, contentCode = "M1", finished = false },
            new KnowledgeItem { contentType = QuestContentType.Quiz, contentCode = "Q1", finished = false },
            new KnowledgeItem { contentType = QuestContentType.FoodFact, contentCode = "F1", finished = false },
        };

        [SetUp]
        public void SetUp()
        {
            KnowledgeBarExpansion.Clear();
        }

        [Test]
        public void NothingRemembered_ExpandsFirstWithOpenItems()
        {
            var sections = KnowledgeBarSections.Build(Items);

            CollectionAssert.AreEqual(new[] { 0 }, KnowledgeBarExpansion.ExpandedIndices(KnowledgeKind.Health, sections).ToArray());
        }

        [Test]
        public void Remembered_RestoresTheSectionsTheUserOpened()
        {
            var sections = KnowledgeBarSections.Build(Items);
            KnowledgeBarExpansion.Remember(KnowledgeKind.Health, QuestContentType.Mission, false);
            KnowledgeBarExpansion.Remember(KnowledgeKind.Health, QuestContentType.FoodFact, true);

            CollectionAssert.AreEqual(new[] { 2 }, KnowledgeBarExpansion.ExpandedIndices(KnowledgeKind.Health, sections).ToArray());
        }

        [Test]
        public void Remembered_AllCollapsed_StaysCollapsed()
        {
            var sections = KnowledgeBarSections.Build(Items);
            KnowledgeBarExpansion.Remember(KnowledgeKind.Health, QuestContentType.Mission, false);

            Assert.AreEqual(0, KnowledgeBarExpansion.ExpandedIndices(KnowledgeKind.Health, sections).Count());
        }

        [Test]
        public void Remembered_IsPerKind()
        {
            var sections = KnowledgeBarSections.Build(Items);
            KnowledgeBarExpansion.Remember(KnowledgeKind.Health, QuestContentType.Quiz, true);

            CollectionAssert.AreEqual(new[] { 0 }, KnowledgeBarExpansion.ExpandedIndices(KnowledgeKind.FoodWaste, sections).ToArray());
        }
    }
}
