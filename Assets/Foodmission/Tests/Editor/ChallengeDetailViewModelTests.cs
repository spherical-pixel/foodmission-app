using System.Linq;
using System.Threading.Tasks;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeDetailViewModelTests
    {
        private Mock<IStoreService> _store;
        private Mock<IChallengeService> _challengeService;
        private Mock<IChallengeCompletionService> _completion;
        private Mock<IChallengeSessionService> _session;
        private ChallengeDetailViewModel _vm;
        private string _navAction;
        private Argument[] _navArgs;

        [SetUp]
        public void SetUp()
        {
            _store = new Mock<IStoreService>();
            _challengeService = new Mock<IChallengeService>();
            _completion = new Mock<IChallengeCompletionService>();
            _session = new Mock<IChallengeSessionService>();
            _vm = new ChallengeDetailViewModel(_store.Object, _challengeService.Object, null, _completion.Object, _session.Object);
            _vm.NavigationRequested += (action, args) => { _navAction = action; _navArgs = args; };
        }

        private void SetupChallenge(string code, bool completed)
        {
            _challengeService.Setup(c => c.GetChallengeAsync(code, It.IsAny<string>()))
                .ReturnsAsync((new Challenge { id = "id-" + code, code = code, title = "T", task = "Do it", whyItMatters = "Because", level = "BEGINNER" }, (ApiErrorResponse)null));
            _challengeService.Setup(c => c.GetChallengeProgressAsync(code, It.IsAny<string>()))
                .ReturnsAsync((new ChallengeProgress { challengeCode = code, completed = completed, progress = completed ? 100f : 0f }, (ApiErrorResponse)null));
        }

        [Test]
        public async Task LoadChallengeAsync_SetsChallengeAndInteractionFromCatalog()
        {
            SetupChallenge("CH.B2.2", completed: false);

            await _vm.LoadChallengeAsync("CH.B2.2");

            Assert.AreEqual("CH.B2.2", _vm.Challenge.code);
            Assert.AreSame(ChallengeInteractionCatalog.Get("CH.B2.2"), _vm.Interaction);
            Assert.IsTrue(_vm.HasHelperModule);
            Assert.IsTrue(_vm.ShowsAutoCompleteHint);
            Assert.AreEqual("CHALLENGE_LEVEL_BEGINNER", _vm.LevelKey);
        }

        [Test]
        public async Task LoadChallengeAsync_WhenAlreadyCompleted_HidesActionsAndNeverPatches()
        {
            SetupChallenge("CH.B2.2", completed: true);

            await _vm.LoadChallengeAsync("CH.B2.2");
            bool result = await _vm.MarkCompletedAsync();

            Assert.IsTrue(_vm.IsCompleted);
            Assert.IsFalse(_vm.HasHelperModule);
            Assert.IsTrue(result);
            _completion.Verify(c => c.CompleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task LoadChallengeAsync_WhenCompletedEarlierInSession_IsCompleted()
        {
            SetupChallenge("CH.A4.1", completed: false);
            _completion.Setup(c => c.IsCompleted("CH.A4.1")).Returns(true);

            await _vm.LoadChallengeAsync("CH.A4.1");

            Assert.IsTrue(_vm.IsCompleted);
        }

        [Test]
        public async Task ConfirmChallenge_HasNoHelperModule()
        {
            SetupChallenge("CH.A4.1", completed: false);

            await _vm.LoadChallengeAsync("CH.A4.1");

            Assert.IsFalse(_vm.HasHelperModule);
            Assert.IsFalse(_vm.ShowsAutoCompleteHint);
        }

        [Test]
        public async Task MarkCompletedAsync_OnSuccess_SetsCompletedRewardAndCancelsSession()
        {
            SetupChallenge("CH.A4.1", completed: false);
            var reward = new ContentReward { xp = 20 };
            _completion.Setup(c => c.CompleteAsync("CH.A4.1")).ReturnsAsync(new ChallengeCompletionResult { Success = true, Reward = reward });
            _session.SetupGet(s => s.ActiveChallengeCode).Returns("CH.A4.1");
            await _vm.LoadChallengeAsync("CH.A4.1");

            bool ok = await _vm.MarkCompletedAsync();

            Assert.IsTrue(ok);
            Assert.IsTrue(_vm.IsCompleted);
            Assert.AreSame(reward, _vm.EarnedReward);
            _session.Verify(s => s.Cancel(), Times.Once);
        }

        [Test]
        public async Task MarkCompletedAsync_WhenApiFails_ExposesErrorAndStaysPending()
        {
            SetupChallenge("CH.A4.1", completed: false);
            var error = new ApiErrorResponse { statusCode = 500, message = "boom" };
            _completion.Setup(c => c.CompleteAsync("CH.A4.1")).ReturnsAsync(new ChallengeCompletionResult { Success = false, Error = error });
            await _vm.LoadChallengeAsync("CH.A4.1");

            bool ok = await _vm.MarkCompletedAsync();

            Assert.IsFalse(ok);
            Assert.IsFalse(_vm.IsCompleted);
            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.IsFalse(_vm.IsCompleting);
        }

        [Test]
        public async Task OpenHelperModule_AutoCompleteChallenge_BeginsSessionAndNavigates()
        {
            SetupChallenge("CH.B2.2", completed: false);
            await _vm.LoadChallengeAsync("CH.B2.2");

            _vm.OpenHelperModule();

            _session.Verify(s => s.Begin("CH.B2.2", ChallengeInteractionCatalog.Get("CH.B2.2")), Times.Once);
            Assert.AreEqual(Actions.go_to_quicksearch, _navAction);
        }

        [Test]
        public async Task OpenHelperModule_FoodFact_PassesFactCode()
        {
            SetupChallenge("CH.B1.3", completed: false);
            await _vm.LoadChallengeAsync("CH.B1.3");

            _vm.OpenHelperModule();

            Assert.AreEqual(Actions.open_food_fact, _navAction);
            Assert.AreEqual("FF1.2.2", _navArgs.Single(a => a.name == "code").value);
        }

        [Test]
        public async Task OpenHelperModule_Comparator_PassesChallengeModeAndSource()
        {
            SetupChallenge("CH.A1.5", completed: false);
            await _vm.LoadChallengeAsync("CH.A1.5");

            _vm.OpenHelperModule();

            Assert.AreEqual(Actions.go_to_food_comparison, _navAction);
            Assert.AreEqual("CH.A1.5", _navArgs.Single(a => a.name == "challengeCode").value);
            Assert.AreEqual("proteins", _navArgs.Single(a => a.name == "mode").value);
            Assert.AreEqual("shopping_list", _navArgs.Single(a => a.name == "source").value);
        }
            [Test]
        public async Task MarkCompletedAsync_WhileAlreadyCompleting_SendsOneRequestAndDisablesActions()
        {
            SetupChallenge("CH.A4.1", completed: false);
            var pending = new TaskCompletionSource<ChallengeCompletionResult>();
            _completion.Setup(c => c.CompleteAsync("CH.A4.1")).Returns(pending.Task);
            await _vm.LoadChallengeAsync("CH.A4.1");
            Assert.IsTrue(_vm.AreActionsEnabled);

            Task<bool> first = _vm.MarkCompletedAsync();
            Assert.IsFalse(_vm.AreActionsEnabled, "Actions stay disabled while the PATCH runs");
            bool second = await _vm.MarkCompletedAsync();

            pending.SetResult(new ChallengeCompletionResult { Success = true });
            Assert.IsTrue(await first);
            Assert.IsFalse(second);
            Assert.IsTrue(_vm.AreActionsEnabled);
            _completion.Verify(c => c.CompleteAsync("CH.A4.1"), Times.Once);
        }

        [Test]
        public async Task LoadChallengeAsync_WhenLoadThrows_ExposesErrorAndKeepsOnlyLaterButton()
        {
            _challengeService.Setup(c => c.GetChallengeAsync("CH.A4.1", It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("socket closed"));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex("LoadChallengeAsync error: socket closed"));

            await _vm.LoadChallengeAsync("CH.A4.1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.IsTrue(_vm.ShowsLaterButton, "The user must always have a way out");
            Assert.IsFalse(_vm.ShowsDoneButton);
            Assert.IsFalse(_vm.HasHelperModule);
        }

        [Test]
        public async Task LoadChallengeAsync_WhenApiError_KeepsOnlyLaterButton()
        {
            _challengeService.Setup(c => c.GetChallengeAsync("CH.A4.1", It.IsAny<string>()))
                .ReturnsAsync(((Challenge)null, new ApiErrorResponse { statusCode = 500, message = "boom" }));

            await _vm.LoadChallengeAsync("CH.A4.1");

            Assert.IsTrue(_vm.ShowsLaterButton);
            Assert.IsFalse(_vm.ShowsDoneButton);
        }

        [Test]
        public async Task LoadChallengeAsync_PendingChallenge_ShowsDoneAndLater_CompletedShowsNeither()
        {
            SetupChallenge("CH.A4.1", completed: false);
            await _vm.LoadChallengeAsync("CH.A4.1");
            Assert.IsTrue(_vm.ShowsDoneButton);
            Assert.IsTrue(_vm.ShowsLaterButton);

            SetupChallenge("CH.A4.2", completed: true);
            await _vm.LoadChallengeAsync("CH.A4.2");
            Assert.IsFalse(_vm.ShowsDoneButton);
            Assert.IsFalse(_vm.ShowsLaterButton);
        }
            [Test]
        public async Task ChallengeCompletedElsewhere_WhileShown_RefreshesToCompleted()
        {
            SetupChallenge("CH.B2.2", completed: false);
            await _vm.LoadChallengeAsync("CH.B2.2");
            bool notified = false;
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChallengeDetailViewModel.IsCompleted))
                {
                    notified = true;
                }
            };

            _completion.Raise(c => c.ChallengeCompleted += null, "CH.B2.2", (ContentReward)null);

            Assert.IsTrue(_vm.IsCompleted);
            Assert.IsTrue(notified, "The screen must be told to refresh");
            Assert.IsFalse(_vm.HasHelperModule);
            Assert.IsFalse(_vm.ShowsDoneButton);
        }

        [Test]
        public async Task OtherChallengeCompleted_WhileShown_DoesNotChangeState()
        {
            SetupChallenge("CH.B2.2", completed: false);
            await _vm.LoadChallengeAsync("CH.B2.2");

            _completion.Raise(c => c.ChallengeCompleted += null, "CH.B3.1", (ContentReward)null);

            Assert.IsFalse(_vm.IsCompleted);
            Assert.IsTrue(_vm.ShowsDoneButton);
        }

        [Test]
        public async Task Dispose_StopsListeningToCompletions()
        {
            SetupChallenge("CH.B2.2", completed: false);
            await _vm.LoadChallengeAsync("CH.B2.2");

            _vm.Dispose();
            _completion.Raise(c => c.ChallengeCompleted += null, "CH.B2.2", (ContentReward)null);

            Assert.IsFalse(_vm.ChallengeProgress != null && _vm.ChallengeProgress.completed);
        }
    }
}
