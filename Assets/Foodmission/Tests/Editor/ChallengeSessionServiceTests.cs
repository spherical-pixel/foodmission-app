using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeSessionServiceTests
    {
        private Mock<IChallengeCompletionService> _completion;
        private ChallengeSessionService _session;

        [SetUp]
        public void SetUp()
        {
            _completion = new Mock<IChallengeCompletionService>();
            _completion.Setup(c => c.CompleteAsync(It.IsAny<string>()))
                .ReturnsAsync(new ChallengeCompletionResult { Success = true, Reward = new ContentReward { xp = 5 } });
            _session = new ChallengeSessionService(_completion.Object);
        }

        [Test]
        public async Task ReportAsync_CountsDistinctProductsOnly()
        {
            _session.Begin("CH.B6.2", ChallengeInteractionCatalog.Get("CH.B6.2")); // ProductViewed x2

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");
            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");
            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Generic:7");
            _completion.Verify(c => c.CompleteAsync("CH.B6.2"), Times.Once);
            Assert.IsNull(_session.ActiveChallengeCode, "Session ends after completion");
        }

        [Test]
        public async Task ReportAsync_WithoutActiveSession_IsNoOp()
        {
            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");

            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ReportAsync_OtherTrigger_IsIgnored()
        {
            _session.Begin("CH.B2.1", ChallengeInteractionCatalog.Get("CH.B2.1")); // ProductViewed x1

            await _session.ReportAsync(ChallengeCompletionTrigger.RecipeViewed, "recipe-1");

            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);
            Assert.AreEqual("CH.B2.1", _session.ActiveChallengeCode);
        }

        [Test]
        public async Task ReportAsync_FoodFact_OnlyTheConfiguredFactCounts()
        {
            _session.Begin("CH.A1.4", ChallengeInteractionCatalog.Get("CH.A1.4")); // FF1.2.8

            await _session.ReportAsync(ChallengeCompletionTrigger.FoodFactRead, "FF1.1.1");
            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);

            await _session.ReportAsync(ChallengeCompletionTrigger.FoodFactRead, "ff1.2.8");
            _completion.Verify(c => c.CompleteAsync("CH.A1.4"), Times.Once);
        }

        [Test]
        public void Begin_ManualOrComparatorInteraction_DoesNotStartSession()
        {
            _session.Begin("CH.A4.1", ChallengeInteractionCatalog.Get("CH.A4.1")); // Confirm
            Assert.IsNull(_session.ActiveChallengeCode);

            _session.Begin("CH.B1.1", ChallengeInteractionCatalog.Get("CH.B1.1")); // Comparator completes itself
            Assert.IsNull(_session.ActiveChallengeCode);
        }

        [Test]
        public async Task Begin_NewChallenge_ReplacesPreviousSessionAndResetsCount()
        {
            _session.Begin("CH.B6.2", ChallengeInteractionCatalog.Get("CH.B6.2"));
            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");

            _session.Begin("CH.I1.2", ChallengeInteractionCatalog.Get("CH.I1.2")); // ProductViewed x2
            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:2");

            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);
            Assert.AreEqual("CH.I1.2", _session.ActiveChallengeCode);
        }

        [Test]
        public async Task ReportAsync_WhenCompletionFails_KeepsSessionForRetry()
        {
            _completion.Setup(c => c.CompleteAsync("CH.B2.1"))
                .ReturnsAsync(new ChallengeCompletionResult { Success = false, Error = new ApiErrorResponse { message = "down" } });
            _session.Begin("CH.B2.1", ChallengeInteractionCatalog.Get("CH.B2.1"));
            bool raised = false;
            _session.AutoCompleted += (_, __) => raised = true;

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");

            Assert.AreEqual("CH.B2.1", _session.ActiveChallengeCode);
            Assert.IsFalse(raised);
        }

        [Test]
        public async Task ReportAsync_WhenCompletionHasNoReward_RaisesAutoCompletedWithNullReward()
        {
            _completion.Setup(c => c.CompleteAsync("CH.B2.1")).ReturnsAsync(new ChallengeCompletionResult { Success = true, Reward = null });
            _session.Begin("CH.B2.1", ChallengeInteractionCatalog.Get("CH.B2.1"));
            string code = null;
            ContentReward reward = new ContentReward();
            _session.AutoCompleted += (c, r) => { code = c; reward = r; };

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");

            Assert.AreEqual("CH.B2.1", code);
            Assert.IsNull(reward);
        }

        [Test]
        public async Task Cancel_StopsCounting()
        {
            _session.Begin("CH.B2.1", ChallengeInteractionCatalog.Get("CH.B2.1"));
            _session.Cancel();

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");

            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Begin_KeepsTitleAndTask_AndRaisesSessionChanged()
        {
            int changes = 0;
            _session.SessionChanged += () => changes++;

            _session.Begin("CH.B6.2", ChallengeInteractionCatalog.Get("CH.B6.2"), "Sugar Detective", "Compare two foods");

            Assert.AreEqual("Sugar Detective", _session.ActiveChallengeTitle);
            Assert.AreEqual("Compare two foods", _session.ActiveChallengeTask);
            Assert.AreSame(ChallengeInteractionCatalog.Get("CH.B6.2"), _session.ActiveInteraction);
            Assert.AreEqual(0, _session.ReportedCount);
            Assert.GreaterOrEqual(changes, 1);
        }

        [Test]
        public async Task ReportAsync_ReturnsCountedThenCompleted_AndTracksReportedProducts()
        {
            _session.Begin("CH.B6.2", ChallengeInteractionCatalog.Get("CH.B6.2")); // ProductViewed x2

            Assert.AreEqual(ChallengeReportResult.Counted, await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1"));
            Assert.IsTrue(_session.HasReported("Product:1"));
            Assert.IsFalse(_session.HasReported("Product:2"));
            Assert.AreEqual(1, _session.ReportedCount);

            Assert.AreEqual(ChallengeReportResult.Completed, await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:2"));
            Assert.IsNull(_session.ActiveInteraction);
            Assert.AreEqual(0, _session.ReportedCount);
        }

        [Test]
        public async Task ReportAsync_WhenCompletionFails_ReturnsFailed_AndSameProductRetries()
        {
            var error = new ApiErrorResponse { message = "down" };
            _completion.SetupSequence(c => c.CompleteAsync("CH.B2.1"))
                .ReturnsAsync(new ChallengeCompletionResult { Success = false, Error = error })
                .ReturnsAsync(new ChallengeCompletionResult { Success = true });
            _session.Begin("CH.B2.1", ChallengeInteractionCatalog.Get("CH.B2.1")); // ProductViewed x1

            Assert.AreEqual(ChallengeReportResult.Failed, await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1"));
            Assert.AreSame(error, _session.LastError);
            Assert.IsTrue(_session.HasReported("Product:1"));

            Assert.AreEqual(ChallengeReportResult.Completed, await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1"));
        }

        [Test]
        public async Task ReportAsync_WithoutSessionOrOtherTrigger_ReturnsIgnored()
        {
            Assert.AreEqual(ChallengeReportResult.Ignored, await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1"));

            _session.Begin("CH.B2.1", ChallengeInteractionCatalog.Get("CH.B2.1"));
            Assert.AreEqual(ChallengeReportResult.Ignored, await _session.ReportAsync(ChallengeCompletionTrigger.RecipeViewed, "r-1"));
        }

        [Test]
        public async Task IsCompletionPending_StaysTrueAfterAFailedCompletion_SoTheProductCanRetryLater()
        {
            _completion.Setup(c => c.CompleteAsync("CH.B6.2"))
                .ReturnsAsync(new ChallengeCompletionResult { Success = false, Error = new ApiErrorResponse { message = "down" } });
            _session.Begin("CH.B6.2", ChallengeInteractionCatalog.Get("CH.B6.2")); // ProductViewed x2

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");
            Assert.IsFalse(_session.IsCompletionPending, "one of two: nothing to retry yet");

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:2");
            Assert.IsTrue(_session.IsCompletionPending, "count reached but not completed");

            _session.Cancel();
            Assert.IsFalse(_session.IsCompletionPending);
        }

        [Test]
        public async Task SessionChanged_IsRaisedOnReportAndCancel()
        {
            _session.Begin("CH.B6.2", ChallengeInteractionCatalog.Get("CH.B6.2"));
            int changes = 0;
            _session.SessionChanged += () => changes++;

            await _session.ReportAsync(ChallengeCompletionTrigger.ProductViewed, "Product:1");
            Assert.AreEqual(1, changes);

            _session.Cancel();
            Assert.AreEqual(2, changes);
        }
    }
}
