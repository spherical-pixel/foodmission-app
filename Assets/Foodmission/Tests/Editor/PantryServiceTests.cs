using System.Reflection;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PantryServiceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private TestStoreService _storeService;
        private PantryService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            SetUser("user-a");
            _service = new PantryService(_storeService);
            SetField("_pantryId", "pantry-a");
            SetField("_pantryOwnerId", "user-a");
        }

        [TearDown]
        public void TearDown()
        {
            _storeService.Dispose();
        }

        private void SetUser(string userId)
        {
            AppState state = _storeService.GetAppState().Copy();
            state.userId = userId;
            _storeService.SetAppState(state);
        }

        private void SetField(string name, object value)
        {
            typeof(PantryService).GetField(name, Private).SetValue(_service, value);
        }

        private string CachedPantryId()
        {
            return (string)typeof(PantryService).GetMethod("CachedPantryIdForCurrentUser", Private).Invoke(_service, null);
        }

        [Test]
        public void CachedPantryId_SameUser_IsReused()
        {
            Assert.AreEqual("pantry-a", CachedPantryId());
        }

        [Test]
        public void CachedPantryId_AfterUserChange_IsDiscarded()
        {
            SetUser("user-b");

            Assert.IsNull(CachedPantryId());

            SetUser("user-a");
            Assert.IsNull(CachedPantryId());
        }
    }
}
