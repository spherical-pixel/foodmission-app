using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;
using eu.foodmission.platform;
using eu.foodmission.platform.Tests;

namespace eu.foodmission.tests
{
    [TestFixture]
    public class AvatarPersistenceTests
    {
        private TestStoreService _storeService;
        private TestLocalStorageService _localStorageService;

        [SetUp]
        public void SetUp()
        {
            _localStorageService = new TestLocalStorageService();
            _storeService = new TestStoreService();
            PlayerPrefs.DeleteKey("AvatarConfig");
            PlayerPrefs.DeleteKey("HasAvatar");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("AvatarConfig");
            PlayerPrefs.DeleteKey("HasAvatar");
        }

        [Test]
        public void ProfilePreferences_Serialization_IncludesAvatarConfigAndHasAvatar()
        {
            var config = new AvatarConfig
            {
                hair = new AvatarPartConfig { idPart = 2, idColor = 3 },
                skin = new AvatarPartConfig { idPart = 0, idColor = 5 }
            };

            var prefs = new ProfilePreferences
            {
                dietaryPreference = new[] { "VEGAN" },
                shoppingResponsibility = "PRIMARY",
                avatarConfig = config,
                hasAvatar = true
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(prefs);
            Assert.IsTrue(json.Contains("avatarConfig"));
            Assert.IsTrue(json.Contains("hasAvatar"));

            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<ProfilePreferences>(json);
            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.hasAvatar);
            Assert.IsNotNull(deserialized.avatarConfig);
            Assert.AreEqual(2, deserialized.avatarConfig.hair.idPart);
            Assert.AreEqual(3, deserialized.avatarConfig.hair.idColor);
            Assert.AreEqual(5, deserialized.avatarConfig.skin.idColor);
        }

        [Test]
        public void ProfileUpdatePreferences_Serialization_IgnoresNullsAndIncludesAvatarFields()
        {
            var updatePrefs = new ProfileUpdatePreferences
            {
                hasAvatar = false
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(updatePrefs, new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

            Assert.IsTrue(json.Contains("\"hasAvatar\":false"));
            Assert.IsFalse(json.Contains("avatarConfig"));

            updatePrefs.hasAvatar = true;
            updatePrefs.avatarConfig = new AvatarConfig
            {
                eyes = new AvatarPartConfig { idPart = 1, idColor = 4 }
            };

            json = Newtonsoft.Json.JsonConvert.SerializeObject(updatePrefs, new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

            Assert.IsTrue(json.Contains("\"hasAvatar\":true"));
            Assert.IsTrue(json.Contains("avatarConfig"));
        }

        [Test]
        public void AppState_Copy_PreservesAvatarConfigAndHasAvatar()
        {
            var state = new AppState();
            state.userHasAvatar = true;
            state.userAvatarConfig = new AvatarConfig
            {
                mouth = new AvatarPartConfig { idPart = 3, idColor = 1 }
            };

            var copy = state.Copy();
            Assert.IsTrue(copy.userHasAvatar);
            Assert.IsNotNull(copy.userAvatarConfig);
            Assert.AreEqual(3, copy.userAvatarConfig.mouth.idPart);
            Assert.AreEqual(1, copy.userAvatarConfig.mouth.idColor);

            // Ensure deep copy of avatar config
            copy.userAvatarConfig.mouth.idPart = 9;
            Assert.AreEqual(3, state.userAvatarConfig.mouth.idPart);
        }

        [Test]
        public void SetAvatarReducer_UpdatesAppStateCorrectly()
        {
            var config = new AvatarConfig
            {
                nose = new AvatarPartConfig { idPart = 4, idColor = 2 }
            };

            _storeService.store.Dispatch(AppActions.setAvatar.Invoke(new AppActions.AvatarPayload(config, true)));

            AppState state = _storeService.GetAppState();
            Assert.IsTrue(state.userHasAvatar);
            Assert.IsNotNull(state.userAvatarConfig);
            Assert.AreEqual(4, state.userAvatarConfig.nose.idPart);
        }

        [Test]
        public void ProfileSyncedReducer_UpdatesAvatarConfigAndHasAvatar()
        {
            var config = new AvatarConfig
            {
                shoes = new AvatarPartConfig { idPart = 1, idColor = 6 }
            };

            var payload = new AppActions.ProfilePayload(
                yearOfBirth: 1990,
                country: "ES",
                region: "Cat",
                zip: "08001",
                gender: "MALE",
                annualIncome: "MEDIUM",
                educationLevel: "UNIVERSITY",
                activityLevel: "MODERATE",
                avatarConfig: config,
                hasAvatar: true
            );

            _storeService.store.Dispatch(AppActions.profileSynced.Invoke(payload));

            AppState state = _storeService.GetAppState();
            Assert.IsTrue(state.userHasAvatar);
            Assert.IsNotNull(state.userAvatarConfig);
            Assert.AreEqual(6, state.userAvatarConfig.shoes.idColor);
        }

        [Test]
        public async Task AvatarService_SaveAndSkip_UpdatesStateAndPrefs()
        {
            var avatarService = new AvatarService(_storeService, null);

            var config = new AvatarConfig
            {
                tshirt = new AvatarPartConfig { idPart = 1, idColor = 8 }
            };

            avatarService.SetAvatarConfig(config);
            await avatarService.SaveCurrentConfigAsync(true);

            Assert.IsTrue(avatarService.HasAvatar);
            Assert.IsTrue(PlayerPrefs.HasKey("AvatarConfig"));
            Assert.AreEqual(1, PlayerPrefs.GetInt("HasAvatar"));

            AppState state = _storeService.GetAppState();
            Assert.IsTrue(state.userHasAvatar);
            Assert.IsNotNull(state.userAvatarConfig);
            Assert.AreEqual(8, state.userAvatarConfig.tshirt.idColor);

            // Test skipping / disabling avatar
            await avatarService.SetHasAvatarAsync(false);
            Assert.IsFalse(avatarService.HasAvatar);
            Assert.AreEqual(0, PlayerPrefs.GetInt("HasAvatar"));
            Assert.IsFalse(_storeService.GetAppState().userHasAvatar);
        }
    }
}
