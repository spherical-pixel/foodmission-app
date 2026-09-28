using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class NutriEditorViewModelTests
    {
        private Mock<IStoreService> _storeServiceMock;
        private Mock<IFoodyService> _foodyServiceMock;
        private Mock<INutriService> _nutriServiceMock;
        private AppState _appState;

        [SetUp]
        public void SetUp()
        {
            _appState = new AppState { userPoints = 100 };
            _storeServiceMock = new Mock<IStoreService>();
            _storeServiceMock.Setup(s => s.GetAppState()).Returns(_appState);

            _foodyServiceMock = new Mock<IFoodyService>();
            _nutriServiceMock = new Mock<INutriService>();
        }

        [Test]
        public async Task InitializeAsync_LoadsCatalogAndCurrentLoadout()
        {
            var loadout = new FoodyLoadout
            {
                antennas = new FoodyItem { code = "ANTENNAS_1", type = "ANTENNAS", slot = 1, owned = true }
            };
            var items = new List<FoodyItem>
            {
                new FoodyItem { code = "ANTENNAS_1", type = "ANTENNAS", slot = 1, owned = true, cost = 0 },
                new FoodyItem { code = "ANTENNAS_2", type = "ANTENNAS", slot = 2, owned = false, cost = 5 }
            };

            _foodyServiceMock.Setup(f => f.GetLoadoutAsync()).ReturnsAsync((loadout, null));
            _foodyServiceMock.Setup(f => f.GetItemsAsync(null, null)).ReturnsAsync((items, null));

            var vm = new NutriEditorViewModel(_storeServiceMock.Object, _foodyServiceMock.Object, _nutriServiceMock.Object);
            await vm.InitializeAsync();

            Assert.AreEqual(1, vm.GetEquippedSlot(FoodyItemType.Antennas));
            Assert.AreEqual(2, vm.GetItemsForCategory(FoodyItemType.Antennas).Count);
            _nutriServiceMock.Verify(n => n.ApplyLoadout(It.IsAny<FoodyLoadout>()), Times.Once);
        }

        [Test]
        public async Task EquipOwnedItem_UpdatesWorkingLoadoutAndNutriView()
        {
            var loadout = new FoodyLoadout();
            var items = new List<FoodyItem>
            {
                new FoodyItem { code = "GLASSES_3", type = "GLASSES", slot = 3, owned = true }
            };
            _foodyServiceMock.Setup(f => f.GetLoadoutAsync()).ReturnsAsync((loadout, null));
            _foodyServiceMock.Setup(f => f.GetItemsAsync(null, null)).ReturnsAsync((items, null));

            var vm = new NutriEditorViewModel(_storeServiceMock.Object, _foodyServiceMock.Object, _nutriServiceMock.Object);
            await vm.InitializeAsync();

            vm.EquipItem(FoodyItemType.Glasses, 3);

            Assert.AreEqual(3, vm.GetEquippedSlot(FoodyItemType.Glasses));
            _nutriServiceMock.Verify(n => n.EquipItem(FoodyItemType.Glasses, 3), Times.Once);
        }

        [Test]
        public async Task PurchaseItem_WhenSufficientPoints_CallsServiceAndUpdatesItem()
        {
            var item = new FoodyItem { code = "EARS_2", type = "EARS", slot = 2, owned = false, cost = 10 };
            _foodyServiceMock.Setup(f => f.PurchaseItemAsync("EARS_2"))
                .ReturnsAsync((new FoodyPurchaseResponse { item = item, pricePaid = 10, pointsBalance = 90 }, null));

            var vm = new NutriEditorViewModel(_storeServiceMock.Object, _foodyServiceMock.Object, _nutriServiceMock.Object);

            bool success = await vm.PurchaseItemAsync(item);

            Assert.IsTrue(success);
            Assert.IsTrue(item.owned);
            _foodyServiceMock.Verify(f => f.PurchaseItemAsync("EARS_2"), Times.Once);
        }

        [Test]
        public async Task PurchaseItem_WhenInsufficientPoints_DoesNotCallService()
        {
            _appState.userPoints = 2;
            var item = new FoodyItem { code = "EARS_2", type = "EARS", slot = 2, owned = false, cost = 10 };

            var vm = new NutriEditorViewModel(_storeServiceMock.Object, _foodyServiceMock.Object, _nutriServiceMock.Object);

            bool success = await vm.PurchaseItemAsync(item);

            Assert.IsFalse(success);
            _foodyServiceMock.Verify(f => f.PurchaseItemAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SaveAsync_EquipsChangedItemsAndUnequipsRemovedItems()
        {
            var initialLoadout = new FoodyLoadout
            {
                antennas = new FoodyItem { code = "ANTENNAS_1", type = "ANTENNAS", slot = 1, owned = true },
                glasses = new FoodyItem { code = "GLASSES_2", type = "GLASSES", slot = 2, owned = true }
            };
            var items = new List<FoodyItem>
            {
                new FoodyItem { code = "ANTENNAS_1", type = "ANTENNAS", slot = 1, owned = true },
                new FoodyItem { code = "ANTENNAS_3", type = "ANTENNAS", slot = 3, owned = true },
                new FoodyItem { code = "GLASSES_2", type = "GLASSES", slot = 2, owned = true }
            };
            _foodyServiceMock.Setup(f => f.GetLoadoutAsync()).ReturnsAsync((initialLoadout, null));
            _foodyServiceMock.Setup(f => f.GetItemsAsync(null, null)).ReturnsAsync((items, null));

            var vm = new NutriEditorViewModel(_storeServiceMock.Object, _foodyServiceMock.Object, _nutriServiceMock.Object);
            await vm.InitializeAsync();

            // Change Antennas from 1 to 3, and Unequip Glasses
            vm.EquipItem(FoodyItemType.Antennas, 3);
            vm.UnequipCategory(FoodyItemType.Glasses);

            await vm.SaveAsync();

            _foodyServiceMock.Verify(f => f.EquipItemAsync("ANTENNAS_3"), Times.Once);
            _foodyServiceMock.Verify(f => f.UnequipItemAsync("GLASSES_2"), Times.Once);
        }

        [Test]
        public async Task ExitWithoutSaving_RestoresInitialLoadout()
        {
            var initialLoadout = new FoodyLoadout
            {
                antennas = new FoodyItem { code = "ANTENNAS_1", type = "ANTENNAS", slot = 1, owned = true }
            };
            _foodyServiceMock.Setup(f => f.GetLoadoutAsync()).ReturnsAsync((initialLoadout, null));
            _foodyServiceMock.Setup(f => f.GetItemsAsync(null, null)).ReturnsAsync((new List<FoodyItem>(), null));

            var vm = new NutriEditorViewModel(_storeServiceMock.Object, _foodyServiceMock.Object, _nutriServiceMock.Object);
            await vm.InitializeAsync();

            vm.EquipItem(FoodyItemType.Antennas, 2);
            vm.ExitWithoutSaving();

            _nutriServiceMock.Verify(n => n.ApplyLoadout(It.Is<FoodyLoadout>(l => l.antennas != null && l.antennas.slot == 1)), Times.Once);
            _foodyServiceMock.Verify(f => f.EquipItemAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
