using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelInfoViewModelTests
    {
        private TestStoreService _store;
        private ProgressWheelInfoViewModel _vm;
        private int _backRequests;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState
            {
                userId = "u1",
                progressWheels = new[]
                {
                    new ProgressWheel { kind = "CO2_REDUCTION", percentComplete = 40f },
                    new ProgressWheel { kind = "WATER_SAVINGS", percentComplete = 10f },
                },
            });
            _vm = new ProgressWheelInfoViewModel(_store);
            _backRequests = 0;
            _vm.BackRequested += () => _backRequests++;
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _store?.Dispose();
        }

        [Test]
        public void Load_KnownKind_SetsKindAndCachedWheel()
        {
            _vm.Load("CO2_REDUCTION");

            Assert.AreEqual("CO2_REDUCTION", _vm.Kind);
            Assert.IsNotNull(_vm.Wheel);
            Assert.AreEqual(40f, _vm.Wheel.percentComplete);
            Assert.AreEqual(0, _backRequests);
        }

        [Test]
        public void Load_NoCachedWheel_WheelIsNull()
        {
            _vm.Load("LAND_USE_REDUCTION");

            Assert.AreEqual("LAND_USE_REDUCTION", _vm.Kind);
            Assert.IsNull(_vm.Wheel);
            Assert.AreEqual(0, _backRequests);
        }

        [TestCase("X")]
        [TestCase("")]
        [TestCase(null)]
        public void Load_UnknownKind_RequestsBack(string kind)
        {
            _vm.Load(kind);

            Assert.AreEqual(1, _backRequests);
            Assert.AreEqual(string.Empty, _vm.Kind);
            Assert.IsNull(_vm.Wheel);
        }
    }
}
