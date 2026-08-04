using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class EventServiceTests
    {
        private TestStoreService _storeService;
        private EventService _eventService;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _eventService = new EventService(_storeService);
        }

        [TearDown]
        public void TearDown()
        {
            _storeService?.Dispose();
        }

        [Test]
        public void CurrentSessionId_ReturnsGuid_And_IsStable()
        {
            string sessionId1 = _eventService.CurrentSessionId;
            Assert.IsFalse(string.IsNullOrEmpty(sessionId1));

            string sessionId2 = _eventService.CurrentSessionId;
            Assert.AreEqual(sessionId1, sessionId2);
        }

        [Test]
        public async Task RecordClientEventAsync_WhenUnauthenticated_ReturnsNull()
        {
            // AppState has no accessToken
            var req = new CreateClientEventRequest
            {
                eventType = ClientEventTypes.AppSessionOpened,
                metadata = new ClientEventMetadata { sessionId = _eventService.CurrentSessionId }
            };

            var (result, error) = await _eventService.RecordClientEventAsync(req);
            Assert.IsNull(result);
            Assert.IsNull(error);
        }

        [Test]
        public async Task TrackSessionEndAsync_WithoutSessionStart_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await _eventService.TrackSessionEndAsync();
            });
        }
    }
}
