using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeBarDetailViewModelTests
    {
        private TestStoreService _store;
        private Mock<IGamificationService> _gamification;
        private KnowledgeBarDetailViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", accessToken = "t" });
            _gamification = new Mock<IGamificationService>();
            _vm = new KnowledgeBarDetailViewModel(_store, _gamification.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _store?.Dispose();
        }

        [Test]
        public async Task Load_FillsHeaderAndSections()
        {
            _gamification.Setup(g => g.GetKnowledgeBarAsync(KnowledgeKind.Health)).ReturnsAsync((new KnowledgeProgress
            {
                kind = KnowledgeKind.Health,
                finishedItems = 1,
                totalItems = 2,
                percentComplete = 50f,
                items = new[]
                {
                    new KnowledgeItem { contentType = QuestContentType.Quiz, contentCode = "Q1", finished = true },
                    new KnowledgeItem { contentType = QuestContentType.Mission, contentCode = "M1", finished = false },
                },
            }, (ApiErrorResponse)null));

            await _vm.LoadAsync(KnowledgeKind.Health);

            Assert.AreEqual(KnowledgeKind.Health, _vm.Kind);
            Assert.AreEqual(1, _vm.Finished);
            Assert.AreEqual(2, _vm.Total);
            Assert.AreEqual(0.5f, _vm.Progress, 0.0001f);
            Assert.AreEqual(2, _vm.Sections.Count);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task Load_Error_SetsErrorDetail_KeepsSectionsEmpty()
        {
            _gamification.Setup(g => g.GetKnowledgeBarAsync(KnowledgeKind.Health))
                .ReturnsAsync(((KnowledgeProgress)null, new ApiErrorResponse { statusCode = 500, message = "x" }));

            await _vm.LoadAsync(KnowledgeKind.Health);

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(0, _vm.Sections.Count);
            Assert.IsFalse(_vm.IsLoading);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("FOOD_AND_WASTE")]
        public async Task Load_UnknownKind_RequestsBack(string kind)
        {
            bool back = false;
            _vm.BackRequested += () => back = true;

            await _vm.LoadAsync(kind);

            Assert.IsTrue(back);
            _gamification.Verify(g => g.GetKnowledgeBarAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Open_QuizAndFact_NavigateByCode_OthersDoNothing()
        {
            string action = null;
            Argument[] args = null;
            _vm.NavigationRequested += (a, ar) => { action = a; args = ar; };

            _vm.Open(new KnowledgeBarRow { ContentType = QuestContentType.Quiz, Code = "Q1", CanOpen = true });
            Assert.AreEqual(Actions.open_quiz, action);
            Assert.AreEqual("code", args[0].name);
            Assert.AreEqual("Q1", args[0].value);

            _vm.Open(new KnowledgeBarRow { ContentType = QuestContentType.FoodFact, Code = "F1", CanOpen = true });
            Assert.AreEqual(Actions.open_food_fact, action);
            Assert.AreEqual("F1", args[0].value);

            action = null;
            _vm.Open(new KnowledgeBarRow { ContentType = QuestContentType.Mission, Code = "M1", CanOpen = false });
            Assert.IsNull(action);
        }
    
        [Test]
        public void Load_PrefillsHeaderFromCachedBar_WhileLoading()
        {
            _store.store.Dispatch(AppActions.setKnowledgeProgress.Invoke(new[]
            {
                new KnowledgeProgress { kind = KnowledgeKind.Health, finishedItems = 3, totalItems = 10, percentComplete = 30f },
            }));
            var pending = new TaskCompletionSource<(KnowledgeProgress, ApiErrorResponse)>();
            _gamification.Setup(g => g.GetKnowledgeBarAsync(KnowledgeKind.Health)).Returns(pending.Task);

            _ = _vm.LoadAsync(KnowledgeKind.Health);

            Assert.AreEqual(3, _vm.Finished);
            Assert.AreEqual(10, _vm.Total);
            Assert.AreEqual(0.3f, _vm.Progress, 0.0001f);
            pending.SetResult((null, new ApiErrorResponse { statusCode = 500 }));
        }
    }
}
