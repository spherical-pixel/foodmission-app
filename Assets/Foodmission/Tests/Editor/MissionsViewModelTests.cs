using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionsViewModelTests
    {
        private Mock<IMissionService> _mockMissionService;
        private Mock<IDimensionService> _mockDimensionService;
        private TestStoreService _storeService;
        private MissionsViewModel _vm;
        private Func<bool> _originalOverride;

        private Dimension[] _mockDimensions;
        private Mission[] _mockMissions;
        private MissionProgress[] _mockProgress;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockMissionService = new Mock<IMissionService>();
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

            _mockMissions = new[]
            {
                new Mission
                {
                    id = "m-1",
                    code = "M.B1.1",
                    dimensionId = "dim-1",
                    topicId = "top-1",
                    level = MissionLevel.Beginner,
                    title = "Meatless Monday",
                    duration = "1 day",
                    goal = "Eat vegetarian on Monday",
                    whyItMatters = "Reduces carbon footprint",
                    available = true
                },
                new Mission
                {
                    id = "m-2",
                    code = "M.I1.1",
                    dimensionId = "dim-1",
                    topicId = "top-1",
                    level = MissionLevel.Intermediate,
                    title = "Try Legumes",
                    duration = "3 days",
                    goal = "Cook with lentils",
                    whyItMatters = "Plant proteins have lower impact",
                    available = true
                },
                new Mission
                {
                    id = "m-3",
                    code = "M.A1.1",
                    dimensionId = "dim-1",
                    topicId = "top-2",
                    level = MissionLevel.Advanced,
                    title = "Zero Plastic Week",
                    duration = "1 week",
                    goal = "Avoid single use plastic",
                    whyItMatters = "Protects oceans",
                    available = true
                },
                new Mission
                {
                    id = "m-4",
                    code = "M.B2.1",
                    dimensionId = "dim-2",
                    topicId = "top-3",
                    level = MissionLevel.Beginner,
                    title = "Pantry Check",
                    duration = "1 day",
                    goal = "Review expirations",
                    whyItMatters = "Prevents waste",
                    available = true
                }
            };

            _mockProgress = new[]
            {
                new MissionProgress
                {
                    missionId = "m-1",
                    userId = "user-1",
                    completed = true,
                    progress = 100f,
                    missionTitle = "Meatless Monday"
                }
            };

            _mockDimensionService.Setup(d => d.IsLoaded).Returns(true);
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns(_mockDimensions);

            _vm = new MissionsViewModel(_storeService, _mockMissionService.Object, _mockDimensionService.Object);
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
            Assert.AreEqual(MissionFilterLevel.All, _vm.SelectedLevel);
            Assert.AreEqual(MissionFilterStatus.All, _vm.SelectedStatus);
            Assert.IsEmpty(_vm.DisplayGroups);
            Assert.AreEqual(0, _vm.TotalMissionsCount);
            Assert.AreEqual(0, _vm.CompletedMissionsCount);
        }

        [Test]
        public async Task LoadDataAsync_PopulatesDisplayGroupsAndCounts()
        {
            _mockMissionService.Setup(s => s.GetMissionsAsync(It.IsAny<MissionFilterParams>(), It.IsAny<string>()))
                .ReturnsAsync((_mockMissions, null));
            _mockMissionService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((_mockProgress, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.AreEqual(4, _vm.TotalMissionsCount);
            Assert.AreEqual(1, _vm.CompletedMissionsCount);
            Assert.AreEqual(2, _vm.DisplayGroups.Count);

            var dim1 = _vm.DisplayGroups.FirstOrDefault(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsNotNull(dim1);
            Assert.AreEqual(3, dim1.TotalCount);
            Assert.AreEqual(1, dim1.CompletedCount);
            Assert.AreEqual(3, dim1.Missions.Count);
            Assert.IsTrue(dim1.Missions[0].IsCompleted); // m-1
            Assert.IsFalse(dim1.Missions[1].IsCompleted); // m-2
        }

        [Test]
        public async Task LoadDataAsync_WhenServiceFails_SetsErrorMessage()
        {
            _mockMissionService.Setup(s => s.GetMissionsAsync(It.IsAny<MissionFilterParams>(), It.IsAny<string>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "Network Error" }));
            _mockMissionService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((null, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual("Network Error", _vm.ErrorMessage);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public void SetLevelFilter_FiltersCorrectly()
        {
            _vm.SetRawDataForTesting(_mockMissions, _mockProgress);

            Assert.AreEqual(4, _vm.TotalMissionsCount);

            _vm.SetLevelFilter(MissionLevel.Beginner);
            Assert.AreEqual(MissionLevel.Beginner, _vm.SelectedLevel);
            Assert.AreEqual(2, _vm.TotalMissionsCount);
            Assert.AreEqual(1, _vm.CompletedMissionsCount);

            _vm.SetLevelFilter(MissionLevel.Advanced);
            Assert.AreEqual(1, _vm.TotalMissionsCount);
            Assert.AreEqual(0, _vm.CompletedMissionsCount);

            _vm.SetLevelFilter(MissionFilterLevel.All);
            Assert.AreEqual(4, _vm.TotalMissionsCount);
        }

        [Test]
        public void SetStatusFilter_FiltersCorrectly()
        {
            _vm.SetRawDataForTesting(_mockMissions, _mockProgress);

            _vm.SetStatusFilter(MissionFilterStatus.Completed);
            Assert.AreEqual(1, _vm.DisplayGroups.Sum(g => g.Missions.Count));

            _vm.SetStatusFilter(MissionFilterStatus.Pending);
            Assert.AreEqual(3, _vm.DisplayGroups.Sum(g => g.Missions.Count));

            _vm.SetStatusFilter(MissionFilterStatus.All);
            Assert.AreEqual(4, _vm.DisplayGroups.Sum(g => g.Missions.Count));
        }

        [Test]
        public void ToggleDimensionExpanded_TogglesState()
        {
            _vm.SetRawDataForTesting(_mockMissions, _mockProgress);

            var dim1 = _vm.DisplayGroups.First(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsFalse(dim1.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsTrue(dim1.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsFalse(dim1.IsExpanded);
        }

        [Test]
        public void OpenMission_InvokesOnMissionSelectedEvent()
        {
            Mission selected = null;
            _vm.OnMissionSelected += m => selected = m;

            var testMission = _mockMissions[0];
            _vm.OpenMission(testMission);

            Assert.AreEqual(testMission, selected);
        }

        [Test]
        public void RebuildDisplayGroups_WithNullTopicId_GroupsUnderDimensionDirectly()
        {
            var directMissions = new[]
            {
                new Mission
                {
                    id = "m-10",
                    code = "M.I3.1",
                    dimensionId = "dim-1",
                    topicId = null,
                    level = MissionLevel.Intermediate,
                    title = "Mission without topic",
                    available = true
                }
            };

            _vm.SetRawDataForTesting(directMissions, null);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            var group = _vm.DisplayGroups[0];
            Assert.AreEqual("DIET_CHANGES", group.Dimension.code);
            Assert.AreEqual(1, group.TotalCount);
            Assert.AreEqual(1, group.Missions.Count);
            Assert.AreEqual("M.I3.1", group.Missions[0].Mission.code);
        }

        [Test]
        public void RebuildDisplayGroups_SortsByLevelThenCode()
        {
            var unsortedMissions = new[]
            {
                new Mission { id = "1", code = "M.A1.2", dimensionId = "dim-1", level = MissionLevel.Advanced, available = true },
                new Mission { id = "2", code = "M.B1.1", dimensionId = "dim-1", level = MissionLevel.Beginner, available = true },
                new Mission { id = "3", code = "M.I1.1", dimensionId = "dim-1", level = MissionLevel.Intermediate, available = true },
                new Mission { id = "4", code = "M.B1.2", dimensionId = "dim-1", level = MissionLevel.Beginner, available = true },
                new Mission { id = "5", code = "M.A1.1", dimensionId = "dim-1", level = MissionLevel.Advanced, available = true },
            };

            _vm.SetRawDataForTesting(unsortedMissions, null);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            var missions = _vm.DisplayGroups[0].Missions.Select(m => m.Mission.code).ToList();

            // Expected order: Beginner (M.B1.1, M.B1.2) -> Intermediate (M.I1.1) -> Advanced (M.A1.1, M.A1.2)
            Assert.AreEqual(new[] { "M.B1.1", "M.B1.2", "M.I1.1", "M.A1.1", "M.A1.2" }, missions);
        }

        [Test]
        public void FallbackGrouping_WhenDimensionsNotLoaded_GroupsDirectly()
        {
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns((IReadOnlyList<Dimension>)null);

            _vm.SetRawDataForTesting(_mockMissions, _mockProgress);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            Assert.AreEqual("ALL_MISSIONS", _vm.DisplayGroups[0].Dimension.code);
            Assert.AreEqual(4, _vm.DisplayGroups[0].TotalCount);
        }
    }
}
