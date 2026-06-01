using NUnit.Framework;
using UnityEngine;

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
                firstName = "Test",
                lastName = "User",
                yearOfBirth = 1990,
                country = "ES",
                weightKg = 75.5f,
                heightCm = 180f
            };
            string json = JsonUtility.ToJson(resp);
            var result = JsonUtility.FromJson<ProfileResponse>(json);
            Assert.AreEqual("uid1", result.id);
            Assert.AreEqual("testuser", result.username);
            Assert.AreEqual(1990, result.yearOfBirth);
            Assert.AreEqual(75.5f, result.weightKg, 0.001f);
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
