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
            _store.SetAppState(new AppState { dimensionLevels = DimensionCode.All.Select(c => new DimensionLevelEntry(c, "INTERMEDIATE")).ToArray(), userAutoAddToPantry = true });
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
        public void Proposal_StartsWithStoredBackendLevels()
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
        public async Task Save_SendsOnlyChangedLevelsAtRoot_AndNothingElse()
        {
            var vm = CreateVm(DimensionLevelsMode.Proposal);
            vm.SetLevel(DimensionCode.FoodWaste, "BEGINNER");

            await vm.SaveAsync();

            Assert.IsNotNull(_captured);
            Assert.AreEqual(1, _captured.dimensionLevels.Length, "unchanged rows are not sent, so a stale proposal never overwrites the backend");
            Assert.AreEqual(DimensionCode.FoodWaste, _captured.dimensionLevels[0].dimensionCode);
            Assert.AreEqual("BEGINNER", _captured.dimensionLevels[0].level);
            Assert.IsNull(_captured.preferences, "no preferences: autoAddToPantry (non-nullable) must not be sent and reset");
            Assert.IsNull(_captured.segment);
            Assert.AreEqual("BEGINNER", DimensionLevels.GetLevel(_store.GetAppState(), DimensionCode.FoodWaste));
        }

        [Test]
        public async Task Save_NothingChanged_SkipsPatch_AndStillContinues()
        {
            // e.g. the level refresh after the survey failed and the proposal shows stale levels: accepting must not lower the backend's scoring
            var vm = CreateVm(DimensionLevelsMode.Proposal);
            string action = null;
            vm.NavigationRequested += (a, args) => action = a;

            await vm.SaveAsync();

            _auth.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
            Assert.IsNotNull(action);
            Assert.IsNull(vm.ErrorDetail);
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
        public void Close_Proposal_GoesHome_Edit_PopsBack()
        {
            var proposal = CreateVm(DimensionLevelsMode.Proposal);
            string action = null;
            proposal.NavigationRequested += (a, args) => action = a;
            proposal.Close();
            Assert.AreEqual(Actions.go_to_home, action);

            var edit = CreateVm(DimensionLevelsMode.Edit);
            edit.NavigationRequested += (a, args) => action = a;
            edit.Close();
            Assert.AreEqual("popBackStack", action);
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
            vm.SetLevel(DimensionCode.FoodWaste, "ADVANCED");
            string action = null;
            vm.NavigationRequested += (a, args) => action = a;

            await vm.SaveAsync();

            Assert.AreSame(error, vm.ErrorDetail);
            Assert.IsNull(action);
            Assert.AreEqual("INTERMEDIATE", DimensionLevels.GetLevel(_store.GetAppState(), DimensionCode.FoodWaste), "the stored (backend) level is kept");
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
