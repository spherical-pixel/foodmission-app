using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeCompletionServiceTests
    {
        private Mock<IChallengeService> _challengeService;
        private Mock<IAuthService> _authService;
        private TestStoreService _storeService;
        private ChallengeCompletionService _service;

        [SetUp]
        public void SetUp()
        {
            _challengeService = new Mock<IChallengeService>();
            _authService = new Mock<IAuthService>();
            _storeService = new TestStoreService();
            _service = new ChallengeCompletionService(_challengeService.Object, _authService.Object, _storeService);
        }

        [TearDown]
        public void TearDown()
        {
            _storeService.Dispose();
        }

        private void SetupPatch(ChallengeProgress progress, ApiErrorResponse error)
        {
            _challengeService.Setup(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null))
                .ReturnsAsync((progress, error));
        }

        [Test]
        public async Task CompleteAsync_OnSuccess_MarksCompletedReturnsRewardAndRaisesEvent()
        {
            var reward = new ContentReward { xp = 10, points = 5 };
            SetupPatch(new ChallengeProgress { completed = true, progress = 100f, reward = reward }, null);
            string raisedCode = null;
            ContentReward raisedReward = null;
            _service.ChallengeCompleted += (code, r) => { raisedCode = code; raisedReward = r; };

            var result = await _service.CompleteAsync("CH.B2.1");

            Assert.IsTrue(result.Success);
            Assert.AreSame(reward, result.Reward);
            Assert.IsTrue(_service.IsCompleted("CH.B2.1"));
            Assert.AreEqual("CH.B2.1", raisedCode);
            Assert.AreSame(reward, raisedReward);
            _authService.Verify(a => a.SyncGamificationAsync(), Times.Once);
        }

        [Test]
        public async Task CompleteAsync_WithReward_AddsItToTheWalletBeforeRaisingEvent()
        {
            var state = new AppState { userXp = 500, userPoints = 40 };
            _storeService.SetAppState(state);
            SetupPatch(new ChallengeProgress { completed = true, progress = 100f, reward = new ContentReward { xp = 10, points = 5 } }, null);
            int xpWhenRaised = -1;
            _service.ChallengeCompleted += (_, _) => xpWhenRaised = _storeService.GetAppState().userXp;

            await _service.CompleteAsync("CH.B2.1");

            Assert.AreEqual(510, xpWhenRaised);
            Assert.AreEqual(45, _storeService.GetAppState().userPoints);
        }

        [Test]
        public async Task CompleteAsync_WhenApiFails_StaysIncompleteAndCanRetry()
        {
            var error = new ApiErrorResponse { statusCode = 503, message = "down" };
            _challengeService.SetupSequence(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null))
                .ReturnsAsync(((ChallengeProgress)null, error))
                .ReturnsAsync((new ChallengeProgress { completed = true, progress = 100f }, (ApiErrorResponse)null));

            var first = await _service.CompleteAsync("CH.B2.1");
            Assert.IsFalse(first.Success);
            Assert.AreSame(error, first.Error);
            Assert.IsFalse(_service.IsCompleted("CH.B2.1"));

            var second = await _service.CompleteAsync("CH.B2.1");
            Assert.IsTrue(second.Success);
            _challengeService.Verify(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null), Times.Exactly(2));
        }

        [Test]
        public async Task CompleteAsync_ConcurrentCalls_ShareOneRequest()
        {
            var pending = new TaskCompletionSource<(ChallengeProgress, ApiErrorResponse)>();
            _challengeService.Setup(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null)).Returns(pending.Task);

            var a = _service.CompleteAsync("CH.B2.1");
            var b = _service.CompleteAsync("CH.B2.1");
            pending.SetResult((new ChallengeProgress { completed = true, progress = 100f }, null));
            await Task.WhenAll(a, b);

            Assert.IsTrue(a.Result.Success);
            Assert.IsTrue(b.Result.Success);
            _challengeService.Verify(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null), Times.Once);
        }

        [Test]
        public async Task CompleteAsync_WhenAlreadyCompletedInSession_DoesNotPatchAgain()
        {
            SetupPatch(new ChallengeProgress { completed = true, progress = 100f }, null);
            await _service.CompleteAsync("CH.B2.1");

            var again = await _service.CompleteAsync(" ch.b2.1 ");

            Assert.IsTrue(again.Success);
            Assert.IsNull(again.Reward);
            _challengeService.Verify(c => c.UpdateChallengeProgressAsync(It.IsAny<string>(), true, 100f, null), Times.Once);
        }

        [Test]
        public async Task CompleteAsync_WithoutRewardInResponse_ReturnsNullRewardAndSkipsSync()
        {
            SetupPatch(new ChallengeProgress { completed = true, progress = 100f, reward = null }, null);

            var result = await _service.CompleteAsync("CH.B2.1");

            Assert.IsTrue(result.Success);
            Assert.IsNull(result.Reward);
            _authService.Verify(a => a.SyncGamificationAsync(), Times.Never);
            Assert.AreEqual(0, _storeService.GetAppState().userXp);
        }

        [Test]
        public async Task CompleteAsync_EmptyCode_FailsWithoutCallingApi()
        {
            var result = await _service.CompleteAsync("  ");

            Assert.IsFalse(result.Success);
            _challengeService.Verify(c => c.UpdateChallengeProgressAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<float?>(), It.IsAny<string>()), Times.Never);
        }
            [Test]
        public async Task Reset_AfterLogout_NextUserCanCompleteTheSameChallenge()
        {
            SetupPatch(new ChallengeProgress { completed = true, progress = 100f }, null);
            await _service.CompleteAsync("CH.B2.1");

            _service.Reset();

            Assert.IsFalse(_service.IsCompleted("CH.B2.1"));
            await _service.CompleteAsync("CH.B2.1");
            _challengeService.Verify(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null), Times.Exactly(2));
        }

        [Test]
        public async Task Reset_WhileRequestInFlight_LateResultIsNotKeptForTheNextUser()
        {
            var pending = new TaskCompletionSource<(ChallengeProgress, ApiErrorResponse)>();
            _challengeService.Setup(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null)).Returns(pending.Task);
            var previousUser = _service.CompleteAsync("CH.B2.1");

            _service.Reset();
            pending.SetResult((new ChallengeProgress { completed = true, progress = 100f }, null));
            await previousUser;

            Assert.IsFalse(_service.IsCompleted("CH.B2.1"));
        }

        [Test]
        public async Task CompleteAsync_CallerJoiningInFlightRequest_GetsNoReward()
        {
            var reward = new ContentReward { xp = 10 };
            var pending = new TaskCompletionSource<(ChallengeProgress, ApiErrorResponse)>();
            _challengeService.Setup(c => c.UpdateChallengeProgressAsync("CH.B2.1", true, 100f, null)).Returns(pending.Task);

            var originator = _service.CompleteAsync("CH.B2.1");
            var joiner = _service.CompleteAsync("CH.B2.1");
            pending.SetResult((new ChallengeProgress { completed = true, progress = 100f, reward = reward }, null));
            await Task.WhenAll(originator, joiner);

            Assert.AreSame(reward, originator.Result.Reward, "Only the caller that started the request celebrates");
            Assert.IsTrue(joiner.Result.Success);
            Assert.IsNull(joiner.Result.Reward);
        }
    }
}
