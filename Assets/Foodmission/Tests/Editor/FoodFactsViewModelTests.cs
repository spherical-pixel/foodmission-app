using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodFactsViewModelTests
    {
        private Mock<IFoodFactService> _mockFoodFactService;
        private Mock<IDimensionService> _mockDimensionService;
        private TestStoreService _storeService;
        private FoodFactsViewModel _vm;
        private Func<bool> _originalOverride;

        private Dimension[] _mockDimensions;
        private FoodFact[] _mockFacts;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockFoodFactService = new Mock<IFoodFactService>();
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
                        new Topic { id = "top-1", code = "REDUCING_MEAT_CONSUMPTION", name = "Reducción de carne", dimensionId = "dim-1", sortOrder = 1 },
                        new Topic { id = "top-2", code = "ALTERNATIVE_STAPLE_FOODS", name = "Alimentos básicos alternativos", dimensionId = "dim-1", sortOrder = 2 }
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
                        new Topic { id = "top-3", code = "PLATE_WASTE", name = "Restos del plato", dimensionId = "dim-2", sortOrder = 1 }
                    }
                }
            };

            _mockFacts = new[]
            {
                new FoodFact
                {
                    id = "f-1",
                    code = "FF1.1.2",
                    topicId = "top-1",
                    body = "Segundo dato sobre carne",
                    level = FoodFactLevel.Beginner
                },
                new FoodFact
                {
                    id = "f-2",
                    code = "FF1.1.1",
                    topicId = "top-1",
                    body = "Primer dato sobre carne",
                    level = FoodFactLevel.Intermediate
                },
                new FoodFact
                {
                    id = "f-3",
                    code = "FF1.2.1",
                    topicId = "top-2",
                    body = "Dato staples",
                    level = FoodFactLevel.Advanced
                },
                new FoodFact
                {
                    id = "f-4",
                    code = "FF5.1.1",
                    topicId = "top-3",
                    body = "Dato desperdicio",
                    level = FoodFactLevel.Beginner
                }
            };

            _mockDimensionService.Setup(d => d.IsLoaded).Returns(true);
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns(_mockDimensions);
            _mockDimensionService.Setup(d => d.GetTopicsForDimension("DIET_CHANGES")).Returns(_mockDimensions[0].topics);
            _mockDimensionService.Setup(d => d.GetTopicsForDimension("FOOD_WASTE")).Returns(_mockDimensions[1].topics);

            _vm = new FoodFactsViewModel(
                _storeService,
                _mockFoodFactService.Object,
                _mockDimensionService.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void InitialState_ShouldHaveDefaultValues()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual(FoodFactFilterLevel.All, _vm.SelectedLevel);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.IsNull(_vm.ErrorDetail);
            Assert.IsEmpty(_vm.DisplayGroups);
            Assert.AreEqual(0, _vm.TotalFactsCount);
        }

        [Test]
        public async Task LoadDataAsync_WhenSuccessful_BuildsHierarchicalDisplayGroups()
        {
            _mockFoodFactService.Setup(s => s.GetFoodFactsAsync(null, 1, 200, null))
                .ReturnsAsync((new PaginatedFoodFactResponse
                {
                    data = _mockFacts,
                    meta = new PaginationMeta { total = 4 }
                }, null));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.AreEqual(4, _vm.TotalFactsCount);
            Assert.AreEqual(2, _vm.DisplayGroups.Count);

            // Dimension 1
            var dim1Group = _vm.DisplayGroups.FirstOrDefault(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsNotNull(dim1Group);
            Assert.AreEqual(3, dim1Group.TotalCount);
            Assert.AreEqual(2, dim1Group.Topics.Count);

            // Topic 1 sorting check (FF1.1.1 should precede FF1.1.2)
            var top1 = dim1Group.Topics.FirstOrDefault(t => t.Topic.code == "REDUCING_MEAT_CONSUMPTION");
            Assert.IsNotNull(top1);
            Assert.AreEqual(2, top1.Facts.Count);
            Assert.AreEqual("FF1.1.1", top1.Facts[0].FoodFact.code);
            Assert.AreEqual("FF1.1.2", top1.Facts[1].FoodFact.code);
        }

        [Test]
        public async Task LoadDataAsync_WhenServiceReturnsError_SetsErrorDetail()
        {
            _mockFoodFactService.Setup(s => s.GetFoodFactsAsync(null, 1, 200, null))
                .ReturnsAsync((null, new ApiErrorResponse { message = "Unauthorized access", statusCode = 401 }));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Unauthorized access", _vm.ErrorMessage);
            Assert.IsEmpty(_vm.DisplayGroups);
        }

        [Test]
        public void SetLevelFilter_ShouldFilterDisplayGroupsCorrectly()
        {
            _vm.SetRawDataForTesting(_mockFacts);

            // Filter Beginner
            _vm.SetLevelFilter(FoodFactLevel.Beginner);
            Assert.AreEqual(FoodFactLevel.Beginner, _vm.SelectedLevel);
            Assert.AreEqual(2, _vm.TotalFactsCount);

            // Filter Intermediate
            _vm.SetLevelFilter(FoodFactLevel.Intermediate);
            Assert.AreEqual(FoodFactLevel.Intermediate, _vm.SelectedLevel);
            Assert.AreEqual(1, _vm.TotalFactsCount);

            // Filter All
            _vm.SetLevelFilter(FoodFactFilterLevel.All);
            Assert.AreEqual(4, _vm.TotalFactsCount);
        }

        [Test]
        public void ToggleDimensionExpanded_ShouldToggleState()
        {
            _vm.SetRawDataForTesting(_mockFacts);

            var dim1Group = _vm.DisplayGroups.FirstOrDefault(g => g.Dimension.code == "DIET_CHANGES");
            Assert.IsNotNull(dim1Group);
            Assert.IsFalse(dim1Group.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsTrue(dim1Group.IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsFalse(dim1Group.IsExpanded);
        }

        [Test]
        public void OpenFoodFact_ShouldRaiseNavigationRequested_WithCorrectArguments()
        {
            string requestedAction = null;
            Argument[] requestedArgs = null;

            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            var fact = _mockFacts[0];
            _vm.OpenFoodFact(fact);

            Assert.AreEqual(Actions.open_food_fact, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual("code", requestedArgs[0].name);
            Assert.AreEqual(fact.code, requestedArgs[0].value);
            Assert.AreEqual("id", requestedArgs[1].name);
            Assert.AreEqual(fact.id, requestedArgs[1].value);
        }

        [Test]
        public void OpenRandomFact_ShouldSelectFactFromPool()
        {
            string requestedAction = null;
            Argument[] requestedArgs = null;

            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            _vm.SetRawDataForTesting(_mockFacts);
            _vm.SetLevelFilter(FoodFactLevel.Intermediate); // only f-2 (FF1.1.1)

            _vm.OpenRandomFact();

            Assert.AreEqual(Actions.open_food_fact, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual("FF1.1.1", requestedArgs[0].value);
        }
    }
}
