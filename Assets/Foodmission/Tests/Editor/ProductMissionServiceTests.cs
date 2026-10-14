using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProductMissionServiceTests
    {
        private TestStoreService _store;
        private Mock<IQuestService> _quests;
        private Mock<IMissionService> _missions;
        private Mock<IGamificationService> _gamification;
        private Mock<IMissionEventEmitter> _emitter;
        private ProductMissionService _service;
        private UserEvent[] _recentEvents;
        private List<PendingReportItem> _sent;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            _quests = new Mock<IQuestService>();
            _missions = new Mock<IMissionService>();
            _gamification = new Mock<IGamificationService>();
            _emitter = new Mock<IMissionEventEmitter>();

            string[] codes = { "M.A2.1", "M.B2.5", "M.A4.5" };
            var items = codes.Select(c => new QuestItem { contentType = QuestContentType.Mission, contentCode = c }).ToArray();
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ReturnsAsync((new Quest { id = "q1", items = items }, (ApiErrorResponse)null));
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((codes
                .Select(c => new MissionProgress { missionCode = c, missionTitle = c, progress = 10, startedAt = DateTime.UtcNow.AddDays(-1) })
                .ToArray(), (ApiErrorResponse)null));

            _recentEvents = new UserEvent[0];
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), ProductMissionService.EventsLimit))
                .ReturnsAsync(() => (_recentEvents, (ApiErrorResponse)null));

            _sent = new List<PendingReportItem>();
            _emitter.Setup(e => e.SendAsync(It.IsAny<IReadOnlyList<PendingReportItem>>()))
                .Callback<IReadOnlyList<PendingReportItem>>(list => _sent.AddRange(list))
                .ReturnsAsync(MissionReportSendResult.Ok);

            _service = new ProductMissionService(_store, _quests.Object, _missions.Object, _gamification.Object, _emitter.Object);
        }

        private static UserEvent Event(string type, string json) =>
            new UserEvent { eventType = type, metadata = JObject.Parse(json) };

        private static ProductMissionCard PlanFor(string code, string sentEvent, DateTime? start = null)
        {
            var inputs = new ProductMissionInputs { FoodType = FoodInfoType.Product, FoodId = "p-1" };
            inputs.Missions.Add(new CheckInMission(code, code, MissionInteractionCatalog.Get(code), start ?? DateTime.Now.AddDays(-1)));
            if (sentEvent != null)
            {
                inputs.Sent[sentEvent] = new Dictionary<string, DateTime> { { "", DateTime.Now } };
            }
            return ProductMissionPlanner.Plan(inputs).Single();
        }

        private static int Index(ProductMissionCard card, string eventType) =>
            card.Options.ToList().FindIndex(o => o.EventType == eventType);

        private static IDictionary<string, object> Metadata(PendingReportItem item) =>
            (IDictionary<string, object>)item.Event.metadata;

        [Test]
        public async Task Load_KeepsOnlyThisProductsEvents()
        {
            _recentEvents = new[]
            {
                Event(ClientEventTypes.ShoppingOriginChecked, "{\"productId\":\"p-1\"}"),
                Event(ClientEventTypes.ProcessingProductionMethodChecked, "{\"productId\":\"p-2\"}"),
            };

            var (cards, error) = await _service.LoadAsync(FoodInfoType.Product, "p-1", new string[0]);

            Assert.IsNull(error);
            Assert.AreEqual(1, cards.Single(c => c.MissionCode == "M.A2.1").Locked.Count);
        }

        [Test]
        public async Task Load_FallsBackOn400()
        {
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), ProductMissionService.EventsLimit))
                .ReturnsAsync(((UserEvent[])null, new ApiErrorResponse { statusCode = 400 }));
            _gamification.Setup(g => g.GetGamificationProfileAsync(ProductMissionService.EventsLimit, 1))
                .ReturnsAsync((new GamificationProfileResponse { recentEvents = new[] { Event(ClientEventTypes.ShoppingOriginChecked, "{\"productId\":\"p-1\"}") } }, (ApiErrorResponse)null));

            var (cards, error) = await _service.LoadAsync(FoodInfoType.Product, "p-1", new string[0]);

            Assert.IsNull(error);
            Assert.AreEqual(1, cards.Single(c => c.MissionCode == "M.A2.1").Locked.Count);
        }

        [Test]
        public void CanSend_ChecklistNeedsTheRule_OnLockedPlusSelected()
        {
            ProductMissionCard card = PlanFor("M.A2.1", ClientEventTypes.ShoppingOriginChecked);
            int cert = Index(card, ClientEventTypes.ShoppingCertificationChosen);

            Assert.IsFalse(ProductMissionService.CanSend(card, new int[0]), "nothing new");
            Assert.IsTrue(ProductMissionService.CanSend(card, new[] { cert }), "origin (sent) + certification");
        }

        [Test]
        public void CanSend_ChecklistWithoutTheRequiredOption_IsFalse()
        {
            ProductMissionCard card = PlanFor("M.A2.1", null);
            int cert = Index(card, ClientEventTypes.ShoppingCertificationChosen);
            int method = Index(card, ClientEventTypes.ProcessingProductionMethodChecked);

            Assert.IsFalse(ProductMissionService.CanSend(card, new[] { cert, method }), "origin is required");
        }

        [Test]
        public void BuildEvents_OnlyNewOptions_WithRealIdAndMetadata()
        {
            ProductMissionCard card = PlanFor("M.A2.1", ClientEventTypes.ShoppingOriginChecked);
            int origin = Index(card, ClientEventTypes.ShoppingOriginChecked);
            int cert = Index(card, ClientEventTypes.ShoppingCertificationChosen);

            List<PendingReportItem> items = ProductMissionService.BuildEvents(card, new[] { origin, cert });

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(ClientEventTypes.ShoppingCertificationChosen, items[0].Event.eventType);
            Assert.AreEqual("p-1", Metadata(items[0])["productId"]);
            Assert.AreEqual("M.A2.1", Metadata(items[0])["missionCode"]);
            Assert.AreEqual(ProductMissionService.ReportedVia, Metadata(items[0])["reportedVia"]);
        }

        [Test]
        public void BuildEvents_ArrayChecklist_SendsLockedPlusSelectedValues()
        {
            ProductMissionCard card = PlanFor("M.A2.2", null);

            List<PendingReportItem> items = ProductMissionService.BuildEvents(card, new[] { 0, 1 });

            Assert.AreEqual(1, items.Count);
            CollectionAssert.AreEquivalent(new[] { card.Options[0].Value, card.Options[1].Value }, (IEnumerable<string>)Metadata(items[0])["labels"]);
        }

        [Test]
        public async Task Send_UpdatesSent_SoReplanLocksOtherCards()
        {
            var (cards, _) = await _service.LoadAsync(FoodInfoType.Product, "p-1", new string[0]);

            MissionReportSendResult result = await _service.SendAsync(cards.Single(c => c.MissionCode == "M.B2.5"), new[] { 0 });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, _sent.Count);
            Assert.IsTrue(_service.Replan().Where(c => c.MissionCode == "M.B2.5" || c.MissionCode == "M.A4.5").All(c => c.AllSent));
        }

        [Test]
        public async Task Send_Failure_KeepsCardUnlocked()
        {
            _emitter.Setup(e => e.SendAsync(It.IsAny<IReadOnlyList<PendingReportItem>>()))
                .ReturnsAsync(MissionReportSendResult.Fail(new ApiErrorResponse { message = "down" }));
            var (cards, _) = await _service.LoadAsync(FoodInfoType.Product, "p-1", new string[0]);

            MissionReportSendResult result = await _service.SendAsync(cards.Single(c => c.MissionCode == "M.B2.5"), new[] { 0 });

            Assert.IsFalse(result.Success);
            Assert.IsFalse(_service.Replan().Single(c => c.MissionCode == "M.B2.5").AllSent);
        }

        [Test]
        public async Task Load_RequestsAtMostTheBackendEventsLimit()
        {
            // The backend rejects eventsLimit > 100 (GamificationProfileQueryDto @Max(100))
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<int>()))
                .ReturnsAsync(() => (_recentEvents, (ApiErrorResponse)null));

            await _service.LoadAsync(FoodInfoType.Product, "p-1", new string[0]);

            _gamification.Verify(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.Is<int>(l => l <= 100)), Times.Once);
        }

        [Test]
        public async Task Load_ReportsBeforeTheMissionStart_DoNotLock()
        {
            _recentEvents = new[]
            {
                new UserEvent { eventType = ClientEventTypes.ShoppingPackagingInfoChecked, timestamp = DateTime.UtcNow.AddDays(-10).ToString("o"),
                    metadata = JObject.Parse("{\"productId\":\"p-1\"}") }
            };

            var (cards, _) = await _service.LoadAsync(FoodInfoType.Product, "p-1", new string[0]);

            Assert.IsFalse(cards.Single(c => c.MissionCode == "M.B2.5").AllSent);
        }

        [Test]
        public void BuildEvents_KeyDependsOnTheMissionStart()
        {
            // A restarted or later mission must not replay (dedupe into) an event of an earlier window
            ProductMissionCard first = PlanFor("M.B2.1", null, DateTime.Now.AddDays(-3));
            ProductMissionCard later = PlanFor("M.B2.1", null, DateTime.Now.AddDays(-1));

            string a = ProductMissionService.BuildEvents(first, new[] { 0 }).Single().Event.idempotencyKey;
            string b = ProductMissionService.BuildEvents(later, new[] { 0 }).Single().Event.idempotencyKey;

            Assert.AreNotEqual(a, b);
        }

        [Test]
        public async Task Load_AnOlderLoadFinishingLast_DoesNotReplaceTheNewerFood()
        {
            var slow = new TaskCompletionSource<(UserEvent[], ApiErrorResponse)>();
            _gamification.SetupSequence(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), ProductMissionService.EventsLimit))
                .Returns(slow.Task)
                .ReturnsAsync((new UserEvent[0], (ApiErrorResponse)null));

            Task<(IReadOnlyList<ProductMissionCard>, ApiErrorResponse)> older = _service.LoadAsync(FoodInfoType.Product, "p-old", new string[0]);
            await _service.LoadAsync(FoodInfoType.Product, "p-new", new string[0]);
            slow.SetResult((new UserEvent[0], null));
            await older;

            Assert.IsTrue(_service.Replan().All(c => c.FoodId == "p-new"));
        }
    }
}
