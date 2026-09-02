using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengesViewModelTests
    {
        private Mock<IChallengeService> _mockChallengeService;
        private Mock<IDimensionService> _mockDimensionService;
        private TestStoreService _storeService;
        private ChallengesViewModel _vm;
        private Func<bool> _originalOverride;

        private Dimension[] _mockDimensions;
        private Challenge[] _mockChallenges;
        private ChallengeProgress[] _mockProgress;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockChallengeService = new Mock<IChallengeService>();
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
                    sortOrder = 1,
                    topics = new[]
                    {
                        new Topic { id = "top-1", code = "REDUCING_MEAT", name = "Reducción de carne", dimensionId = "dim-1", sortOrder = 1 },
                        new Topic { id = "top-2", code = "ALTERNATIVE_PROTEIN", name = "Proteínas alternativas", dimensionId = "dim-1", sortOrder = 2 }
                    }
                },
                new Dimension
                {
                    id = "dim-2",
                    code = "FOOD_WASTE",
                    name = "Desperdicio de comida",
                    sortOrder = 2,
                    topics = new[]
                    {
                        new Topic { id = "top-3", code = "PANTRY_MANAGEMENT", name = "Gestión de despensa", dimensionId = "dim-2", sortOrder = 1 }
                    }
                }
            };

            _mockChallenges = new[]
            {
                new Challenge
                {
                    id = "ch-1",
                    code = "CH.B1.1",
                    dimensionId = "dim-1",
                    topicId = "top-1",
                    level = ChallengeLevel.Beginner,
                    title = "Meatless Monday",
                    task = "Eat vegetarian on Monday",
                    whyItMatters = "Reduces carbon footprint",
                    available = true
                },
                new Challenge
                {
                    id = "ch-2",
                    code = "CH.I1.1",
                    dimensionId = "dim-1",
                    topicId = "top-1",
                    level = ChallengeLevel.Intermediate,
                    title = "Try Legumes",
                    task = "Cook a meal with lentils",
                    whyItMatters = "Plant proteins have lower impact",
                    available = true
                },
                new Challenge
                {
                    id = "ch-3",
                    code = "CH.A1.1",
                    dimensionId = "dim-1",
                    topicId = "top-2",
                    level = ChallengeLevel.Advanced,
                    title = "Alternative Grains",
                    task = "Buy and cook with quinoa or buckwheat",
                    whyItMatters = "Diversifies crops",
                    available = true
                },
                new Challenge
                {
                    id = "ch-4",
                    code = "CH.B2.1",
                    dimensionId = "dim-2",
                    topicId = "top-3",
                    level = ChallengeLevel.Beginner,
                    title = "Check Expirations",
                    task = "Review expiration dates in pantry",
                    whyItMatters = "Prevents waste",
                    available = true
                }
            };

            _mockProgress = new[]
            {
                new ChallengeProgress
                {
                    challengeId = "ch-1",
                    userId = "user-1",
                    completed = true,
                    progress = 100f,
                    challengeTitle = "Meatless Monday"
                }
            };

            _mockDimensionService.Setup(d => d.IsLoaded).Returns(true);
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns(_mockDimensions);

            _vm = new ChallengesViewModel(_storeService, _mockChallengeService.Object, _mockDimensionService.Object);
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
            Assert.AreEqual(ChallengeFilterLevel.All, _vm.SelectedLevel);
            Assert.AreEqual(ChallengeFilterStatus.All, _vm.SelectedStatus);
            Assert.IsEmpty(_vm.DisplayGroups);
            Assert.AreEqual(0, _vm.TotalChallengesCount);
            Assert.AreEqual(0, _vm.CompletedChallengesCount);
        }

        [Test]
        public async Task LoadDataAsync_PopulatesDisplayGroupsAndCounts()
        {
            _mockChallengeService.Setup(s => s.GetChallengesAsync(It.IsAny<ChallengeFilterParams>(), It.IsAny<string>()))
                .ReturnsAsync((_mockChallenges, null));
            _mockChallengeService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((_mockProgress, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.AreEqual(4, _vm.TotalChallengesCount);
            Assert.AreEqual(1, _vm.CompletedChallengesCount);
            Assert.AreEqual(2, _vm.DisplayGroups.Count);

            var dim1 = _vm.DisplayGroups.FirstOrDefault(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsNotNull(dim1);
            Assert.AreEqual(3, dim1.TotalCount);
            Assert.AreEqual(1, dim1.CompletedCount);
            Assert.AreEqual(3, dim1.Challenges.Count);
            Assert.IsTrue(dim1.Challenges[0].IsCompleted); // ch-1
            Assert.IsFalse(dim1.Challenges[1].IsCompleted); // ch-2
        }

        [Test]
        public async Task LoadDataAsync_WhenServiceFails_SetsErrorMessage()
        {
            _mockChallengeService.Setup(s => s.GetChallengesAsync(It.IsAny<ChallengeFilterParams>(), It.IsAny<string>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "Network Error" }));
            _mockChallengeService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((null, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual("Network Error", _vm.ErrorMessage);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public void SetLevelFilter_FiltersCorrectly()
        {
            _vm.SetRawDataForTesting(_mockChallenges, _mockProgress);

            Assert.AreEqual(4, _vm.TotalChallengesCount);

            _vm.SetLevelFilter(ChallengeLevel.Beginner);
            Assert.AreEqual(ChallengeLevel.Beginner, _vm.SelectedLevel);
            Assert.AreEqual(2, _vm.TotalChallengesCount);
            Assert.AreEqual(1, _vm.CompletedChallengesCount);

            _vm.SetLevelFilter(ChallengeLevel.Advanced);
            Assert.AreEqual(1, _vm.TotalChallengesCount);
            Assert.AreEqual(0, _vm.CompletedChallengesCount);

            _vm.SetLevelFilter(ChallengeFilterLevel.All);
            Assert.AreEqual(4, _vm.TotalChallengesCount);
        }

        [Test]
        public void SetStatusFilter_FiltersCorrectly()
        {
            _vm.SetRawDataForTesting(_mockChallenges, _mockProgress);

            _vm.SetStatusFilter(ChallengeFilterStatus.Completed);
            Assert.AreEqual(1, _vm.DisplayGroups.Sum(g => g.Challenges.Count));

            _vm.SetStatusFilter(ChallengeFilterStatus.Pending);
            Assert.AreEqual(3, _vm.DisplayGroups.Sum(g => g.Challenges.Count));

            _vm.SetStatusFilter(ChallengeFilterStatus.All);
            Assert.AreEqual(4, _vm.DisplayGroups.Sum(g => g.Challenges.Count));
        }

        [Test]
        public void ToggleDimensionExpanded_TogglesState()
        {
            _vm.SetRawDataForTesting(_mockChallenges, _mockProgress);

            var dim1 = _vm.DisplayGroups.First(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsFalse(dim1.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsTrue(dim1.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsFalse(dim1.IsExpanded);
        }

        [Test]
        public void OpenChallenge_InvokesOnChallengeSelectedEvent()
        {
            Challenge selected = null;
            _vm.OnChallengeSelected += ch => selected = ch;

            var testChallenge = _mockChallenges[0];
            _vm.OpenChallenge(testChallenge);

            Assert.AreEqual(testChallenge, selected);
        }

        [Test]
        public void RebuildDisplayGroups_WithNullTopicId_GroupsUnderDimensionDirectly()
        {
            var directChallenges = new[]
            {
                new Challenge
                {
                    id = "ch-10",
                    code = "CH.I3.1",
                    dimensionId = "dim-1",
                    topicId = null,
                    level = ChallengeLevel.Intermediate,
                    title = "Challenge without topic",
                    available = true
                }
            };

            _vm.SetRawDataForTesting(directChallenges, null);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            var group = _vm.DisplayGroups[0];
            Assert.AreEqual("DIET_CHANGES", group.Dimension.code);
            Assert.AreEqual(1, group.TotalCount);
            Assert.AreEqual(1, group.Challenges.Count);
            Assert.AreEqual("CH.I3.1", group.Challenges[0].Challenge.code);
        }

        [Test]
        public void RebuildDisplayGroups_SortsByLevelThenCode()
        {
            var unsortedChallenges = new[]
            {
                new Challenge { id = "1", code = "CH.A1.2", dimensionId = "dim-1", level = ChallengeLevel.Advanced, available = true },
                new Challenge { id = "2", code = "CH.B1.1", dimensionId = "dim-1", level = ChallengeLevel.Beginner, available = true },
                new Challenge { id = "3", code = "CH.I1.1", dimensionId = "dim-1", level = ChallengeLevel.Intermediate, available = true },
                new Challenge { id = "4", code = "CH.B1.2", dimensionId = "dim-1", level = ChallengeLevel.Beginner, available = true },
                new Challenge { id = "5", code = "CH.A1.1", dimensionId = "dim-1", level = ChallengeLevel.Advanced, available = true },
            };

            _vm.SetRawDataForTesting(unsortedChallenges, null);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            var challenges = _vm.DisplayGroups[0].Challenges.Select(c => c.Challenge.code).ToList();

            // Expected order: Beginner (CH.B1.1, CH.B1.2) -> Intermediate (CH.I1.1) -> Advanced (CH.A1.1, CH.A1.2)
            Assert.AreEqual(new[] { "CH.B1.1", "CH.B1.2", "CH.I1.1", "CH.A1.1", "CH.A1.2" }, challenges);
        }

        [Test]
        public void FallbackGrouping_WhenDimensionsNotLoaded_GroupsDirectly()
        {
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns((IReadOnlyList<Dimension>)null);

            _vm.SetRawDataForTesting(_mockChallenges, _mockProgress);

            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            Assert.AreEqual("ALL_CHALLENGES", _vm.DisplayGroups[0].Dimension.code);
            Assert.AreEqual(4, _vm.DisplayGroups[0].TotalCount);
        }
    }
}
