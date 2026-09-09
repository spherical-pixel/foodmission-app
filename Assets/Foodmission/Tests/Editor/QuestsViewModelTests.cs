using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuestsViewModelTests
    {
        private Mock<IQuestService> _mockQuestService;
        private Mock<IDimensionService> _mockDimensionService;
        private TestStoreService _storeService;
        private QuestsViewModel _vm;
        private Func<bool> _originalOverride;

        private Dimension[] _mockDimensions;
        private Quest[] _mockQuests;
        private QuestProgress[] _mockProgress;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockQuestService = new Mock<IQuestService>();
            _mockDimensionService = new Mock<IDimensionService>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _mockDimensions = new[]
            {
                new Dimension
                {
                    id = "dim-1",
                    code = "DIET_CHANGES",
                    name = "Cambios en la dieta",
                    sortOrder = 1
                },
                new Dimension
                {
                    id = "dim-2",
                    code = "FOOD_WASTE",
                    name = "Desperdicio de comida",
                    sortOrder = 2
                }
            };

            _mockQuests = new[]
            {
                new Quest
                {
                    id = "q-1",
                    code = "QUEST.DIET.BEGINNER.1",
                    dimensionId = "dim-1",
                    level = QuestLevel.Beginner,
                    title = "Learn to Log Your Food",
                    available = true,
                    items = new[]
                    {
                        new QuestItem { id = "item-1", contentType = QuestContentType.Mission, contentCode = "M.1", label = "Log lunch" },
                        new QuestItem { id = "item-2", contentType = QuestContentType.Quiz, contentCode = "Q.1", label = "Diet quiz" }
                    }
                },
                new Quest
                {
                    id = "q-2",
                    code = "QUEST.DIET.INTERMEDIATE.1",
                    dimensionId = "dim-1",
                    level = QuestLevel.Intermediate,
                    title = "Plant-Based Week",
                    available = true,
                    items = new[]
                    {
                        new QuestItem { id = "item-3", contentType = QuestContentType.Mission, contentCode = "M.2", label = "Plant meal" }
                    }
                },
                new Quest
                {
                    id = "q-3",
                    code = "QUEST.DIET.ADVANCED.1",
                    dimensionId = "dim-1",
                    level = QuestLevel.Advanced,
                    title = "Zero UPF Mastery",
                    available = true
                },
                new Quest
                {
                    id = "q-4",
                    code = "QUEST.WASTE.BEGINNER.1",
                    dimensionId = "dim-2",
                    level = QuestLevel.Beginner,
                    title = "Pantry Inventory",
                    available = true
                }
            };

            _mockProgress = new[]
            {
                new QuestProgress
                {
                    questId = "q-1",
                    questCode = "QUEST.DIET.BEGINNER.1",
                    userId = "user-1",
                    completed = true,
                    progress = 100f,
                    questTitle = "Learn to Log Your Food"
                }
            };

            _mockDimensionService.Setup(d => d.IsLoaded).Returns(true);
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns(_mockDimensions);

            _vm = new QuestsViewModel(_storeService, _mockQuestService.Object, _mockDimensionService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaultState()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual(QuestFilterLevel.All, _vm.SelectedLevel);
            Assert.AreEqual(QuestFilterStatus.All, _vm.SelectedStatus);
            Assert.IsEmpty(_vm.DisplayGroups);
            Assert.AreEqual(0, _vm.TotalQuestsCount);
            Assert.AreEqual(0, _vm.CompletedQuestsCount);
        }

        [Test]
        public async Task LoadDataAsync_PopulatesDisplayGroupsAndCounts()
        {
            _mockQuestService.Setup(s => s.GetQuestsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((_mockQuests, null));
            _mockQuestService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((_mockProgress, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.AreEqual(4, _vm.TotalQuestsCount);
            Assert.AreEqual(1, _vm.CompletedQuestsCount);
            Assert.AreEqual(2, _vm.DisplayGroups.Count);

            var dim1 = _vm.DisplayGroups.FirstOrDefault(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsNotNull(dim1);
            Assert.AreEqual(3, dim1.TotalCount);
            Assert.AreEqual(1, dim1.CompletedCount);
            Assert.AreEqual(3, dim1.Quests.Count);
            Assert.IsTrue(dim1.Quests[0].IsCompleted); // q-1
            Assert.IsFalse(dim1.Quests[1].IsCompleted); // q-2
        }

        [Test]
        public async Task LoadDataAsync_WhenServiceFails_SetsErrorMessage()
        {
            _mockQuestService.Setup(s => s.GetQuestsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "Network Error" }));
            _mockQuestService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((null, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual("Network Error", _vm.ErrorMessage);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public void SetLevelFilter_FiltersCorrectly()
        {
            _vm.SetRawDataForTesting(_mockQuests, _mockProgress);

            Assert.AreEqual(4, _vm.TotalQuestsCount);

            _vm.SetLevelFilter(QuestLevel.Beginner);
            Assert.AreEqual(QuestLevel.Beginner, _vm.SelectedLevel);
            Assert.AreEqual(2, _vm.TotalQuestsCount);
            Assert.AreEqual(1, _vm.CompletedQuestsCount);

            _vm.SetLevelFilter(QuestLevel.Advanced);
            Assert.AreEqual(1, _vm.TotalQuestsCount);
            Assert.AreEqual(0, _vm.CompletedQuestsCount);

            _vm.SetLevelFilter(QuestFilterLevel.All);
            Assert.AreEqual(4, _vm.TotalQuestsCount);
        }

        [Test]
        public void SetStatusFilter_FiltersCorrectly()
        {
            _vm.SetRawDataForTesting(_mockQuests, _mockProgress);

            _vm.SetStatusFilter(QuestFilterStatus.Completed);
            Assert.AreEqual(1, _vm.DisplayGroups.Sum(g => g.Quests.Count));

            _vm.SetStatusFilter(QuestFilterStatus.Pending);
            Assert.AreEqual(3, _vm.DisplayGroups.Sum(g => g.Quests.Count));

            _vm.SetStatusFilter(QuestFilterStatus.All);
            Assert.AreEqual(4, _vm.DisplayGroups.Sum(g => g.Quests.Count));
        }

        [Test]
        public void ToggleDimensionExpanded_TogglesState()
        {
            _vm.SetRawDataForTesting(_mockQuests, _mockProgress);

            var dim1 = _vm.DisplayGroups.First(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsFalse(dim1.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsTrue(dim1.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsFalse(dim1.IsExpanded);
        }

        [Test]
        public void OpenQuest_InvokesOnQuestSelectedEvent()
        {
            Quest selected = null;
            string requestedAction = null;
            Argument[] requestedArgs = null;

            _vm.OnQuestSelected += q => selected = q;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            var testQuest = _mockQuests[0];
            _vm.OpenQuest(testQuest);

            Assert.AreEqual(testQuest, selected);
            Assert.AreEqual(Actions.open_quest, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual(testQuest.code, requestedArgs.FirstOrDefault(a => a.name == "code")?.value);
        }

        [Test]
        public void RebuildDisplayGroups_SortsByLevelThenCode()
        {
            var unsortedQuests = new[]
            {
                new Quest { id = "1", code = "QUEST.DIET.ADVANCED.2", dimensionId = "dim-1", level = QuestLevel.Advanced, available = true },
                new Quest { id = "2", code = "QUEST.DIET.BEGINNER.1", dimensionId = "dim-1", level = QuestLevel.Beginner, available = true },
                new Quest { id = "3", code = "QUEST.DIET.INTERMEDIATE.1", dimensionId = "dim-1", level = QuestLevel.Intermediate, available = true },
                new Quest { id = "4", code = "QUEST.DIET.BEGINNER.2", dimensionId = "dim-1", level = QuestLevel.Beginner, available = true },
                new Quest { id = "5", code = "QUEST.DIET.ADVANCED.1", dimensionId = "dim-1", level = QuestLevel.Advanced, available = true },
            };

            _vm.SetRawDataForTesting(unsortedQuests, null);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            var quests = _vm.DisplayGroups[0].Quests.Select(q => q.Quest.code).ToList();

            // Expected order: Beginner (BEGINNER.1, BEGINNER.2) -> Intermediate (INTERMEDIATE.1) -> Advanced (ADVANCED.1, ADVANCED.2)
            Assert.AreEqual(new[] { "QUEST.DIET.BEGINNER.1", "QUEST.DIET.BEGINNER.2", "QUEST.DIET.INTERMEDIATE.1", "QUEST.DIET.ADVANCED.1", "QUEST.DIET.ADVANCED.2" }, quests);
        }

        [Test]
        public void FallbackGrouping_WhenDimensionsNotLoaded_GroupsDirectly()
        {
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns((IReadOnlyList<Dimension>)null);

            _vm.SetRawDataForTesting(_mockQuests, _mockProgress);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            Assert.AreEqual("ALL_QUESTS", _vm.DisplayGroups[0].Dimension.code);
            Assert.AreEqual(4, _vm.DisplayGroups[0].TotalCount);
        }
    }
}
