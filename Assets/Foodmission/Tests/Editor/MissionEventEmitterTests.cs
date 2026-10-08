using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionEventEmitterTests
    {
        private Mock<IEventService> _events;
        private Mock<IMealLogService> _mealLogs;
        private MissionEventEmitter _emitter;
        private List<int> _delays;

        [SetUp]
        public void SetUp()
        {
            _events = new Mock<IEventService>();
            _mealLogs = new Mock<IMealLogService>();
            _events.Setup(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync((new UserEvent(), (ApiErrorResponse)null));
            _mealLogs.Setup(m => m.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog(), (ApiErrorResponse)null));
            _delays = new List<int>();
            _emitter = new MissionEventEmitter(_events.Object, _mealLogs.Object);
            _emitter.Delay = ms =>
            {
                _delays.Add(ms);
                return Task.CompletedTask;
            };
        }

        private static List<PendingReportItem> Items(int events, int meals)
        {
            var list = new List<PendingReportItem>();
            for (int i = 0; i < events; i++)
            {
                list.Add(new PendingReportItem(new CreateClientEventRequest { eventType = ClientEventTypes.ShoppingOriginChecked, idempotencyKey = $"k{i}" }));
            }
            for (int i = 0; i < meals; i++)
            {
                list.Add(new PendingReportItem(new CreateMealLogRequest { typeOfMeal = "LUNCH", flags = new[] { ClientEventTypes.MealLegumeConsumed } }));
            }
            return list;
        }

        [Test]
        public async Task SendAsync_SendsEventsAndMealLogs_AndMarksThemSent()
        {
            var items = Items(2, 1);

            var result = await _emitter.SendAsync(items);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(items.TrueForAll(i => i.Sent));
            _events.Verify(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()), Times.Exactly(2));
            _mealLogs.Verify(m => m.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Once);
        }

        [Test]
        public async Task SendAsync_AfterPartialFailure_RetrySendsOnlyPending()
        {
            var items = Items(3, 0);
            var error = new ApiErrorResponse { message = "offline" };
            _events.SetupSequence(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync((new UserEvent(), (ApiErrorResponse)null))
                .ReturnsAsync(((UserEvent)null, error))
                .ReturnsAsync((new UserEvent(), (ApiErrorResponse)null))
                .ReturnsAsync((new UserEvent(), (ApiErrorResponse)null));

            var first = await _emitter.SendAsync(items);

            Assert.IsFalse(first.Success);
            Assert.AreSame(error, first.Error);
            Assert.IsTrue(items[0].Sent);
            Assert.IsFalse(items[1].Sent);
            Assert.IsFalse(items[2].Sent);

            var retry = await _emitter.SendAsync(items);

            Assert.IsTrue(retry.Success);
            _events.Verify(e => e.RecordClientEventAsync(It.Is<CreateClientEventRequest>(r => r.idempotencyKey == "k0")), Times.Once);
            _events.Verify(e => e.RecordClientEventAsync(It.Is<CreateClientEventRequest>(r => r.idempotencyKey == "k1")), Times.Exactly(2));
        }

        [Test]
        public async Task SendAsync_WhenServiceThrows_ReturnsFailure()
        {
            _mealLogs.Setup(m => m.CreateAsync(It.IsAny<CreateMealLogRequest>())).ThrowsAsync(new System.Exception("boom"));
            var items = Items(0, 1);

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("MissionEventEmitter"));
            var result = await _emitter.SendAsync(items);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("boom", result.Error.message);
            Assert.IsFalse(items[0].Sent);
        }

        [Test]
        public async Task SendAsync_SpacesRequestsAfterTheFirst()
        {
            var items = Items(3, 1);

            await _emitter.SendAsync(items);

            CollectionAssert.AreEqual(new[] { 250, 250, 250 }, _delays);
        }

        [Test]
        public async Task SendAsync_WhenThrottled_RetriesWithBackoff()
        {
            var items = Items(1, 0);
            var throttled = new ApiErrorResponse { statusCode = 429, error = "ThrottlerException" };
            _events.SetupSequence(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync(((UserEvent)null, throttled))
                .ReturnsAsync(((UserEvent)null, throttled))
                .ReturnsAsync((new UserEvent(), (ApiErrorResponse)null));

            var result = await _emitter.SendAsync(items);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(items[0].Sent);
            CollectionAssert.AreEqual(new[] { 1000, 2000 }, _delays);
        }

        [Test]
        public async Task SendAsync_WhenStillThrottledAfterRetries_FailsAndKeepsItemPending()
        {
            var items = Items(2, 0);
            var throttled = new ApiErrorResponse { statusCode = 429, error = "ThrottlerException" };
            _events.Setup(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync(((UserEvent)null, throttled));

            var result = await _emitter.SendAsync(items);

            Assert.IsFalse(result.Success);
            Assert.AreSame(throttled, result.Error);
            Assert.IsFalse(items[0].Sent);
            _events.Verify(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()), Times.Exactly(3));
        }

        [Test]
        public async Task SendAsync_NonThrottleError_DoesNotRetry()
        {
            var items = Items(1, 0);
            _events.Setup(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync(((UserEvent)null, new ApiErrorResponse { statusCode = 400 }));

            var result = await _emitter.SendAsync(items);

            Assert.IsFalse(result.Success);
            _events.Verify(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()), Times.Once);
        }

        [Test]
        public async Task SendAsync_EmptyOrNull_Succeeds()
        {
            Assert.IsTrue((await _emitter.SendAsync(new List<PendingReportItem>())).Success);
            Assert.IsTrue((await _emitter.SendAsync(null)).Success);
        }
    }
}
