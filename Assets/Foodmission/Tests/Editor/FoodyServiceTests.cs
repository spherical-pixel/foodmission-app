using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodyServiceTests
    {
        private TestStoreService _storeService;
        private FoodyService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                userXp = 500,
                userPoints = 1000
            });
            _service = new FoodyService(_storeService);
        }

        [Test]
        public async Task PurchaseItemAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (resultEmpty, errorEmpty) = await _service.PurchaseItemAsync("");
            Assert.IsNull(resultEmpty);
            Assert.IsNotNull(errorEmpty);
            Assert.AreEqual("Item code or ID is required", errorEmpty.message);

            var (resultNull, errorNull) = await _service.PurchaseItemAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
            Assert.AreEqual("Item code or ID is required", errorNull.message);
        }

        [Test]
        public async Task EquipItemAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (resultEmpty, errorEmpty) = await _service.EquipItemAsync("");
            Assert.IsNull(resultEmpty);
            Assert.IsNotNull(errorEmpty);
            Assert.AreEqual("Item code or ID is required", errorEmpty.message);

            var (resultNull, errorNull) = await _service.EquipItemAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
            Assert.AreEqual("Item code or ID is required", errorNull.message);
        }

        [Test]
        public async Task UnequipItemAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (resultEmpty, errorEmpty) = await _service.UnequipItemAsync("");
            Assert.IsNull(resultEmpty);
            Assert.IsNotNull(errorEmpty);
            Assert.AreEqual("Item code or ID is required", errorEmpty.message);

            var (resultNull, errorNull) = await _service.UnequipItemAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
            Assert.AreEqual("Item code or ID is required", errorNull.message);
        }

        [Test]
        public void SetWalletBalance_ActionAndReducer_UpdatesStore()
        {
            _storeService.store.Dispatch(AppActions.setWalletBalance.Invoke(new AppActions.WalletPayload(650, 450)));

            var state = _storeService.GetAppState();
            Assert.AreEqual(650, state.userXp);
            Assert.AreEqual(450, state.userPoints);
        }

        [Test]
        public void LogoutReducer_Resets_WalletBalance()
        {
            _storeService.store.Dispatch(AppActions.setWalletBalance.Invoke(new AppActions.WalletPayload(650, 450)));
            Assert.AreEqual(450, _storeService.GetAppState().userPoints);

            _storeService.store.Dispatch(AppActions.logout.Invoke());
            var state = _storeService.GetAppState();
            Assert.AreEqual(0, state.userXp);
            Assert.AreEqual(0, state.userPoints);
        }
    }
}
