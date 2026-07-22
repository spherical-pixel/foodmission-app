using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class StepFlowViewModelBaseTests
    {
        private TestStoreService _storeService;
        private TestStepFlowViewModel _vm;

        public class TestStepFlowViewModel : StepFlowViewModelBase
        {
            public bool ShouldValidate = true;
            public int CompletedCallCount = 0;
            public int CancelledCallCount = 0;
            public readonly List<int> EnteredSteps = new();
            public readonly List<int> ExitedSteps = new();

            public TestStepFlowViewModel(IStoreService storeService) : base(storeService)
            {
            }

            protected override int GetStepCount() => 3;

            protected override bool ValidateStep(int stepIndex)
            {
                return ShouldValidate;
            }

            protected override string GetStepTitle(int stepIndex)
            {
                return $"Step {stepIndex + 1}";
            }

            protected override Task OnStepEnteredAsync(int stepIndex)
            {
                EnteredSteps.Add(stepIndex);
                return Task.CompletedTask;
            }

            protected override Task OnStepExitingAsync(int stepIndex)
            {
                ExitedSteps.Add(stepIndex);
                return Task.CompletedTask;
            }

            protected override Task OnFlowCompletedAsync()
            {
                CompletedCallCount++;
                return Task.CompletedTask;
            }

            protected override Task OnFlowCancelledAsync()
            {
                CancelledCallCount++;
                return Task.CompletedTask;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _vm = new TestStepFlowViewModel(_storeService);
            _vm.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Initialize_SetsStepCountAndInitialState()
        {
            Assert.AreEqual(3, _vm.StepCount);
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsFirstStep);
            Assert.IsFalse(_vm.IsLastStep);
            Assert.IsFalse(_vm.CanGoPrevious);
            Assert.IsTrue(_vm.CanGoNext);
            Assert.AreEqual("Step 1", _vm.StepTitle);
            CollectionAssert.AreEqual(new[] { 0 }, _vm.EnteredSteps);
        }

        [Test]
        public async Task GoNext_AdvancesStepWhenValidated()
        {
            await _vm.GoNextAsync();

            Assert.AreEqual(1, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.IsFirstStep);
            Assert.IsFalse(_vm.IsLastStep);
            Assert.IsTrue(_vm.CanGoPrevious);
            Assert.IsTrue(_vm.CanGoNext);
            Assert.AreEqual("Step 2", _vm.StepTitle);
            
            CollectionAssert.AreEqual(new[] { 0 }, _vm.ExitedSteps);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _vm.EnteredSteps);
        }

        [Test]
        public async Task GoNext_BlocksWhenValidationFails()
        {
            _vm.ShouldValidate = false;
            _vm.InvalidateValidation();

            Assert.IsFalse(_vm.CanGoNext);

            await _vm.GoNextAsync();

            Assert.AreEqual(0, _vm.CurrentStepIndex);
            CollectionAssert.IsEmpty(_vm.ExitedSteps);
        }

        [Test]
        public async Task GoPrevious_NavigatesBack()
        {
            await _vm.GoNextAsync();
            Assert.AreEqual(1, _vm.CurrentStepIndex);

            await _vm.GoPreviousAsync();

            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsFirstStep);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _vm.ExitedSteps);
            CollectionAssert.AreEqual(new[] { 0, 1, 0 }, _vm.EnteredSteps);
        }

        [Test]
        public async Task GoNext_OnLastStep_CompletesFlow()
        {
            await _vm.GoNextAsync(); // to step 1
            await _vm.GoNextAsync(); // to step 2 (last)

            Assert.IsTrue(_vm.IsLastStep);
            Assert.AreEqual(0, _vm.CompletedCallCount);

            await _vm.GoNextAsync();

            Assert.AreEqual(1, _vm.CompletedCallCount);
            // Index should remain at last step
            Assert.AreEqual(2, _vm.CurrentStepIndex);
        }

        [Test]
        public async Task GoToStep_NavigatesDirectlyToStep()
        {
            await _vm.GoToStepAsync(2);

            Assert.AreEqual(2, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsLastStep);
            CollectionAssert.AreEqual(new[] { 0 }, _vm.ExitedSteps);
            CollectionAssert.AreEqual(new[] { 0, 2 }, _vm.EnteredSteps);
        }

        [Test]
        public async Task GoToStep_BlocksIfIntermediateValidationFails()
        {
            await _vm.GoNextAsync(); // to step 1
            
            _vm.ShouldValidate = false;
            _vm.InvalidateValidation();

            await _vm.GoToStepAsync(2); // Should fail to navigate to step 2 because step 1 is invalid

            Assert.AreEqual(1, _vm.CurrentStepIndex);
        }

        [Test]
        public void CancelFlow_TriggersCallbackAndRaisesNavigationEvent()
        {
            bool navFired = false;
            string targetAction = null;

            _vm.NavigationRequested += (action, args) =>
            {
                navFired = true;
                targetAction = action;
            };

            _vm.CancelFlow();

            Assert.AreEqual(1, _vm.CancelledCallCount);
            Assert.IsTrue(navFired);
            Assert.AreEqual("popBackStack", targetAction);
        }
    }
}
