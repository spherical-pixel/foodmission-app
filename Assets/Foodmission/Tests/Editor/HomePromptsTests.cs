using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class HomePromptsTests
    {
        private TestStoreService _store;
        private Mock<IAudioService> _audio;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", accessToken = "t" });
            _audio = new Mock<IAudioService>();
            HomePromptSession.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            HomePromptSession.Reset();
            _store?.Dispose();
        }

        [Test]
        public void Create_ListsThePromptsInTheAgreedOrder()
        {
            var vm = new HomeScreenViewModel(_store, _audio.Object);

            IReadOnlyList<IHomePrompt> prompts = HomePrompts.Create(vm, new HomePromptRunState(), null);

            var types = new List<System.Type>();
            foreach (IHomePrompt p in prompts)
            {
                types.Add(p.GetType());
            }
            CollectionAssert.AreEqual(new[]
            {
                typeof(LegalConsentPrompt), typeof(PilotConsentPrompt),
                typeof(RewardCelebrationsPrompt), typeof(FailedMissionsPrompt), typeof(WhatsNewPrompt),
                typeof(UnlockedQuestPrompt), typeof(OnboardingReminderPrompt), typeof(NotificationsPrompt),
                typeof(PilotSurveyPrompt), typeof(MissionLastDayPrompt), typeof(MissionNudgePrompt), typeof(DailyFoodFactPrompt)
            }, types);
            vm.Dispose();
        }

        [Test]
        public async Task MissionLastDayPrompt_ShowsWhenAMissionEndsToday()
        {
            var lastDay = new Mock<IMissionLastDayService>();
            var day = new MissionDeadlines.LastDay(System.DateTime.Now, System.DateTime.Now.AddHours(1),
                new[] { new MissionDeadlines.ActiveMission("M.A1.1", "Green zone", System.DateTime.Now.AddDays(-7)) });
            lastDay.Setup(s => s.GetTodayAsync()).ReturnsAsync(day);
            var vm = new HomeScreenViewModel(_store, _audio.Object, missionLastDayService: lastDay.Object);

            Assert.IsNotNull(await new MissionLastDayPrompt(vm).CheckAsync());

            lastDay.Setup(s => s.GetTodayAsync()).ReturnsAsync((MissionDeadlines.LastDay)null);
            Assert.IsNull(await new MissionLastDayPrompt(vm).CheckAsync());
            vm.Dispose();
        }

        [Test]
        public async Task MissionNudgePrompt_StaysQuiet_OnADayNutriAlreadyToldTheLastDay()
        {
            var nudges = new Mock<IMissionNudgeService>();
            nudges.Setup(s => s.GetNudgeAsync()).ReturnsAsync(MissionNudge.ForMissingDays(2));
            var lastDay = new Mock<IMissionLastDayService>();
            lastDay.SetupGet(s => s.ShownToday).Returns(true);
            var vm = new HomeScreenViewModel(_store, _audio.Object, missionNudgeService: nudges.Object, missionLastDayService: lastDay.Object);

            Assert.IsNull(await new MissionNudgePrompt(vm).CheckAsync());
            vm.Dispose();
        }

        [Test]
        public void LegalConsentPrompt_PendingDocuments_ListsEveryUnacceptedDocument()
        {
            var status = new LegalConsentStatus
            {
                mustAccept = true,
                documents = new[]
                {
                    new PendingLegalConsent { docType = LegalDocType.TermsOfService, documentKey = "tos", accepted = false },
                    new PendingLegalConsent { docType = "PRIVACY_POLICY", documentKey = "pp", accepted = false },
                    new PendingLegalConsent { docType = "COOKIES", documentKey = "ck", accepted = true }
                }
            };

            var keys = new List<string>();
            foreach (PendingLegalConsent doc in LegalConsentPrompt.PendingDocuments(status))
            {
                keys.Add(doc.documentKey);
            }

            CollectionAssert.AreEqual(new[] { "tos", "pp" }, keys);
        }

        [Test]
        public async Task UnlockedQuestPrompt_PendingOnlyWhenACelebrationLeftAQuest()
        {
            var vm = new HomeScreenViewModel(_store, _audio.Object);
            var run = new HomePromptRunState();
            var prompt = new UnlockedQuestPrompt(vm, run);

            Assert.IsNull(await prompt.CheckAsync());
            run.QuestToOffer = new Quest { id = "q2" };
            Assert.IsNotNull(await prompt.CheckAsync());
            vm.Dispose();
        }

        [Test]
        public async Task MissionNudgePrompt_CheckDoesNotMarkShown()
        {
            var nudges = new Mock<IMissionNudgeService>();
            nudges.Setup(n => n.GetNudgeAsync()).ReturnsAsync(MissionNudge.ForMissingDays(2));
            var vm = new HomeScreenViewModel(_store, _audio.Object, missionNudgeService: nudges.Object);

            Assert.IsNotNull(await new MissionNudgePrompt(vm).CheckAsync());

            nudges.Verify(n => n.MarkShown(It.IsAny<MissionNudge>()), Times.Never);
            vm.Dispose();
        }

        [Test]
        public async Task DailyFoodFactPrompt_CheckDoesNotMarkShown()
        {
            var facts = new Mock<IDailyFoodFactService>();
            facts.Setup(f => f.GetFactToShowAsync()).ReturnsAsync("FF1");
            var vm = new HomeScreenViewModel(_store, _audio.Object, dailyFoodFactService: facts.Object);

            Assert.IsNotNull(await new DailyFoodFactPrompt(vm).CheckAsync());

            facts.Verify(f => f.MarkShown(It.IsAny<string>()), Times.Never);
            vm.Dispose();
        }

        [Test]
        public async Task OnboardingReminderPrompt_DeferredThisSession_IsNotPending()
        {
            _store.SetAppState(new AppState { userId = "u1", accessToken = "t", hasCompletedExtendedProfile = false });
            var vm = new HomeScreenViewModel(_store, _audio.Object);
            HomePromptSession.OnboardingDeferred = true;

            Assert.IsNull(await new OnboardingReminderPrompt(vm).CheckAsync());
            vm.Dispose();
        }
    }
}
