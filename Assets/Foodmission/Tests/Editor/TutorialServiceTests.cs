using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class TutorialServiceTests
    {
        private TestStoreService _store;
        private Mock<IAuthService> _auth;
        private Mock<ITutorialPresenter> _presenter;
        private TutorialService _service;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { accessToken = "tok" });
            _auth = new Mock<IAuthService>();
            _auth.Setup(a => a.SyncSettingsAsync()).Returns(Task.CompletedTask);
            _presenter = new Mock<ITutorialPresenter>();
            _presenter.Setup(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>())).ReturnsAsync(true);
            _service = new TutorialService(_store, _auth.Object, _presenter.Object, TimeSpan.Zero);
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        private void MarkSeen(string id)
        {
            _store.store.Dispatch(AppActions.markTutorialSeen.Invoke(id));
        }

        [Test]
        public async Task ShowAsync_Shown_MarksSeenAndSyncs()
        {
            await _service.ShowAsync(SectionTutorials.Pantry);

            Assert.IsTrue(_service.HasSeen(SectionTutorials.Pantry));
            _auth.Verify(a => a.SyncSettingsAsync(), Times.Once);
        }

        [Test]
        public async Task ShowAsync_NotShown_DoesNotMark()
        {
            _presenter.Setup(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>())).ReturnsAsync(false);

            await _service.ShowAsync(SectionTutorials.Pantry);

            Assert.IsFalse(_service.HasSeen(SectionTutorials.Pantry));
            _auth.Verify(a => a.SyncSettingsAsync(), Times.Never);
        }

        [Test]
        public async Task ShowAsync_AlreadySeen_StillShows()
        {
            MarkSeen(SectionTutorials.Pantry);

            await _service.ShowAsync(SectionTutorials.Pantry);

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Once);
        }

        [Test]
        public async Task ShowAsync_Home_PassesFiveKeys()
        {
            IReadOnlyList<string> shown = null;
            _presenter.Setup(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()))
                .Callback<IReadOnlyList<string>>(keys => shown = keys)
                .ReturnsAsync(true);

            await _service.ShowAsync(SectionTutorials.Home);

            CollectionAssert.AreEqual(new[] { "TUTORIAL_HOME_1", "TUTORIAL_HOME_2", "TUTORIAL_HOME_3", "TUTORIAL_HOME_4", "TUTORIAL_HOME_5" }, shown);
        }

        [Test]
        public async Task ShowAsync_UnknownId_DoesNothing()
        {
            await _service.ShowAsync("unknown");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [Test]
        public async Task ShowAsync_WhileAnotherIsShowing_IsIgnored()
        {
            var first = new TaskCompletionSource<bool>();
            _presenter.Setup(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>())).Returns(first.Task);

            Task showing = _service.ShowAsync(SectionTutorials.Pantry);
            await _service.ShowAsync(SectionTutorials.Quests);
            first.SetResult(true);
            await showing;

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Once);
            Assert.IsFalse(_service.HasSeen(SectionTutorials.Quests));
        }

        [Test]
        public async Task ShowIfFirstTime_NotSeen_Shows()
        {
            await _service.ShowIfFirstTimeAsync("Pantry", () => "Pantry");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Once);
            Assert.IsTrue(_service.HasSeen(SectionTutorials.Pantry));
        }

        [Test]
        public async Task ShowIfFirstTime_Seen_DoesNotShow()
        {
            MarkSeen(SectionTutorials.Pantry);

            await _service.ShowIfFirstTimeAsync("Pantry", () => "Pantry");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [Test]
        public async Task ShowIfFirstTime_Home_DoesNotShow()
        {
            await _service.ShowIfFirstTimeAsync("Home", () => "Home");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [Test]
        public async Task ShowIfFirstTime_UncoveredDestination_DoesNotShow()
        {
            await _service.ShowIfFirstTimeAsync("Badges", () => "Badges");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [Test]
        public async Task ShowIfFirstTime_DialogOpen_SkipsWithoutMarking()
        {
            _presenter.SetupGet(p => p.IsAnyDialogOpen).Returns(true);

            await _service.ShowIfFirstTimeAsync("Pantry", () => "Pantry");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
            Assert.IsFalse(_service.HasSeen(SectionTutorials.Pantry));
        }

        [Test]
        public async Task ShowIfFirstTime_DestinationChangedBeforeDelay_DoesNothing()
        {
            await _service.ShowIfFirstTimeAsync("Pantry", () => "MealLog");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
            Assert.IsFalse(_service.HasSeen(SectionTutorials.Pantry));
        }

        [Test]
        public async Task ShowIfFirstTime_LoggedOut_DoesNothing()
        {
            _store.SetAppState(new AppState());

            await _service.ShowIfFirstTimeAsync("Pantry", () => "Pantry");

            _presenter.Verify(p => p.ShowAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }
    }
}
