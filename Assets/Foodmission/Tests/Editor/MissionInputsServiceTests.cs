using System;
using System.Globalization;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionInputsServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Local);

        private Mock<IMealLogService> _mealLogs;
        private MissionInputsService _service;

        [SetUp]
        public void SetUp()
        {
            _mealLogs = new Mock<IMealLogService>();
            _service = new MissionInputsService(_mealLogs.Object) { Clock = () => Now };
        }

        private static MealLog Log(string status, params string[] flags) => new MealLog { status = status, flags = flags };

        [Test]
        public void MissionInputs_OnlyTheMeatReductionMissionAsks()
        {
            Assert.IsTrue(MissionInputs.AsksPreviousMeatMeals("M.B1.3"));
            Assert.IsTrue(MissionInputs.AsksPreviousMeatMeals(" m.b1.3 "));
            Assert.IsFalse(MissionInputs.AsksPreviousMeatMeals("M.B1.4"));
            Assert.IsFalse(MissionInputs.AsksPreviousMeatMeals(null));
        }

        [Test]
        public void MissionInputs_For_DropsInputsOfOtherMissions()
        {
            Assert.AreEqual(3, MissionInputs.For("M.B1.3", 3));
            Assert.AreEqual(0, MissionInputs.For("M.B1.3", 0));
            Assert.IsNull(MissionInputs.For("M.B1.4", 3));
            Assert.IsNull(MissionInputs.For("M.B1.3", null));
        }

        [Test]
        public void MissionInputs_MeatFreeGoal_OnlyWithZero()
        {
            Assert.IsTrue(MissionInputs.IsMeatFreeGoal("M.B1.3", 0));
            Assert.IsFalse(MissionInputs.IsMeatFreeGoal("M.B1.3", 2));
            Assert.IsFalse(MissionInputs.IsMeatFreeGoal("M.B1.3", null));
            Assert.IsFalse(MissionInputs.IsMeatFreeGoal("M.B1.4", 0));
        }

        [Test]
        public async Task Suggest_CountsConfirmedMeatMealsOfTheLastSevenDays()
        {
            string from = null;
            string to = null;
            _mealLogs.Setup(m => m.GetLogsAsync(1, MissionInputsService.PageSize, null, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<int, int, string, string, string>((_, _, _, f, t) => { from = f; to = t; })
                .ReturnsAsync((new PaginatedMealLogResponse
                {
                    page = 1, totalPages = 1, data = new[]
                    {
                        Log(MealLogStatus.Confirmed, ClientEventTypes.MealMeatConsumed),
                        Log(null, ClientEventTypes.MealMeatConsumed, ClientEventTypes.MealLegumeConsumed),
                        Log(MealLogStatus.Draft, ClientEventTypes.MealMeatConsumed),
                        Log(MealLogStatus.Confirmed, ClientEventTypes.MealMeatFree),
                        Log(MealLogStatus.Confirmed)
                    }
                }, (ApiErrorResponse)null));

            int count = await _service.SuggestPreviousMeatMealsAsync();

            Assert.AreEqual(2, count, "drafts and meals without the meat flag don't count");
            Assert.AreEqual(Now.AddDays(-7).ToUniversalTime(), DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime());
            Assert.AreEqual(Now.ToUniversalTime(), DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime());
        }

        [Test]
        public async Task Suggest_ReadsEveryPage()
        {
            _mealLogs.Setup(m => m.GetLogsAsync(1, MissionInputsService.PageSize, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new PaginatedMealLogResponse { page = 1, totalPages = 2, data = new[] { Log(MealLogStatus.Confirmed, ClientEventTypes.MealMeatConsumed) } }, (ApiErrorResponse)null));
            _mealLogs.Setup(m => m.GetLogsAsync(2, MissionInputsService.PageSize, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new PaginatedMealLogResponse { page = 2, totalPages = 2, data = new[] { Log(MealLogStatus.Confirmed, ClientEventTypes.MealMeatConsumed) } }, (ApiErrorResponse)null));

            Assert.AreEqual(2, await _service.SuggestPreviousMeatMealsAsync());
        }

        [Test]
        public async Task Suggest_OnError_IsZero()
        {
            _mealLogs.Setup(m => m.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(((PaginatedMealLogResponse)null, new ApiErrorResponse { message = "x" }));

            Assert.AreEqual(0, await _service.SuggestPreviousMeatMealsAsync());
        }

        [Test]
        public void MissionInputs_MeatFreeGoal_FromTheStoredInputs()
        {
            // The backend ignores inputs on an already started mission: trust what it stored, not what was asked
            var meatFree = new MissionProgress { missionCode = "M.B1.3", inputs = new System.Collections.Generic.Dictionary<string, int> { { "previousMeatMeals", 0 } } };
            var three = new MissionProgress { missionCode = "M.B1.3", inputs = new System.Collections.Generic.Dictionary<string, int> { { "previousMeatMeals", 3 } } };
            var none = new MissionProgress { missionCode = "M.B1.3" };

            Assert.IsTrue(MissionInputs.SetsMeatFreeGoal(meatFree));
            Assert.IsFalse(MissionInputs.SetsMeatFreeGoal(three));
            Assert.IsFalse(MissionInputs.SetsMeatFreeGoal(none), "old backends store no inputs");
            Assert.IsFalse(MissionInputs.SetsMeatFreeGoal(null));
        }
    }
}
