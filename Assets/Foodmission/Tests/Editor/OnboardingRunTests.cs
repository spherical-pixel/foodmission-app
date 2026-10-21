using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingRunTests
    {
        private OnboardingRun _run;

        [SetUp]
        public void SetUp()
        {
            HomePromptSession.Reset();
            _run = new OnboardingRun();
            _run.Start();
        }

        [TearDown]
        public void TearDown()
        {
            HomePromptSession.Reset();
        }

        private static AppState AnsweredSurveyState()
        {
            return new AppState
            {
                userOnboardingSurvey = new OnboardingSurveyData
                {
                    answers = new[] { new OnboardingSurveyAnswer { field = "weeklyMeatConsumption", value = "3" } }
                }
            };
        }

        private static OnboardingStepState[] States(params OnboardingStepState[] states) => states;

        [Test]
        public void NewRun_FreshUser_ProfileCurrent_RestPending()
        {
            CollectionAssert.AreEqual(
                States(OnboardingStepState.Current, OnboardingStepState.Pending, OnboardingStepState.Pending, OnboardingStepState.Pending),
                _run.GetStates(new AppState()));
            Assert.AreEqual(OnboardingStep.Profile, _run.GetCurrent(new AppState()));
        }

        [Test]
        public void NotActive_MarkDone_IsIgnored()
        {
            _run.End();

            _run.MarkDone(OnboardingStep.Profile);

            Assert.IsFalse(_run.IsActive);
            Assert.AreEqual(OnboardingStep.Profile, _run.GetCurrent(new AppState()));
        }

        [Test]
        public void MarkSkipped_Profile_GoalsBecomesCurrent()
        {
            _run.MarkSkipped(OnboardingStep.Profile);

            CollectionAssert.AreEqual(
                States(OnboardingStepState.Skipped, OnboardingStepState.Current, OnboardingStepState.Pending, OnboardingStepState.Pending),
                _run.GetStates(new AppState()));
        }

        [Test]
        public void StateDone_ReminderUser_SkipsToFirstPending()
        {
            AppState state = AnsweredSurveyState();
            state.userGoals = new[] { "PROTEIN" };

            CollectionAssert.AreEqual(
                States(OnboardingStepState.Current, OnboardingStepState.Done, OnboardingStepState.Done, OnboardingStepState.Pending),
                _run.GetStates(state));
        }

        [Test]
        public void StateDone_ProfileAndAvatar_FromAppState()
        {
            var state = new AppState { hasCompletedExtendedProfile = true, userHasAvatar = true };

            CollectionAssert.AreEqual(
                States(OnboardingStepState.Done, OnboardingStepState.Current, OnboardingStepState.Pending, OnboardingStepState.Done),
                _run.GetStates(state));
        }

        [Test]
        public void MarkDone_SurveyWithoutAnswers_IsDone()
        {
            _run.MarkSkipped(OnboardingStep.Profile);
            _run.MarkSkipped(OnboardingStep.Goals);

            _run.MarkDone(OnboardingStep.Survey);

            Assert.AreEqual(OnboardingStepState.Done, _run.GetStates(new AppState())[(int)OnboardingStep.Survey]);
            Assert.AreEqual(OnboardingStep.Avatar, _run.GetCurrent(new AppState()));
        }

        [Test]
        public void DoneWinsOverSkipped()
        {
            _run.MarkSkipped(OnboardingStep.Goals);
            var state = new AppState { userGoals = new[] { "PROTEIN" } };

            Assert.AreEqual(OnboardingStepState.Done, _run.GetStates(state)[(int)OnboardingStep.Goals]);
        }

        [Test]
        public void AllDoneOrSkipped_GetCurrent_IsNull()
        {
            _run.MarkDone(OnboardingStep.Profile);
            _run.MarkSkipped(OnboardingStep.Goals);
            _run.MarkDone(OnboardingStep.Survey);
            _run.MarkSkipped(OnboardingStep.Avatar);

            Assert.IsNull(_run.GetCurrent(new AppState()));
        }

        [Test]
        public void End_ThenStart_ClearsMarks()
        {
            _run.MarkSkipped(OnboardingStep.Profile);

            _run.End();
            _run.Start();

            Assert.IsTrue(_run.IsActive);
            Assert.AreEqual(OnboardingStep.Profile, _run.GetCurrent(new AppState()));
        }

        [Test]
        public void MarkSkipped_DefersHomeOnboardingReminderForTheSession()
        {
            _run.MarkSkipped(OnboardingStep.Goals);

            Assert.IsTrue(HomePromptSession.OnboardingDeferred);
        }

        [Test]
        public void NotActive_MarkSkipped_DoesNotDeferReminder()
        {
            _run.End();

            _run.MarkSkipped(OnboardingStep.Goals);

            Assert.IsFalse(HomePromptSession.OnboardingDeferred);
        }

        [Test]
        public void MarkDone_DoesNotDeferReminder()
        {
            _run.MarkDone(OnboardingStep.Profile);

            Assert.IsFalse(HomePromptSession.OnboardingDeferred);
        }
    }
}
