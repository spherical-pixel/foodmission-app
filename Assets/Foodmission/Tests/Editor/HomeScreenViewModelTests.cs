using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class HomeScreenViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IAudioService> _mockAudioService;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILegalService> _mockLegalService;
        private HomeScreenViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _mockAudioService = new Mock<IAudioService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLegalService = new Mock<ILegalService>();

            _vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public async Task CheckPendingLegalConsentAsync_ReturnsStatusFromService()
        {
            var expectedStatus = new LegalConsentStatus
            {
                mustAccept = true,
                documents = new[]
                {
                    new PendingLegalConsent
                    {
                        docType = LegalDocType.TermsOfService,
                        documentKey = "TERMS_OF_SERVICE:1.0:es",
                        accepted = false
                    }
                }
            };

            _mockLegalService.Setup(s => s.GetConsentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync((expectedStatus, (ApiErrorResponse)null));

            var result = await _vm.CheckPendingLegalConsentAsync();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.mustAccept);
            Assert.AreEqual(1, result.documents.Length);
            Assert.IsFalse(result.documents[0].accepted);
        }

        [Test]
        public async Task AcceptLegalConsentAsync_ReturnsTrueOnSuccess()
        {
            _mockLegalService.Setup(s => s.AcceptConsentAsync("TERMS_OF_SERVICE:1.0:es"))
                .ReturnsAsync((new AcceptLegalConsentResponse { accepted = true }, (ApiErrorResponse)null));

            bool success = await _vm.AcceptLegalConsentAsync("TERMS_OF_SERVICE:1.0:es");

            Assert.IsTrue(success);
        }

        [Test]
        public async Task GetPilotConsentFormAsync_LoadsFromCatalogService()
        {
            var mockCatalog = new Mock<ICatalogService>();
            var mockPilot = new Mock<IPilotSurveyService>();

            mockCatalog.Setup(c => c.GetConsentFormAsync("de", It.IsAny<string>()))
                .ReturnsAsync((new ConsentFormData { countryCode = "de", content = "# Pilot Consent MD" }, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userCountry = "de", lang = "de" });

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object,
                mockPilot.Object,
                mockCatalog.Object
            );

            var (content, error) = await vm.GetPilotConsentFormAsync();
            Assert.IsNull(error);
            Assert.AreEqual("# Pilot Consent MD", content);
        }

        [Test]
        public async Task LoadActiveQuestAsync_WithoutQuestId_SetsHasActiveQuestFalse()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });

            await _vm.LoadActiveQuestAsync();

            Assert.IsFalse(_vm.HasActiveQuest);
            Assert.IsEmpty(_vm.CurrentQuestTitle);
            Assert.IsEmpty(_vm.CurrentQuestActivityStates);
        }

        [Test]
        public async Task LoadActiveQuestAsync_WithValidQuest_LoadsTitleAndActivityStates()
        {
            var mockQuestService = new Mock<IQuestService>();
            var quest = new Quest
            {
                id = "quest-1",
                code = "HEALTHY_BREAKFAST",
                title = "Healthy Breakfast",
                items = new[]
                {
                    new QuestItem { id = "item-1", contentType = QuestContentType.Quiz },
                    new QuestItem { id = "item-2", contentType = QuestContentType.FoodFact },
                    new QuestItem { id = "item-3", contentType = QuestContentType.Mission },
                    new QuestItem { id = "item-4", contentType = QuestContentType.Challenge }
                }
            };
            var progress = new QuestProgress
            {
                questId = "quest-1",
                progress = 50f,
                completed = false
            };

            mockQuestService.Setup(q => q.GetQuestAsync("quest-1", It.IsAny<string>()))
                .ReturnsAsync((quest, (ApiErrorResponse)null));
            mockQuestService.Setup(q => q.GetQuestProgressAsync("quest-1", It.IsAny<string>()))
                .ReturnsAsync((progress, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userCurrentQuestId = "quest-1" });

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object,
                questService: mockQuestService.Object
            );

            await vm.LoadActiveQuestAsync();

            Assert.IsTrue(vm.HasActiveQuest);
            Assert.AreEqual("Healthy Breakfast", vm.CurrentQuestTitle);
            Assert.AreEqual("HEALTHY_BREAKFAST", vm.CurrentQuestCode);
            Assert.AreEqual("quest-1", vm.CurrentQuestId);
            Assert.AreEqual(4, vm.CurrentQuestActivityStates.Length);
            // 50% of 4 items = 2 items completed
            Assert.IsTrue(vm.CurrentQuestActivityStates[0]);
            Assert.IsTrue(vm.CurrentQuestActivityStates[1]);
            Assert.IsFalse(vm.CurrentQuestActivityStates[2]);
            Assert.IsFalse(vm.CurrentQuestActivityStates[3]);
        }

        [Test]
        public void OpenCurrentQuest_RequestsNavigationToQuestDetail()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.SetCurrentQuestForTesting("Healthy Breakfast", "HEALTHY_BREAKFAST", "quest-1", new[] { true, false });
            _vm.OpenCurrentQuest();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.open_quest, requestedAction);
        }

        [Test]
        public void NavigateToQuests_RequestsNavigationToQuests()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.NavigateToQuests();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_quests, requestedAction);
        }
    }
}
