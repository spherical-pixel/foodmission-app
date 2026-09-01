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
    }
}
