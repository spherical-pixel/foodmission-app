using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeBarsTests
    {
        private static AppState StateWith(params KnowledgeProgress[] bars) => new AppState { knowledgeProgress = bars };

        [Test]
        public void Bars_ValueIsPercentOverHundred()
        {
            AppState state = StateWith(new KnowledgeProgress { kind = KnowledgeKind.Health, percentComplete = 35f });

            Assert.AreEqual(0.35f, KnowledgeBars.Value(state, KnowledgeKind.Health), 0.0001f);
        }

        [Test]
        public void Bars_MissingAndUnknownKinds()
        {
            AppState state = StateWith(new KnowledgeProgress { kind = "FOOD_AND_WASTE", percentComplete = 80f }, null);

            Assert.AreEqual(0f, KnowledgeBars.Value(state, KnowledgeKind.FoodWaste));
            Assert.AreEqual(0f, KnowledgeBars.Value(null, KnowledgeKind.Health));
        }

        [Test]
        public void Bars_ClampsPercent()
        {
            AppState state = StateWith(
                new KnowledgeProgress { kind = KnowledgeKind.Health, percentComplete = 120f },
                new KnowledgeProgress { kind = KnowledgeKind.FoodChoices, percentComplete = -5f });

            Assert.AreEqual(1f, KnowledgeBars.Value(state, KnowledgeKind.Health));
            Assert.AreEqual(0f, KnowledgeBars.Value(state, KnowledgeKind.FoodChoices));
        }

        [Test]
        public void Signature_ChangesWithValues_NotWithUnrelatedState()
        {
            AppState a = StateWith(new KnowledgeProgress { kind = KnowledgeKind.Health, finishedItems = 1, totalItems = 10, percentComplete = 10f });
            AppState b = a.Copy();
            b.userXp = 500;
            AppState c = a.Copy();
            c.knowledgeProgress[0].finishedItems = 2;
            c.knowledgeProgress[0].percentComplete = 20f;

            Assert.AreEqual(KnowledgeBars.Signature(a), KnowledgeBars.Signature(b));
            Assert.AreNotEqual(KnowledgeBars.Signature(a), KnowledgeBars.Signature(c));
        }
    }
}
