using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GamificationServiceTests
    {
        private TestStoreService _storeService;
        private GamificationService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                lang = "es"
            });
            _service = new GamificationService(_storeService);
        }

        [Test]
        public async Task GetWalletBalanceAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetWalletBalanceAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetEarnedRewardsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetEarnedRewardsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetGamificationProfileAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetGamificationProfileAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task SubmitOnboardingSurveyAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.SubmitOnboardingSurveyAsync(new OnboardingSurveyData());

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void BuildOnboardingSurveyUrl_TargetsGamificationRoute()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/onboarding-survey",
                GamificationService.BuildOnboardingSurveyUrl("https://api.test"));
        }

        [Test]
        public void BuildProgressWheelsUrl_TargetsGamificationRoute()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/progress-wheels",
                GamificationService.BuildProgressWheelsUrl("https://api.test"));
        }

        [Test]
        public async Task GetProgressWheelsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetProgressWheelsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void ParseDimensionLevels_ReadsCodeAndLevel_IgnoringQuests()
        {
            const string json = "[{\"dimensionCode\":\"DIET_CHANGES\",\"dimensionName\":\"Diet\",\"level\":\"INTERMEDIATE\",\"quests\":[{\"id\":\"q\"}]},{\"dimensionCode\":\"PACKAGING\",\"level\":\"BEGINNER\",\"quests\":[]}]";

            DimensionLevelEntry[] levels = GamificationService.ParseDimensionLevels(json);

            Assert.AreEqual(2, levels.Length);
            Assert.AreEqual(DimensionCode.DietChanges, levels[0].dimensionCode);
            Assert.AreEqual("INTERMEDIATE", levels[0].level);
        }

        [Test]
        public void BuildDimensionsUrl_TargetsGamificationRoute()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/dimensions", GamificationService.BuildDimensionsUrl("https://api.test"));
        }

        [Test]
        public async Task GetDimensionLevelsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetDimensionLevelsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void ParseOnboardingSurvey_ReadsQuestionsInOrder_AndDropsInvalidOnes()
        {
            const string json = "{\"questions\":[" +
                "{\"field\":\"weeklyMeatConsumption\",\"text\":\"How many meals…?\",\"options\":[{\"value\":\"ZERO_TO_FOUR\",\"label\":\"0-4\"},{\"value\":\"FIVE_TO_NINE\",\"label\":\"5-9\"}]}," +
                "{\"field\":\"\",\"text\":\"no field\",\"options\":[{\"value\":\"A\",\"label\":\"A\"}]}," +
                "{\"field\":\"usesLeftovers\",\"text\":\"no options\",\"options\":[]}," +
                "{\"field\":\"wholeGrainFrequency\",\"text\":\"Whole grain?\",\"options\":[{\"value\":\"NEVER\",\"label\":\"Never\"},{\"value\":\"\",\"label\":\"broken\"}]}" +
                "]}";

            OnboardingSurveyQuestion[] questions = GamificationService.ParseOnboardingSurvey(json);

            CollectionAssert.AreEqual(new[] { "weeklyMeatConsumption", "wholeGrainFrequency" }, System.Linq.Enumerable.Select(questions, q => q.field));
            Assert.AreEqual("5-9", questions[0].options[1].label);
            Assert.AreEqual(1, questions[1].options.Length, "options without a value are dropped");
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"questions\":null}")]
        public void ParseOnboardingSurvey_EmptyOrMissing_ReturnsEmpty(string json)
        {
            Assert.AreEqual(0, GamificationService.ParseOnboardingSurvey(json).Length);
        }

        [Test]
        public async Task GetOnboardingSurveyAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetOnboardingSurveyAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }
    
        [Test]
        public void BuildKnowledgeProgressUrls_TargetGamificationRoutes()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/knowledge-progress",
                GamificationService.BuildKnowledgeProgressUrl("https://api.test"));
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/knowledge-progress/FOOD_WASTE",
                GamificationService.BuildKnowledgeBarUrl("https://api.test", KnowledgeKind.FoodWaste, null));
        }

        [Test]
        public void BuildKnowledgeBarUrl_SendsLangAsQuery()
        {
            // The backend translates item titles only from ?lang= (it ignores Accept-Language)
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/knowledge-progress/HEALTH?lang=es",
                GamificationService.BuildKnowledgeBarUrl("https://api.test", KnowledgeKind.Health, "es"));
        }

        [Test]
        public void ParseKnowledgeProgress_ReadsBarsWithoutItems()
        {
            const string json = "[{\"kind\":\"HEALTH\",\"finishedItems\":3,\"totalItems\":40,\"percentComplete\":7.5}," +
                                "{\"kind\":\"FOOD_WASTE\",\"finishedItems\":0,\"totalItems\":12,\"percentComplete\":0}]";

            KnowledgeProgress[] bars = GamificationService.ParseKnowledgeProgress(json);

            Assert.AreEqual(2, bars.Length);
            Assert.AreEqual(KnowledgeKind.Health, bars[0].kind);
            Assert.AreEqual(3, bars[0].finishedItems);
            Assert.AreEqual(40, bars[0].totalItems);
            Assert.AreEqual(7.5f, bars[0].percentComplete);
            Assert.IsNull(bars[0].items);
        }

        [Test]
        public void ParseKnowledgeBar_ReadsItems()
        {
            const string json = "{\"kind\":\"HEALTH\",\"finishedItems\":1,\"totalItems\":2,\"percentComplete\":50," +
                                "\"items\":[{\"contentType\":\"QUIZ\",\"contentCode\":\"Q1\",\"title\":\"Sugar?\",\"finished\":true}," +
                                "{\"contentType\":\"MISSION\",\"contentCode\":\"M.B1.1\",\"title\":\"\",\"finished\":false}]}";

            KnowledgeProgress bar = GamificationService.ParseKnowledgeBar(json);

            Assert.AreEqual(2, bar.items.Length);
            Assert.AreEqual(QuestContentType.Quiz, bar.items[0].contentType);
            Assert.AreEqual("Q1", bar.items[0].contentCode);
            Assert.IsTrue(bar.items[0].finished);
            Assert.IsFalse(bar.items[1].finished);
        }

        [Test]
        public void ParseKnowledge_EmptyJson_ReturnsEmpty()
        {
            Assert.AreEqual(0, GamificationService.ParseKnowledgeProgress("").Length);
            Assert.IsNull(GamificationService.ParseKnowledgeBar(""));
        }

        [Test]
        public async Task GetKnowledgeProgressAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetKnowledgeProgressAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetKnowledgeBarAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetKnowledgeBarAsync(KnowledgeKind.Health);

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }
    
        [Test]
        public void BuildCurrentQuestUrl_SendsLangAsQuery()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/current-quest?lang=es",
                GamificationService.BuildCurrentQuestUrl("https://api.test", "es"));
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/current-quest",
                GamificationService.BuildCurrentQuestUrl("https://api.test", null));
        }

        [Test]
        public void ParseCurrentQuest_ReadsQuestAndItems()
        {
            const string json = "{\"questId\":\"q-1\",\"questCode\":\"QUEST.X\",\"name\":\"N\",\"description\":null,\"progress\":50," +
                                "\"completed\":false,\"finishedItems\":1,\"totalItems\":2,\"dimensionCode\":\"DIET_CHANGES\",\"level\":\"BEGINNER\"," +
                                "\"items\":[{\"contentType\":\"MISSION\",\"contentCode\":\"M1\",\"title\":\"T\",\"finished\":false," +
                                "\"status\":\"IN_PROGRESS\",\"progress\":40,\"sortOrder\":2}]}";

            CurrentQuest quest = GamificationService.ParseCurrentQuest(json);

            Assert.AreEqual("q-1", quest.questId);
            Assert.AreEqual(50f, quest.progress);
            Assert.AreEqual("DIET_CHANGES", quest.dimensionCode);
            Assert.AreEqual(CurrentQuestItemStatus.InProgress, quest.items[0].status);
            Assert.AreEqual(40f, quest.items[0].progress);
            Assert.AreEqual(2, quest.items[0].sortOrder);
        }

        [TestCase("")]
        [TestCase("null")]
        [TestCase("  ")]
        public void ParseCurrentQuest_NoQuest_ReturnsNull(string json)
        {
            Assert.IsNull(GamificationService.ParseCurrentQuest(json));
        }

        [Test]
        public async Task GetCurrentQuestAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetCurrentQuestAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void RecentEventsUrl_FiltersByEventTypes()
        {
            string url = GamificationService.RecentEventsUrl("https://api", new[] { "FOOD_WASTE_FIFO_ORGANIZED", "NUTRITION_RAINBOW_COLOURS_LOGGED" }, 100);

            Assert.AreEqual("https://api/api/v1/users/me/gamification?eventsLimit=100&walletEntriesLimit=1&eventTypes=FOOD_WASTE_FIFO_ORGANIZED%2CNUTRITION_RAINBOW_COLOURS_LOGGED", url);
        }

        [Test]
        public async Task GetRecentEventsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetRecentEventsAsync(new[] { "FOOD_WASTE_FIFO_ORGANIZED" }, 10);

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void ProfileUrl_WithEventTypes_AddsTheFilter()
        {
            string url = GamificationService.ProfileUrl("http://api", 30, 30, new[] { "MISSION_COMPLETED", "QUEST_COMPLETED" });

            StringAssert.Contains("eventsLimit=30", url);
            StringAssert.Contains("walletEntriesLimit=30", url);
            StringAssert.Contains("eventTypes=MISSION_COMPLETED%2CQUEST_COMPLETED", url);
            StringAssert.DoesNotContain("eventTypes", GamificationService.ProfileUrl("http://api", 30, 30, null));
        }

        [Test]
        public void KeepEventsOfTypes_FiltersEventsAndKeepsTheRest()
        {
            // Fallback for backends without #419: the unfiltered profile is filtered here, wallet untouched
            var profile = new GamificationProfileResponse
            {
                userId = "u1",
                wallet = new WalletBalance { xp = 10 },
                recentEvents = new[]
                {
                    new UserEvent { id = "1", eventType = "MEAL_LOGGED" },
                    new UserEvent { id = "2", eventType = "MISSION_COMPLETED" },
                    null
                }
            };

            GamificationProfileResponse filtered = GamificationService.KeepEventsOfTypes(profile, new[] { "MISSION_COMPLETED" }, 30);

            Assert.AreEqual(new[] { "2" }, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(filtered.recentEvents, e => e.id)));
            Assert.AreEqual(10, filtered.wallet.xp);
        }
    }
}
