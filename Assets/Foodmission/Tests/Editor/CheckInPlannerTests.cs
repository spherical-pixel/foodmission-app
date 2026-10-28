using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInPlannerTests
    {
        internal static readonly DateTime Now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Local);

        internal static CheckInMission M(string code, DateTime? start = null) =>
            new CheckInMission(code, "T " + code, MissionInteractionCatalog.Get(code), start);

        internal static CheckInInputs Inputs(params CheckInMission[] missions)
        {
            var inputs = new CheckInInputs { NowLocal = Now };
            inputs.Missions.AddRange(missions);
            return inputs;
        }

        [Test]
        public void MealDays_CoverMissionWindow_AndSkipFullyLoggedDays()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-2)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-1)] = new HashSet<string> { "BREAKFAST", "LUNCH", "DINNER" };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date }, plan.MealDays.Select(d => d.Day).ToArray());
        }

        [Test]
        public void MealDay_OpenTypes_AreTheUnloggedOnes()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-1)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-1)] = new HashSet<string> { "LUNCH" };

            var day = CheckInPlanner.Plan(inputs).MealDays.First();

            CollectionAssert.AreEqual(new[] { "BREAKFAST", "SNACK", "DINNER" }, day.OpenMealTypes);
            Assert.IsTrue(day.HasAnyLog);
        }

        [Test]
        public void MealQuestions_AreTheUnionOfActiveMealMissions_PerDayWindow()
        {
            // A1.3 legumes since 2 days ago; B1.4 protein swaps since today
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date.AddDays(-2)), M("M.B1.4", Now.Date)));

            var older = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-2));
            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealLegumeConsumed }, older.Questions.Select(q => q.EventType).ToArray());

            var today = plan.MealDays.Single(d => d.Day == Now.Date);
            Assert.AreEqual(1 + 6, today.Questions.Count, "legumes + 6 protein swaps");
            Assert.IsTrue(today.Questions.Where(q => q.IsSwap).All(q => q.LabelKey == q.EventType));
            Assert.AreEqual(ClientEventTypes.MealLegumeConsumed, today.Questions[0].EventType, "flags before swaps");
        }

        [Test]
        public void MealQuestions_AreDeduplicated()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.1", Now.Date), M("M.B1.3", Now.Date)));

            Assert.AreEqual(1, plan.MealDays.Single().Questions.Count(q => q.EventType == ClientEventTypes.MealMeatConsumed));
        }

        [Test]
        public void MealDay_Hints_ComeFromTheMissionsAskedThatDay()
        {
            // M.A6.4 asks the fibre flag too: the fibre-only hint of M.I6.2 must not show for it alone
            var onlyDense = CheckInPlanner.Plan(Inputs(M("M.A6.4", Now.Date))).MealDays.Single();
            CollectionAssert.AreEqual(new[] { "MISSION_HINT_NUTRIENT_DENSE_MEALS" }, onlyDense.HintKeys);

            var plan = CheckInPlanner.Plan(Inputs(M("M.I6.2", Now.Date.AddDays(-1)), M("M.A6.4", Now.Date)));
            CollectionAssert.AreEqual(new[] { "MISSION_HINT_HIGH_FIBRE_MEAL" }, plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1)).HintKeys);
            CollectionAssert.AreEqual(new[] { "MISSION_HINT_HIGH_FIBRE_MEAL", "MISSION_HINT_NUTRIENT_DENSE_MEALS" },
                plan.MealDays.Single(d => d.Day == Now.Date).HintKeys);
        }

        [Test]
        public void MealDay_Hints_AreEmptyForMissionsWithoutOne()
        {
            Assert.IsEmpty(CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date))).MealDays.Single().HintKeys);
        }

        [TestCase("M.A1.1")]
        [TestCase("M.B1.3")]
        public void MeatCountingMissions_AskMeatFreeAndMeat(string code)
        {
            // Only "had meat?" left users unsure what an unchecked meal meant
            var day = CheckInPlanner.Plan(Inputs(M(code, Now.Date))).MealDays.Single();

            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed },
                day.Questions.Select(q => q.EventType).ToArray());
        }

        [Test]
        public void BreakfastOnlyFlag_AppliesOnlyToBreakfast_UnlessAnotherMissionNeedsItAnywhere()
        {
            var breakfastOnly = CheckInPlanner.Plan(Inputs(M("M.A6.2", Now.Date))).MealDays.Single();
            var q = breakfastOnly.Questions.Single();
            Assert.AreEqual("BREAKFAST", q.OnlyMealType);
            Assert.AreEqual(0, breakfastOnly.QuestionsFor("LUNCH").Count);
            Assert.AreEqual(1, breakfastOnly.QuestionsFor("BREAKFAST").Count);
            Assert.AreEqual("EVENTS_HIGH_FIBRE", q.LabelKey);
            CollectionAssert.AreEqual(new[] { "BREAKFAST" }, breakfastOnly.OpenMealTypes.ToArray(), "meals with nothing to ask aren't offered");
        }

        [TestCase("M.I6.2", 1)]
        [TestCase("M.A6.4", 4)]
        public void MainMealFlags_AreAskedForMainMealsOnly(string code, int flags)
        {
            // Rules count breakfast, lunch and dinner only: a flagged snack would be wasted
            var day = CheckInPlanner.Plan(Inputs(M(code, Now.Date))).MealDays.Single();

            Assert.AreEqual(flags, day.Questions.Count);
            foreach (CheckInMealQuestion q in day.Questions)
            {
                Assert.IsNull(q.OnlyMealType);
                CollectionAssert.AreEquivalent(CheckInMealTypes.Main, q.MealTypes.ToArray());
            }
            Assert.AreEqual(0, day.QuestionsFor("SNACK").Count);
        }

        [Test]
        public void MainMealFlag_SharedWithAnAnyMealMission_IsAskedForEveryMeal()
        {
            // M.I6.4 (healthy fat, any meal) and M.A6.4 (main meals) ask the same flag
            var day = CheckInPlanner.Plan(Inputs(M("M.A6.4", Now.Date), M("M.I6.4", Now.Date))).MealDays.Single();

            var fat = day.Questions.Single(q => q.EventType == ClientEventTypes.NutritionHealthyFatChosen);
            CollectionAssert.AreEquivalent(CheckInMealTypes.All, fat.MealTypes.ToArray());
            Assert.AreEqual(1, day.QuestionsFor("SNACK").Count);
        }

        [Test]
        public void MealDays_StartDay_OffersOnlyMealsDatedAfterTheStart()
        {
            // Rules ignore events before startedAt: a 13:00 lunch can't count for a mission started at 14:00
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date.AddDays(-1).AddHours(14))));

            CheckInMealDay startDay = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1));
            CollectionAssert.AreEqual(new[] { "SNACK", "DINNER" }, startDay.OpenMealTypes.ToArray());
            Assert.AreEqual(0, startDay.QuestionsFor("LUNCH").Count);
            Assert.AreEqual(1, startDay.QuestionsFor("DINNER").Count);
        }

        [Test]
        public void MealDays_StartDay_KeepsQuestionsOfMissionsStartedEarlier()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date.AddDays(-1).AddHours(14)), M("M.B1.4", Now.Date.AddDays(-3))));

            CheckInMealDay day = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1));
            Assert.AreEqual(CheckInMealTypes.All.Length, day.OpenMealTypes.Count);
            Assert.IsFalse(day.QuestionsFor("LUNCH").Any(q => q.EventType == ClientEventTypes.MealLegumeConsumed), "A1.3 had not started at lunch");
            Assert.IsTrue(day.QuestionsFor("LUNCH").All(q => q.IsSwap));
            Assert.IsTrue(day.QuestionsFor("DINNER").Any(q => q.EventType == ClientEventTypes.MealLegumeConsumed));
        }

        [Test]
        public void MealDays_StartedAfterTheLastMeal_OffersNothingThatDay()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-1).AddHours(21)));

            var plan = CheckInPlanner.Plan(inputs);

            Assert.IsFalse(plan.MealDays.Any(d => d.Day == Now.Date.AddDays(-1)));
            Assert.AreEqual(0, plan.PendingPastDays(Now));
        }

        [Test]
        public void MissionsPastTheirDeadline_AreNotOffered()
        {
            // Ended at 15:00 today; the backend fails it within the hour, and nothing reported now would count
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-7).AddHours(15)), M("M.A5.4", Now.Date.AddDays(-7).AddHours(15)));
            inputs.NowLocal = Now.Date.AddHours(18);

            Assert.IsTrue(CheckInPlanner.Plan(inputs).IsEmpty);

            inputs.NowLocal = Now.Date.AddHours(14);
            Assert.IsFalse(CheckInPlanner.Plan(inputs).IsEmpty, "still inside the window");
        }

        [Test]
        public void DayEvents_ExcludeCoveredDays_AndRespectMissionStart()
        {
            var inputs = Inputs(M("M.A5.4", Now.AddDays(-3)));
            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized] = new HashSet<DateTime> { Now.Date.AddDays(-2) };

            var plan = CheckInPlanner.Plan(inputs);

            var de = plan.DayEvents.Single();
            Assert.AreEqual("M.A5.4", de.MissionCode);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1), Now.Date }, de.OpenDays);
            Assert.AreEqual(Now.AddDays(-3), de.StartLocal);
            Assert.AreEqual(0, plan.MealDays.Count);

            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized].UnionWith(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1), Now.Date });
            Assert.AreEqual(0, CheckInPlanner.Plan(inputs).DayEvents.Count, "every day already recorded");
        }

        [Test]
        public void NonMealEventSteps_AreMissionSteps()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A3.3"), M("M.A5.5")));

            CollectionAssert.AreEqual(new[] { "M.A3.3", "M.A3.3", "M.A3.3", "M.A5.5", "M.A5.5" }, plan.MissionSteps.Select(s => s.MissionCode).ToArray());
            Assert.AreEqual(5, plan.StepCount);
        }

        [Test]
        public void PendingRuleAndUnknownMissions_AreIgnored_AndEmptyPlanIsEmpty()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.Z9.9"), M("M.Z9.9")));

            Assert.IsTrue(plan.IsEmpty);
            Assert.AreEqual(0, plan.StepCount);
        }

        [Test]
        public void PendingPastDays_CountsPastDaysWithoutAnyLog_OrWithOpenDayEvents()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-3)), M("M.A5.4", Now.AddDays(-1)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-3)] = new HashSet<string> { "LUNCH" }; // partially logged: not "missing"
            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized] = new HashSet<DateTime>();

            var plan = CheckInPlanner.Plan(inputs);

            // -2 and -1 have no meals; FIFO is open on -1 too. Today never counts.
            Assert.AreEqual(2, plan.PendingPastDays(Now));
        }

        [Test]
        public void MealDays_Today_OffersMealTypesFromTwoHoursBeforeTheirTypicalHour()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-1)));
            inputs.NowLocal = Now.Date.AddHours(10);

            var plan = CheckInPlanner.Plan(inputs);

            CheckInMealDay today = plan.MealDays.Single(d => d.Day == Now.Date);
            CollectionAssert.AreEqual(new[] { "BREAKFAST" }, today.OpenMealTypes.ToArray());
            CheckInMealDay yesterday = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1));
            Assert.AreEqual(CheckInMealTypes.All.Length, yesterday.OpenMealTypes.Count, "past days offer every meal type");

            inputs.NowLocal = Now.Date.AddHours(11);
            CollectionAssert.AreEqual(new[] { "BREAKFAST", "LUNCH" }, CheckInPlanner.Plan(inputs).MealDays.Single(d => d.Day == Now.Date).OpenMealTypes.ToArray());
        }

        [Test]
        public void MealDays_TodayWithEveryOfferedMealLogged_IsSkipped()
        {
            var inputs = Inputs(M("M.A1.3"));
            inputs.NowLocal = Now.Date.AddHours(10);
            inputs.LoggedMealTypes[Now.Date] = new HashSet<string> { "BREAKFAST" };

            Assert.IsFalse(CheckInPlanner.Plan(inputs).MealDays.Any(d => d.Day == Now.Date));
        }

        private static void Record(CheckInInputs inputs, DateTime day, params string[] colours)
        {
            if (!inputs.RecordedValues.TryGetValue(ClientEventTypes.NutritionRainbowColoursLogged, out var byDay))
            {
                byDay = new Dictionary<DateTime, HashSet<string>>();
                inputs.RecordedValues[ClientEventTypes.NutritionRainbowColoursLogged] = byDay;
            }
            byDay[day] = new HashSet<string>(colours);
        }

        [Test]
        public void DayChecklist_OpenDaysAreWindowDays_WithRecordedValues()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date.AddDays(-2)));
            Record(inputs, Now.Date.AddDays(-1), "GREEN", "RED");

            var step = CheckInPlanner.Plan(inputs).MissionSteps.Single();

            Assert.AreEqual(MissionStepType.DayChecklist, step.Step.Type);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date.AddDays(-1), Now.Date }, step.OpenDays.ToArray());
            CollectionAssert.AreEquivalent(new[] { "GREEN", "RED" }, step.RecordedValuesFor(Now.Date.AddDays(-1)).ToArray());
            Assert.AreEqual(0, step.RecordedValuesFor(Now.Date).Count);
            Assert.AreEqual(Now.Date.AddDays(-2), step.StartLocal);
        }

        [Test]
        public void DayChecklist_DaysAlreadyCounting_AreNotOpen()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date.AddDays(-1)));
            Record(inputs, Now.Date.AddDays(-1), "GREEN", "RED", "ORANGE", "YELLOW", "PURPLE");

            var step = CheckInPlanner.Plan(inputs).MissionSteps.Single();

            CollectionAssert.AreEqual(new[] { Now.Date }, step.OpenDays.ToArray());
        }

        [Test]
        public void DayChecklist_NoOpenDay_StepIsSkipped()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date));
            Record(inputs, Now.Date, "GREEN", "RED", "ORANGE", "YELLOW", "PURPLE", "WHITE");

            Assert.IsTrue(CheckInPlanner.Plan(inputs).IsEmpty);
        }

        [Test]
        public void PendingPastDays_CountsDayChecklistDaysWithNothingRecorded()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date.AddDays(-2)));
            Record(inputs, Now.Date.AddDays(-1), "GREEN");

            // -2 has nothing; -1 has something (not "untold"); today doesn't count as past
            Assert.AreEqual(1, CheckInPlanner.Plan(inputs).PendingPastDays(Now));
        }

        [Test]
        public void ProductChecklist_IsMissionStep_AndAnswersStartWithOneEmptyProduct()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A2.1", Now.Date)));

            Assert.AreEqual(MissionStepType.ProductChecklist, plan.MissionSteps.Single().Step.Type);
            Assert.AreEqual(0, plan.MissionSteps.Single().OpenDays.Count);
            var answers = CheckInAnswers.For(plan);
            Assert.AreEqual(1, answers.MissionSteps.Single().Products.Count);
            Assert.AreEqual(0, answers.MissionSteps.Single().Products[0].Count);
        }

        private static CheckInProductReport Report(string type, string id, DateTime at, string[] values = null, string metaKey = null, string metaValue = null) =>
            new CheckInProductReport(type, id, at, values, metaKey == null ? null : new Dictionary<string, string> { { metaKey, metaValue } });

        [Test]
        public void ProductStep_CountsRealIdsFromProducts_AfterTheStart()
        {
            var inputs = Inputs(M("M.B2.1", Now.Date.AddDays(-2)));
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingOriginChecked, "p-1", Now.Date.AddDays(-1)));
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingOriginChecked, "p-2", Now.Date.AddDays(-3))); // before the start
            // Reported before and again after the start: counts
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingOriginChecked, "p-3", Now.Date.AddDays(-3)));
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingOriginChecked, "p-3", Now.Date.AddDays(-1)));

            CheckInMissionStep step = CheckInPlanner.Plan(inputs).MissionSteps.Single(s => s.MissionCode == "M.B2.1");

            Assert.AreEqual(2, step.FromProducts);
        }

        [Test]
        public void ChecklistStep_CountsOnlyProductsThatMeetTheRule()
        {
            var inputs = Inputs(M("M.A2.1", Now.Date.AddDays(-2))); // origin required + 2 of 4
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingOriginChecked, "p-1", Now.Date.AddDays(-1)));
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingOriginChecked, "p-2", Now.Date.AddDays(-1)));
            inputs.ProductReports.Add(Report(ClientEventTypes.ShoppingCertificationChosen, "p-2", Now.Date.AddDays(-1)));

            CheckInMissionStep step = CheckInPlanner.Plan(inputs).MissionSteps.Single(s => s.MissionCode == "M.A2.1");

            Assert.AreEqual(1, step.FromProducts, "p-1 has only origin");
        }

        [Test]
        public void ProductStep_WithFixedMetadata_CountsOnlyMatchingReports()
        {
            var inputs = Inputs(M("M.A3.1", Now.Date.AddDays(-2))); // NOVA score compared with GREEN_SCORE
            inputs.ProductReports.Add(Report(ClientEventTypes.ProcessingNovaScoreCompared, "p-1", Now.Date.AddDays(-1), null, "comparedWith", "GREEN_SCORE"));
            inputs.ProductReports.Add(Report(ClientEventTypes.ProcessingNovaScoreCompared, "p-2", Now.Date.AddDays(-1)));

            CheckInMissionStep step = CheckInPlanner.Plan(inputs).MissionSteps.Single(s => s.MissionCode == "M.A3.1");

            Assert.AreEqual(1, step.FromProducts);
        }

        // ── Day questions (spec 2026-10-27) ───────────────────

        private static readonly DateTime DayStart = Now.Date.AddDays(-3).AddHours(9);

        internal static CheckInMission MP(string code, float progress, DateTime? start = null) =>
            new CheckInMission(code, "T " + code, MissionInteractionCatalog.Get(code), start ?? DayStart, progress);

        private static IEnumerable<DateTime> DaysAsking(CheckInPlan plan, string code) =>
            plan.MealDays.Where(d => d.DayQuestions.Any(q => q.MissionCode == code)).Select(d => d.Day);

        [Test]
        public void Plan_FlagDayQuestion_OnDaysWithoutTheFlag()
        {
            var inputs = Inputs(MP("M.B6.2", 0f));
            inputs.CoveredEventDays[ClientEventTypes.NutritionFruitVegServingAdded] = new HashSet<DateTime> { Now.Date.AddDays(-2) };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1), Now.Date }, DaysAsking(plan, "M.B6.2").ToArray());
        }

        [Test]
        public void Plan_LoggedDayQuestion_SkipsDaysWithAnyLog()
        {
            var inputs = Inputs(MP("M.I1.3", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "LUNCH" };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.DoesNotContain(DaysAsking(plan, "M.I1.3").ToArray(), Now.Date.AddDays(-2));
            CollectionAssert.Contains(DaysAsking(plan, "M.I1.3").ToArray(), Now.Date.AddDays(-1));
        }

        [Test]
        public void Plan_MeatDayQuestion_DerivedWhenMealBlockAsksMeat()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.I1.3", 0f), MP("M.B1.3", 0f)));

            var meatQuestions = plan.MealDays.SelectMany(d => d.DayQuestions).Where(q => q.MissionCode == "M.I1.3").ToList();
            Assert.IsNotEmpty(meatQuestions);
            Assert.IsTrue(meatQuestions.All(q => q.DerivedFromMeals));
            Assert.IsTrue(plan.MealDays.All(d => d.Questions.Any(q => q.EventType == ClientEventTypes.MealMeatConsumed)));
        }

        [Test]
        public void Plan_SecuredMission_IsListedWithItsStart()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.I1.3", 1f), MP("M.A6.3", 1f)));

            var secured = plan.Secured.Single();
            Assert.AreEqual("M.I1.3", secured.Code);
            Assert.AreEqual(DayStart, secured.StartLocal);
        }

        [Test]
        public void Plan_FlagDayQuestion_JoinsTheMealBlockWhenItsFlagsAreAskedPerMeal()
        {
            // B5.1 / B5.2 ask the plate flags per meal: B5.5 is answered there, with its missing flag added to the meals
            var plan = CheckInPlanner.Plan(Inputs(MP("M.B5.5", 0f), MP("M.B5.1", 0f), MP("M.B5.2", 0f)));

            Assert.IsEmpty(DaysAsking(plan, "M.B5.5"));
            var day = plan.MealDays.First();
            CollectionAssert.IsSupersetOf(day.Questions.Select(q => q.EventType).ToArray(), new[]
            {
                ClientEventTypes.FoodWasteHalfPlateSaved, ClientEventTypes.FoodWasteFullPlateSaved, ClientEventTypes.FoodWasteExpiredConsumed
            });
            Assert.AreEqual(1, day.Questions.Count(q => q.EventType == ClientEventTypes.FoodWasteHalfPlateSaved), "no duplicate chips");
        }

        [Test]
        public void Plan_FlagDayQuestion_StaysOnADayWithoutMealBlock()
        {
            var inputs = Inputs(MP("M.B5.5", 0f), MP("M.B5.1", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "BREAKFAST", "LUNCH", "DINNER" };

            CollectionAssert.Contains(DaysAsking(CheckInPlanner.Plan(inputs), "M.B5.5").ToArray(), Now.Date.AddDays(-2));
        }

        [Test]
        public void Plan_DecidedDay_NotAskedAgain()
        {
            var inputs = Inputs(MP("M.B6.2", 0f));
            inputs.DecidedDays["M.B6.2"] = new HashSet<DateTime> { Now.Date.AddDays(-2) };

            CollectionAssert.DoesNotContain(DaysAsking(CheckInPlanner.Plan(inputs), "M.B6.2").ToArray(), Now.Date.AddDays(-2));
        }

        [Test]
        public void Plan_MissionWithEnoughProgress_AsksNothing()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.I1.3", 1f)));

            Assert.IsTrue(plan.IsEmpty);
        }

        [Test]
        public void Plan_Remaining_FromProgress()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.B5.5", 0.5f)));

            Assert.IsTrue(plan.MealDays.SelectMany(d => d.DayQuestions).All(q => q.Remaining == 2));
        }

        [Test]
        public void Plan_LimitMission_NeverPruned()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.A6.3", 1f)));

            Assert.AreEqual(4, DaysAsking(plan, "M.A6.3").Count());
            Assert.IsTrue(plan.MealDays.SelectMany(d => d.DayQuestions).All(q => q.Remaining == null));
        }

        [Test]
        public void Plan_AllMealsQuestion_OnLoggedDayWithOpenMeals()
        {
            var inputs = Inputs(MP("M.A6.3", 0f), MP("M.A1.3", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "BREAKFAST" };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.Contains(DaysAsking(plan, "M.A6.3").ToArray(), Now.Date.AddDays(-2));
        }

        [Test]
        public void Plan_AllMealsQuestion_NotOnLoggedDayWithoutMealBlock()
        {
            var inputs = Inputs(MP("M.A6.3", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "BREAKFAST" };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.DoesNotContain(DaysAsking(plan, "M.A6.3").ToArray(), Now.Date.AddDays(-2));
        }

        [Test]
        public void Plan_TodayDayQuestion_OfferedAllDay()
        {
            var inputs = Inputs(MP("M.B6.2", 0f));
            inputs.NowLocal = Now.Date.AddHours(8);

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.Contains(DaysAsking(plan, "M.B6.2").ToArray(), Now.Date);
        }

        [Test]
        public void Plan_DayWithOnlyDayQuestions_IsEmitted()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.B6.2", 0f)));

            Assert.AreEqual(4, plan.MealDays.Count);
            Assert.IsTrue(plan.MealDays.All(d => d.OpenMealTypes.Count == 0 && d.Questions.Count == 0));
            Assert.AreEqual("T M.B6.2", plan.MealDays[0].DayQuestions.Single().MissionTitle);
        }

        [Test]
        public void Plan_FullyLoggedDay_KeepsItsDayQuestions()
        {
            var inputs = Inputs(MP("M.B6.2", 0f), MP("M.A1.3", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "BREAKFAST", "LUNCH", "DINNER" };

            var day = CheckInPlanner.Plan(inputs).MealDays.Single(d => d.Day == Now.Date.AddDays(-2));

            Assert.IsEmpty(day.OpenMealTypes);
            Assert.AreEqual("M.B6.2", day.DayQuestions.Single().MissionCode);
        }

        [Test]
        public void PendingPastDays_DayQuestionsCountOnlyOnDaysWithoutLogs()
        {
            // A day with a meal log was told about, even if a day question is still open on it
            var inputs = Inputs(MP("M.B6.2", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "LUNCH" };

            var plan = CheckInPlanner.Plan(inputs);

            Assert.AreEqual(2, plan.PendingPastDays(Now));
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1) }, plan.WaitingMissions(Now).Single().Days.ToArray());
        }

        [Test]
        public void WaitingMissions_GroupsPastDaysPerMission()
        {
            var inputs = Inputs(MP("M.B6.2", 0f), MP("M.A5.4", 0f, Now.Date.AddDays(-1).AddHours(9)));
            inputs.CoveredEventDays[ClientEventTypes.NutritionFruitVegServingAdded] = new HashSet<DateTime> { Now.Date.AddDays(-3) };

            var waiting = CheckInPlanner.Plan(inputs).WaitingMissions(Now);

            var fruit = waiting.Single(w => w.Code == "M.B6.2");
            Assert.AreEqual("T M.B6.2", fruit.Title);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date.AddDays(-1) }, fruit.Days.ToArray());
            var fifo = waiting.Single(w => w.Code == "M.A5.4");
            Assert.AreEqual("T M.A5.4", fifo.Title);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-1) }, fifo.Days.ToArray());
        }

        [Test]
        public void WaitingMissions_MealMissionsOnlyOnDaysWithoutLogs()
        {
            var inputs = Inputs(MP("M.A1.3", 0f));
            inputs.LoggedMealTypes[Now.Date.AddDays(-2)] = new HashSet<string> { "LUNCH" };

            var waiting = CheckInPlanner.Plan(inputs).WaitingMissions(Now).Single();

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1) }, waiting.Days.ToArray());
        }

        // ── Day swaps (spec 2026-10-27 swaps) ─────────────────

        private static CheckInSwapQuestion SwapsOn(CheckInPlan plan, DateTime day) =>
            plan.MealDays.FirstOrDefault(d => d.Day == day)?.SwapQuestion;

        [Test]
        public void SwapMission_Alone_GetsOneSwapQuestionPerOpenDay()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.B6.5", 0f, Now.Date.AddDays(-2).AddHours(9))));

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date.AddDays(-1), Now.Date },
                plan.MealDays.Where(d => d.SwapQuestion != null).Select(d => d.Day).ToArray());
            CheckInSwapQuestion question = SwapsOn(plan, Now.Date);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapSnackToFruitNuts }, question.Swaps);
            Assert.AreEqual(5, question.Missions.Single().Remaining);
            Assert.IsTrue(plan.MealDays.All(d => d.Questions.Count == 0), "never per meal on its own");
        }

        [Test]
        public void SwapMissions_ShareOneQuestion_UnionInCatalogOrder()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.I1.1", 0f), MP("M.I1.2", 0f)));

            CheckInSwapQuestion question = SwapsOn(plan, Now.Date);
            CollectionAssert.AreEqual(new[] { "M.I1.1", "M.I1.2" }, question.Missions.Select(m => m.Code).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                ClientEventTypes.SwapBeefToPork, ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes,
                ClientEventTypes.SwapPorkToChicken, ClientEventTypes.SwapPorkToLegumes, ClientEventTypes.SwapChickenToLegumes
            }, question.Swaps);
        }

        [Test]
        public void SwapMission_WithPerMealMission_StaysInMealBlock()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.B1.3", Now.Date), M("M.B1.5", Now.Date)));

            Assert.IsTrue(plan.MealDays.All(d => d.SwapQuestion == null));
            CollectionAssert.IsSupersetOf(plan.MealDays.Single().Questions.Select(q => q.EventType).ToArray(),
                new[] { ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes });
        }

        [Test]
        public void SwapMission_WithResolvedPerMealMissions_IsAskedPerDay()
        {
            // B1.3 started 8 days ago: past its deadline, so nothing is asked per meal any more
            var plan = CheckInPlanner.Plan(Inputs(M("M.B1.3", Now.Date.AddDays(-8)), M("M.B1.5", Now.Date)));

            Assert.IsNotNull(SwapsOn(plan, Now.Date));
            Assert.IsTrue(plan.MealDays.All(d => d.Questions.Count == 0));
        }

        [Test]
        public void SwapMission_WithPerMealMissionOutsideTheCheckIn_StaysInMealBlock()
        {
            var inputs = Inputs(M("M.B1.5", Now.Date));
            inputs.QuestMissions.Add(M("M.B1.3", Now.Date));

            var plan = CheckInPlanner.Plan(inputs);

            Assert.IsTrue(plan.MealDays.All(d => d.SwapQuestion == null));
            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.SwapBeefToChicken, ClientEventTypes.SwapBeefToLegumes },
                plan.MealDays.Single().Questions.Select(q => q.EventType).ToArray());
        }

        [Test]
        public void CountMission_AtTarget_IsNotAsked()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.B6.3", 1f)));

            Assert.IsTrue(plan.MealDays.All(d => d.SwapQuestion == null));
        }

        [Test]
        public void CountMission_Remaining_FromProgress()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.I3.3", 0.5f)));

            Assert.AreEqual(2, SwapsOn(plan, Now.Date).Missions.Single().Remaining);
        }

        [Test]
        public void LevelMission_MissingLevels_FromLoggedSwaps()
        {
            var inputs = Inputs(MP("M.I1.2", 0.34f));
            inputs.LoggedSwaps.Add((DayStart.AddDays(1).AddHours(4), ClientEventTypes.SwapBeefToLegumes));

            CheckInSwapMission mission = SwapsOn(CheckInPlanner.Plan(inputs), Now.Date).Missions.Single();

            Assert.IsNull(mission.Remaining);
            CollectionAssert.AreEqual(new[] { 0, 2 }, mission.MissingLevels);
        }

        [Test]
        public void LevelMission_IgnoresSwapsBeforeStart()
        {
            var inputs = Inputs(MP("M.I1.2", 0f));
            inputs.LoggedSwaps.Add((DayStart.AddHours(-1), ClientEventTypes.SwapBeefToLegumes));

            CheckInSwapQuestion question = SwapsOn(CheckInPlanner.Plan(inputs), DayStart.Date);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, question.Missions.Single().MissingLevels);
            Assert.AreEqual(1, question.LoggedCount(ClientEventTypes.SwapBeefToLegumes), "still locked on that day");
        }

        [Test]
        public void LoggedSwaps_AreCountedPerDay()
        {
            var inputs = Inputs(MP("M.B6.5", 0f));
            inputs.LoggedSwaps.Add((Now.Date.AddDays(-1).AddHours(11), ClientEventTypes.SwapSnackToFruitNuts));
            inputs.LoggedSwaps.Add((Now.Date.AddDays(-1).AddHours(17), ClientEventTypes.SwapSnackToFruitNuts));

            var plan = CheckInPlanner.Plan(inputs);

            Assert.AreEqual(2, SwapsOn(plan, Now.Date.AddDays(-1)).LoggedCount(ClientEventTypes.SwapSnackToFruitNuts));
            Assert.AreEqual(0, SwapsOn(plan, Now.Date).LoggedCount(ClientEventTypes.SwapSnackToFruitNuts));
        }

        [Test]
        public void DecidedDay_IsNotAskedForThatMission()
        {
            var inputs = Inputs(MP("M.B6.5", 0f));
            inputs.DecidedDays["M.B6.5"] = new HashSet<DateTime> { Now.Date.AddDays(-1) };

            var plan = CheckInPlanner.Plan(inputs);

            Assert.IsNull(SwapsOn(plan, Now.Date.AddDays(-1)));
            Assert.IsNotNull(SwapsOn(plan, Now.Date));
        }

        [Test]
        public void SwapOnlyDay_IsNotPendingNorWaiting()
        {
            var plan = CheckInPlanner.Plan(Inputs(MP("M.B6.5", 0f)));

            Assert.IsNotNull(SwapsOn(plan, Now.Date.AddDays(-2)));
            Assert.AreEqual(0, plan.PendingPastDays(Now));
            Assert.IsEmpty(plan.WaitingMissions(Now));
        }
    }
}
