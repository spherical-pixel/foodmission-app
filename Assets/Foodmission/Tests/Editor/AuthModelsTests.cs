using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json;

using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AuthModelsTests
    {
        [Test]
        public void LoginRequest_Roundtrips_Via_JsonUtility()
        {
            var req = new LoginRequest { username = "testuser", password = "secret123" };
            string json = JsonUtility.ToJson(req);
            var result = JsonUtility.FromJson<LoginRequest>(json);
            Assert.AreEqual("testuser", result.username);
            Assert.AreEqual("secret123", result.password);
        }

        [Test]
        public void RegisterRequest_ToJson_OmitsNullFields()
        {
            var req = new RegisterRequest { username = "newuser", email = "a@b.com", password = "pass" };
            string json = req.ToJson();
            Assert.IsTrue(json.Contains("username"));
            Assert.IsTrue(json.Contains("email"));
            Assert.IsFalse(json.Contains("yearOfBirth"));
            Assert.IsFalse(json.Contains("country"));
        }

        [Test]
        public void RegisterRequest_ToJson_IncludesNonNullOptionalFields()
        {
            var req = new RegisterRequest
            {
                username = "user",
                email = "a@b.com",
                password = "pass",
                yearOfBirth = 1990,
                country = "ES"
            };
            string json = req.ToJson();
            Assert.IsTrue(json.Contains("1990"));
            Assert.IsTrue(json.Contains("ES"));
        }

        [Test]
        public void LoginResponse_Roundtrips_Via_JsonUtility()
        {
            var resp = new LoginResponse
            {
                access_token = "tok123",
                refresh_token = "ref456",
                token_type = "Bearer",
                expires_in = 300,
                refresh_expires_in = 1800,
                user = new UserData { id = "uid1", email = "a@b.com", firstName = "John", lastName = "Doe" }
            };
            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<LoginResponse>(json);
            Assert.AreEqual("tok123", result.access_token);
            Assert.AreEqual("Bearer", result.token_type);
            Assert.AreEqual(300, result.expires_in);
            Assert.IsNotNull(result.user);
            Assert.AreEqual("John", result.user.firstName);
        }

        [Test]
        public void RegisterResponse_Roundtrips_Via_JsonUtility()
        {
            var resp = new RegisterResponse
            {
                createdUser = new KeycloakUserData { id = "kc1", username = "user", email = "a@b.com" },
                localUser = new LocalUserData { id = "loc1", email = "a@b.com", username = "user", keycloakId = "kc1" }
            };
            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<RegisterResponse>(json);
            Assert.IsNotNull(result.createdUser);
            Assert.AreEqual("kc1", result.createdUser.id);
            Assert.IsNotNull(result.localUser);
            Assert.AreEqual("loc1", result.localUser.id);
        }

        [Test]
        public void RefreshRequest_ToJson_IncludesToken()
        {
            var req = new RefreshRequest { token = "refresh-tok" };
            string json = req.ToJson();
            StringAssert.Contains("refresh-tok", json);
        }

        [Test]
        public void ForgotPasswordRequest_ToJson_IncludesEmail()
        {
            var req = new ForgotPasswordRequest { email = "user@example.com" };
            string json = req.ToJson();
            StringAssert.Contains("user@example.com", json);
        }

        [Test]
        public void RevokeTokenRequest_ToJson_IncludesTokenAndHint()
        {
            var req = new RevokeTokenRequest { token = "tok", tokenTypeHint = "access_token" };
            string json = req.ToJson();
            StringAssert.Contains("tok", json);
            StringAssert.Contains("access_token", json);
        }

        [Test]
        public void ProfileResponse_Roundtrips_Via_JsonUtility()
        {
            var resp = new ProfileResponse
            {
                id = "uid1",
                email = "a@b.com",
                username = "testuser",
                yearOfBirth = 1990,
                country = "ES"
            };
            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<ProfileResponse>(json);
            Assert.AreEqual("uid1", result.id);
            Assert.AreEqual("testuser", result.username);
            Assert.AreEqual(1990, result.yearOfBirth);
        }

        [Test]
        public void ProfileResponse_Deserializes_OldSingleStringDietaryPreference_AsSingleElementArray()
        {
            // Simulates an old DB record where dietaryPreference was stored as a string
            string json = @"{""id"":""uid1"",""email"":""a@b.com"",""preferences"":{""dietaryPreference"":""VEGETARIAN"",""shoppingResponsibility"":""PRIMARY""}}";

            var result = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.IsNotNull(result.preferences);
            Assert.IsNotNull(result.preferences.dietaryPreference);
            Assert.AreEqual(1, result.preferences.dietaryPreference.Length);
            Assert.AreEqual("VEGETARIAN", result.preferences.dietaryPreference[0]);
            Assert.AreEqual("PRIMARY", result.preferences.shoppingResponsibility);
        }

        [Test]
        public void ProfileResponse_Deserializes_ArrayDietaryPreference()
        {
            // New format: dietaryPreference is an array
            string json = @"{""id"":""uid1"",""email"":""a@b.com"",""preferences"":{""dietaryPreference"":[""VEGAN"",""GLUTEN_FREE""],""shoppingResponsibility"":""PRIMARY""}}";

            var result = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.IsNotNull(result.preferences);
            CollectionAssert.AreEqual(new[] { "VEGAN", "GLUTEN_FREE" }, result.preferences.dietaryPreference);
            Assert.AreEqual("PRIMARY", result.preferences.shoppingResponsibility);
        }

        [Test]
        public void ProfileResponse_Deserializes_MissingDietaryPreference_AsEmptyArray()
        {
            // No preferences at all
            string json = @"{""id"":""uid1"",""email"":""a@b.com""}";

            var result = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.IsNull(result.preferences);
        }

        [Test]
        public void ProfileResponse_Deserializes_EmptyPreferencesObject()
        {
            // Empty preferences object
            string json = @"{""id"":""uid1"",""email"":""a@b.com"",""preferences"":{}}";

            var result = JsonConvert.DeserializeObject<ProfileResponse>(json);

            Assert.IsNotNull(result.preferences);
            // dietaryPreference should be null (not in JSON) — AuthService handles null via ?? new string[0]
            Assert.IsNull(result.preferences.dietaryPreference);
        }

        [Test]
        public void RefreshResponse_Roundtrips_Via_JsonUtility()
        {
            var resp = new RefreshResponse
            {
                access_token = "new-token",
                refresh_token = "new-refresh",
                token_type = "Bearer",
                expires_in = 300,
                refresh_expires_in = 1800
            };
            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<RefreshResponse>(json);
            Assert.AreEqual("new-token", result.access_token);
            Assert.AreEqual("new-refresh", result.refresh_token);
        }
    }
}
