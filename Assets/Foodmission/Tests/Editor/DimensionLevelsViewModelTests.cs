using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionLevelsViewModelTests
    {
        private TestStoreService _store;
        private Mock<IAuthService> _auth;
        private Mock<IDimensionService> _dims;
        private ProfileUpdateRequest _captured;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userSegment = "INTERMEDIATE", userAutoAddToPantry = true });
            _auth = new Mock<IAuthService>();
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                 .Callback<ProfileUpdateRequest>(r => _captured = r)
                 .ReturnsAsync((true, (ApiErrorResponse)null));
            _dims = new Mock<IDimensionService>();
            _dims.Setup(d => d.GetDimension(DimensionCode.FoodWaste)).Returns(new Dimension { code = DimensionCode.FoodWaste, name = "Food waste" });
        }

        private DimensionLevelsViewModel CreateVm(DimensionLevelsMode mode)
        {
            var vm = new DimensionLevelsViewModel(_store, _auth.Object, _dims.Object) { Mode = mode };
            vm.Initialize();
            return vm;
        }

        [Test]
        public void Proposal_StartsWithSegmentForEveryDimension()
        {
            var vm = CreateVm(DimensionLevelsMode.Proposal);

            Assert.AreEqual(6, vm.Rows.Count);
            Assert.IsTrue(vm.Rows.All(r => r.Level == "INTERMEDIATE" && r.ProposedLevel == "INTERMEDIATE"));
            Assert.AreEqual("Food waste", vm.Rows.First(r => r.DimensionCode == DimensionCode.FoodWaste).DimensionName);
            Assert.IsFalse(vm.IsDifferentFromProposal);
        }

        [Test]
        public void Edit_StartsWithResolvedCurrentLevels()
        {
            _store.SetAppState(new AppState
            {
                userSegment = "BEGINNER",
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.Packaging, "ADVANCED") }
            });

            var vm = CreateVm(DimensionLevelsMode.Edit);

            Assert.AreEqual("ADVANCED", vm.Rows.First(r => r.DimensionCode == DimensionCode.Packaging).Level);
            Assert.AreEqual("BEGINNER", vm.Rows.First(r => r.DimensionCode == DimensionCode.FoodWaste).Level);
        }

        [Test]
        public void SetLevel_ThenReset_RestoresProposal()
        {
            var vm = CreateVm(DimensionLevelsMode.Proposal);

            vm.SetLevel(DimensionCode.DietChanges, "ADVANCED");
            Assert.IsTrue(vm.IsDifferentFromProposal);

            vm.ResetToProposal();
            Assert.IsFalse(vm.IsDifferentFromProposal);
            Assert.AreEqual("INTERMEDIATE", vm.Rows.First(r => r.DimensionCode == DimensionCode.DietChanges).Level);
        }

        [Test]
        public void SetLevel_IgnoresInvalidLevel()
        {
            var vm = CreateVm(DimensionLevelsMode.Proposal);

            vm.SetLevel(DimensionCode.DietChanges, "EXPERT");

            Assert.AreEqual("INTERMEDIATE", vm.Rows.First(r => r.DimensionCode == DimensionCode.DietChanges).Level);
        }

        [Test]
        public async Task Save_SendsOnlyLevelsAndKeepsAutoAddToPantry()
        {
            var vm = CreateVm(DimensionLevelsMode.Proposal);
            vm.SetLevel(DimensionCode.FoodWaste, "ADVANCED");

            await vm.SaveAsync();

            Assert.IsNotNull(_captured);
            Assert.AreEqual(6, _captured.preferences.dimensionLevels.Count);
            Assert.AreEqual("ADVANCED", _captured.preferences.dimensionLevels[DimensionCode.FoodWaste]);
            Assert.IsTrue(_captured.preferences.autoAddToPantry, "autoAddToPantry is a non-nullable bool and must keep the user's value");
            Assert.IsNull(_captured.preferences.goals);
            Assert.IsNull(_captured.preferences.onboardingSurvey);
            Assert.IsNull(_captured.segment);
            Assert.AreEqual("ADVANCED", DimensionLevels.GetLevel(_store.GetAppState(), DimensionCode.FoodWaste));
            Assert.IsTrue(_store.GetAppState().hasConfirmedDimensionLevels);
        }

        [Test]
        public async Task Save_Proposal_NavigatesToAvatar_OrHomeWhenFromHome()
        {
            var vm = CreateVm(DimensionLevelsMode.Proposal);
            string action = null;
            vm.NavigationRequested += (a, args) => action = a;

            await vm.SaveAsync();
            Assert.AreEqual(Actions.onboardingprofile_to_onboardingavatar, action);

            var fromHome = CreateVm(DimensionLevelsMode.Proposal);
            fromHome.FromHome = true;
            fromHome.NavigationRequested += (a, args) => action = a;
            await fromHome.SaveAsync();
            Assert.AreEqual(Actions.go_to_home, action);
        }

        [Test]
        public async Task Save_Edit_PopsBack()
        {
            var vm = CreateVm(DimensionLevelsMode.Edit);
            string action = null;
            vm.NavigationRequested += (a, args) => action = a;

            await vm.SaveAsync();

            Assert.AreEqual("popBackStack", action);
        }

        [Test]
        public async Task Save_Failure_SetsErrorAndKeepsState()
        {
            var error = new ApiErrorResponse { statusCode = 500, message = "boom" };
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((false, error));
            var vm = CreateVm(DimensionLevelsMode.Proposal);
            string action = null;
            vm.NavigationRequested += (a, args) => action = a;

            await vm.SaveAsync();

            Assert.AreSame(error, vm.ErrorDetail);
            Assert.IsNull(action);
            Assert.IsFalse(_store.GetAppState().hasConfirmedDimensionLevels);
            Assert.AreEqual(0, _store.GetAppState().dimensionLevels.Length);
        }

        [Test]
        public void Initialize_EnablesContinue()
        {
            // StepFlowViewModelBase validates the step inside Initialize, before the rows exist
            var vm = CreateVm(DimensionLevelsMode.Proposal);

            Assert.IsTrue(vm.CanGoNext, "Continue must be enabled: the levels always have a value");

            vm.Mode = DimensionLevelsMode.Edit;
            vm.Reload();
            Assert.IsTrue(vm.CanGoNext);
        }

        [Test]
        public async Task EnsureDimensionNamesAsync_PreloadsCatalogueAndRenamesRows()
        {
            var dims = new Mock<IDimensionService>();
            bool loaded = false;
            dims.Setup(d => d.IsLoaded).Returns(() => loaded);
            dims.Setup(d => d.PreloadAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Callback(() => loaded = true)
                .ReturnsAsync((new Dimension[0], (ApiErrorResponse)null));
            dims.Setup(d => d.GetDimension(DimensionCode.Packaging))
                .Returns(() => loaded ? new Dimension { code = DimensionCode.Packaging, name = "Packaging" } : null);
            var vm = new DimensionLevelsViewModel(_store, _auth.Object, dims.Object) { Mode = DimensionLevelsMode.Proposal };
            vm.Initialize();
            vm.SetLevel(DimensionCode.Packaging, "ADVANCED");
            int changes = 0;
            vm.RowsChanged += () => changes++;

            await vm.EnsureDimensionNamesAsync();

            DimensionLevelRow row = vm.Rows.First(r => r.DimensionCode == DimensionCode.Packaging);
            Assert.AreEqual("Packaging", row.DimensionName);
            Assert.AreEqual("ADVANCED", row.Level, "Loading names must not reset the user's choice");
            Assert.AreEqual(1, changes);
        }
    }
}
