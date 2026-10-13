using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeProgressServiceTests
    {
        private TestStoreService _store;
        private Mock<IGamificationService> _gamification;
        private KnowledgeProgressService _service;

        private static KnowledgeProgress Bar(string kind, int finished = 1, int total = 10) => new KnowledgeProgress
        {
            kind = kind,
            finishedItems = finished,
            totalItems = total,
            percentComplete = total == 0 ? 0 : 100f * finished / total,
            items = new[] { new KnowledgeItem { contentCode = "X" } },
        };

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", accessToken = "t", tokenType = "Bearer" });
            _gamification = new Mock<IGamificationService>();
            _service = new KnowledgeProgressService(_store, _gamification.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
        }

        [Test]
        public async Task Refresh_StoresBars_WithoutItems()
        {
            _gamification.Setup(g => g.GetKnowledgeProgressAsync())
                .ReturnsAsync((new[] { Bar(KnowledgeKind.Health), Bar(KnowledgeKind.FoodWaste) }, (ApiErrorResponse)null));

            await _service.RefreshAsync();

            KnowledgeProgress[] cached = _store.GetAppState().knowledgeProgress;
            Assert.AreEqual(2, cached.Length);
            Assert.IsNull(cached[0].items);
            Assert.IsFalse(_service.IsLoading);
        }

        [Test]
        public async Task Refresh_Error_KeepsCache()
        {
            _store.store.Dispatch(AppActions.setKnowledgeProgress.Invoke(new[] { Bar(KnowledgeKind.Health) }));
            _gamification.Setup(g => g.GetKnowledgeProgressAsync())
                .ReturnsAsync(((KnowledgeProgress[])null, new ApiErrorResponse { statusCode = 500 }));

            await _service.RefreshAsync();

            Assert.AreEqual(1, _store.GetAppState().knowledgeProgress.Length);
        }

        [Test]
        public async Task Refresh_NoToken_DoesNothing()
        {
            _store.SetAppState(new AppState { userId = "u1", accessToken = "" });

            await _service.RefreshAsync();

            _gamification.Verify(g => g.GetKnowledgeProgressAsync(), Times.Never);
        }

        [Test]
        public async Task Refresh_ResultAfterLogout_IsDropped()
        {
            var pending = new TaskCompletionSource<(KnowledgeProgress[], ApiErrorResponse)>();
            _gamification.Setup(g => g.GetKnowledgeProgressAsync()).Returns(pending.Task);

            Task refresh = _service.RefreshAsync();
            _store.SetAppState(new AppState { userId = "u2", accessToken = "t2" });
            pending.SetResult((new[] { Bar(KnowledgeKind.Health) }, null));
            await refresh;

            Assert.AreEqual(0, _store.GetAppState().knowledgeProgress.Length);
        }

        [Test]
        public async Task Refresh_ConcurrentCalls_ShareOneRequest()
        {
            var pending = new TaskCompletionSource<(KnowledgeProgress[], ApiErrorResponse)>();
            _gamification.Setup(g => g.GetKnowledgeProgressAsync()).Returns(pending.Task);

            Task first = _service.RefreshAsync();
            Task second = _service.RefreshAsync();
            Assert.IsTrue(_service.IsLoading);
            pending.SetResult((new KnowledgeProgress[0], null));
            await Task.WhenAll(first, second);

            _gamification.Verify(g => g.GetKnowledgeProgressAsync(), Times.Once);
        }
    
        [Test]
        public async Task Refresh_ResultAfterUserSwitch_IsDropped()
        {
            var pending = new TaskCompletionSource<(KnowledgeProgress[], ApiErrorResponse)>();
            _gamification.Setup(g => g.GetKnowledgeProgressAsync()).Returns(pending.Task);

            Task refresh = _service.RefreshAsync();
            _store.SetAppState(new AppState { userId = "u2", accessToken = "t" });
            pending.SetResult((new[] { Bar(KnowledgeKind.Health) }, null));
            await refresh;

            Assert.AreEqual(0, _store.GetAppState().knowledgeProgress.Length);
        }
    }
}
