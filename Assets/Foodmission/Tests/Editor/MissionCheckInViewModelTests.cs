using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionCheckInViewModelTests
    {
        private static readonly DateTime Now = CheckInPlannerTests.Now;

        private Mock<ICheckInService> _checkIn;
        private Mock<IMissionEventEmitter> _emitter;
        private Mock<IMissionService> _missions;
        private MissionCheckInViewModel _vm;
        private readonly List<IReadOnlyList<PendingReportItem>> _sent = new();

        [SetUp]
        public void SetUp()
        {
            _sent.Clear();
            _checkIn = new Mock<ICheckInService>();
            _emitter = new Mock<IMissionEventEmitter>();
            _missions = new Mock<IMissionService>();
            _emitter.Setup(e => e.SendAsync(It.IsAny<IReadOnlyList<PendingReportItem>>()))
                .Callback<IReadOnlyList<PendingReportItem>>(items =>
                {
                    _sent.Add(items);
                    foreach (var i in items)
                    {
                        i.Sent = true;
                    }
                })
                .ReturnsAsync(MissionReportSendResult.Ok);
            var store = new TestStoreService();
            store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            _vm = new MissionCheckInViewModel(store, _checkIn.Object, _emitter.Object, _missions.Object) { Clock = () => Now };
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
        }

        private async Task LoadAsync(string onlyCode, params CheckInMission[] missions)
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(missions));
            _checkIn.Setup(c => c.LoadPlanAsync(onlyCode)).ReturnsAsync((plan, (ApiErrorResponse)null));
            await _vm.LoadAsync(onlyCode);
        }

        private async Task CompleteAsync()
        {
            await _vm.GoToStepAsync(_vm.SummaryStepIndex);
            await _vm.GoNextAsync();
        }

        [Test]
        public async Task LoadAsync_EmptyPlan_IsUpToDate()
        {
            await LoadAsync(null);

            Assert.IsTrue(_vm.IsUpToDate);
        }

        [Test]
        public async Task LoadAsync_StepsAreMealDaysThenDayEventsThenMissionStepsThenSummary()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.A1.3", Now.AddDays(-1)), CheckInPlannerTests.M("M.A5.4", Now), CheckInPlannerTests.M("M.B2.1"));

            Assert.AreEqual(2 + 1 + 1 + 1, _vm.StepCount);
            CollectionAssert.AreEqual(
                new[] { CheckInStepKind.MealDay, CheckInStepKind.MealDay, CheckInStepKind.DayEvent, CheckInStepKind.MissionStep, CheckInStepKind.Summary },
                Enumerable.Range(0, _vm.StepCount).Select(_vm.KindOf).ToArray());
            Assert.AreEqual(0, _vm.ItemIndexOf(3));
            Assert.AreEqual("MISSION_CHECKIN_Q_MEALS", _vm.NutriPromptKey);
        }

        [Test]
        public async Task ToggleMealEvent_RequiresChosenMeal_AndMeatFlagsAreExclusive()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.B1.1", Now));

            _vm.ToggleMealEvent(0, "LUNCH", ClientEventTypes.MealMeatFree);
            Assert.IsFalse(_vm.Answers.MealDays[0].Meals.ContainsKey("LUNCH"), "meal not chosen yet");

            _vm.ToggleMeal(0, "LUNCH");
            _vm.ToggleMealEvent(0, "LUNCH", ClientEventTypes.MealMeatFree);
            _vm.ToggleMealEvent(0, "LUNCH", ClientEventTypes.MealMeatConsumed);

            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.MealMeatConsumed }, _vm.Answers.MealDays[0].Meals["LUNCH"]);
        }

        [Test]
        public async Task ToggleMeal_OnlyOpenTypes()
        {
            var inputs = CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A1.3", Now));
            inputs.LoggedMealTypes[Now.Date] = new HashSet<string> { "LUNCH" };
            _checkIn.Setup(c => c.LoadPlanAsync(null)).ReturnsAsync((CheckInPlanner.Plan(inputs), (ApiErrorResponse)null));
            await _vm.LoadAsync(null);

            _vm.ToggleMeal(0, "LUNCH");

            Assert.IsFalse(_vm.Answers.MealDays[0].Meals.ContainsKey("LUNCH"));
        }

        [Test]
        public async Task SummaryStep_IsInvalidWhenNothingToSend()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.B2.1"));
            Assert.IsFalse(_vm.IsStepValid(_vm.SummaryStepIndex));

            _vm.SetCount(0, 2);

            Assert.IsTrue(_vm.IsStepValid(_vm.SummaryStepIndex));
        }

        [Test]
        public async Task Complete_SendsGroupedItems_AndRaisesOutcome()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.A1.3", Now), CheckInPlannerTests.M("M.B2.1"));
            _vm.ToggleMeal(0, "DINNER");
            _vm.ToggleMealEvent(0, "DINNER", ClientEventTypes.MealLegumeConsumed);
            _vm.SetCount(1, 1);
            CheckInOutcome outcome = null;
            _vm.CheckInCompleted += o => outcome = o;

            await CompleteAsync();

            Assert.AreEqual(1, _sent.Count);
            Assert.AreEqual(2, _sent[0].Count);
            Assert.AreEqual(2, outcome.ItemsSent);
            Assert.IsNull(outcome.SingleMissionProgress);
        }

        [Test]
        public async Task Complete_SingleMission_RefreshesThatMissionProgress()
        {
            _missions.Setup(m => m.GetMissionProgressAsync("M.B2.1", null)).ReturnsAsync((new MissionProgress { missionCode = "M.B2.1", progress = 60 }, (ApiErrorResponse)null));
            await LoadAsync("M.B2.1", CheckInPlannerTests.M("M.B2.1"));
            _vm.SetCount(0, 3);
            CheckInOutcome outcome = null;
            _vm.CheckInCompleted += o => outcome = o;

            await CompleteAsync();

            Assert.AreEqual(60f, outcome.SingleMissionProgress.progress);
        }

        [Test]
        public async Task Complete_WhenSendFails_SetsErrorAndRetryReusesSameItems()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.B2.1"));
            _vm.SetCount(0, 1);
            var error = new ApiErrorResponse { message = "offline" };
            var lists = new List<IReadOnlyList<PendingReportItem>>();
            _emitter.Setup(e => e.SendAsync(It.IsAny<IReadOnlyList<PendingReportItem>>()))
                .Callback<IReadOnlyList<PendingReportItem>>(lists.Add)
                .ReturnsAsync(() => lists.Count == 1 ? MissionReportSendResult.Fail(error) : MissionReportSendResult.Ok);
            bool completed = false;
            _vm.CheckInCompleted += _ => completed = true;

            await CompleteAsync();
            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.IsFalse(completed);

            await _vm.GoNextAsync();

            Assert.AreEqual(2, lists.Count);
            Assert.AreSame(lists[0], lists[1]);
            Assert.IsTrue(completed);
        }

        [Test]
        public async Task Mutators_AfterPartialSend_AreIgnored()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.B2.1"));
            _vm.SetCount(0, 2);
            _emitter.Setup(e => e.SendAsync(It.IsAny<IReadOnlyList<PendingReportItem>>()))
                .Callback<IReadOnlyList<PendingReportItem>>(items => items[0].Sent = true)
                .ReturnsAsync(MissionReportSendResult.Fail(new ApiErrorResponse { message = "x" }));

            await CompleteAsync();
            Assert.IsTrue(_vm.HasPartialSend);

            _vm.SetCount(0, 5);

            Assert.AreEqual(2, _vm.Answers.MissionSteps[0].Count);
        }

        [Test]
        public async Task ToggleEventDay_OnlyOpenDays()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.A5.4"));

            _vm.ToggleEventDay(0, Now.Date.AddDays(1));
            _vm.ToggleEventDay(0, Now.Date.AddDays(-1));
            Assert.AreEqual(0, _vm.Answers.DayEvents[0].Count, "only today is open");

            _vm.ToggleEventDay(0, Now.Date);
            Assert.AreEqual(1, _vm.Answers.DayEvents[0].Count);

            _vm.ToggleEventDay(0, Now.Date);
            Assert.AreEqual(0, _vm.Answers.DayEvents[0].Count);
        }

        [Test]
        public async Task BuildSummary_HasOneLinePerStepWithSomethingToSend()
        {
            await LoadAsync(null, CheckInPlannerTests.M("M.A1.3", Now), CheckInPlannerTests.M("M.B2.1"));
            _vm.ToggleMeal(0, "LUNCH");
            _vm.ToggleMealEvent(0, "LUNCH", ClientEventTypes.MealLegumeConsumed);

            var summary = _vm.BuildSummary();

            Assert.AreEqual(1, summary.Count);
            Assert.AreEqual(CheckInStepKind.MealDay, summary[0].Kind);
            Assert.AreEqual(Now.Date, summary[0].Day);
            Assert.AreEqual(1, summary[0].Count);
        }

        [Test]
        public async Task LoadAsync_WhenServiceFails_SetsErrorDetail()
        {
            var error = new ApiErrorResponse { message = "x" };
            _checkIn.Setup(c => c.LoadPlanAsync(null)).ReturnsAsync(((CheckInPlan)null, error));

            await _vm.LoadAsync(null);

            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }
    }
}
