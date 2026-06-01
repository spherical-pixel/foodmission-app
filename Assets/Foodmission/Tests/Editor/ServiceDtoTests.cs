using System;
using System.Text;

using NUnit.Framework;

using eu.foodmission.platform;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodWasteDtoTests
    {
        [Test]
        public void CreateFoodWasteRequest_ToJsonBody_SerializesAllFields()
        {
            var req = new CreateFoodWasteRequest
            {
                pantryItemId = "pi1",
                quantity = 2.5f,
                unit = "kg",
                wasteReason = WasteReason.Expired,
                detectionMethod = DetectionMethod.Manual,
                notes = "Test note",
                costEstimate = 5.99f,
                wastedAt = "2026-05-30T12:00:00Z"
            };

            byte[] jsonBytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(jsonBytes);

            Assert.IsTrue(json.Contains("pi1"));
            Assert.IsTrue(json.Contains("2.5") || json.Contains("2,5"));
            Assert.IsTrue(json.Contains("kg"));
            Assert.IsTrue(json.Contains(WasteReason.Expired));
            Assert.IsTrue(json.Contains(DetectionMethod.Manual));
            Assert.IsTrue(json.Contains("Test note"));
            Assert.IsTrue(json.Contains("5.99") || json.Contains("5,99"));
        }

        [Test]
        public void CreateFoodWasteRequest_ToJsonBody_SkipsNullFields()
        {
            var req = new CreateFoodWasteRequest
            {
                pantryItemId = "pi1",
                unit = "kg",
                wasteReason = WasteReason.Spoiled,
                detectionMethod = DetectionMethod.Automatic
            };

            byte[] jsonBytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(jsonBytes);

            Assert.IsFalse(json.Contains("costEstimate"));
            Assert.IsFalse(json.Contains("carbonFootprint"));
            Assert.IsFalse(json.Contains("notes"));
        }

        [Test]
        public void BatchWasteRequest_ToJsonBody_SerializesItems()
        {
            var req = new BatchWasteRequest
            {
                items = new[]
                {
                    new BatchWasteItemRequest { pantryItemId = "pi1", quantity = 1, unit = "kg" },
                    new BatchWasteItemRequest { pantryItemId = "pi2", quantity = 2, unit = "L" },
                }
            };

            byte[] jsonBytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(jsonBytes);

            Assert.IsTrue(json.Contains("pi1"));
            Assert.IsTrue(json.Contains("pi2"));
            Assert.IsTrue(json.Contains("items"));
            Assert.IsTrue(json.Contains("quantity"));
        }

        [Test]
        public void WasteReason_ContainsExpectedConstants()
        {
            Assert.AreEqual("EXPIRED", WasteReason.Expired);
            Assert.AreEqual("SPOILED", WasteReason.Spoiled);
            Assert.AreEqual("OVERCOOKED", WasteReason.Overcooked);
            Assert.AreEqual("UNWANTED", WasteReason.Unwanted);
            Assert.AreEqual("PORTION_TOO_LARGE", WasteReason.PortionTooLarge);
            Assert.AreEqual("OTHER", WasteReason.Other);
            Assert.AreEqual(6, WasteReason.All.Length);
        }

        [Test]
        public void DetectionMethod_ContainsExpectedConstants()
        {
            Assert.AreEqual("AUTOMATIC", DetectionMethod.Automatic);
            Assert.AreEqual("MANUAL", DetectionMethod.Manual);
            Assert.AreEqual(2, DetectionMethod.All.Length);
        }
    }

    [TestFixture]
    public class MealLogDtoTests
    {
        [Test]
        public void CreateMealLogRequest_ToJsonBody_SerializesFields()
        {
            var req = new CreateMealLogRequest
            {
                mealId = "meal1",
                typeOfMeal = "LUNCH",
                mealFromPantry = true,
                eatenOut = false,
                timestamp = "2026-05-30T12:00:00Z"
            };

            byte[] jsonBytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(jsonBytes);

            Assert.IsTrue(json.Contains("meal1"));
            Assert.IsTrue(json.Contains("LUNCH"));
            Assert.IsTrue(json.Contains("mealFromPantry"));
        }

        [Test]
        public void CreateMealLogRequest_ToJsonBody_SkipsNullTimestamp()
        {
            var req = new CreateMealLogRequest
            {
                mealId = "meal1",
                typeOfMeal = "BREAKFAST"
            };

            byte[] jsonBytes = req.ToJsonBody();
            string json = Encoding.UTF8.GetString(jsonBytes);

            Assert.IsFalse(json.Contains("timestamp"));
        }
    }

    [TestFixture]
    public class AuthDtoTests
    {
        [Test]
        public void RegisterRequest_ToJson_SerializesWithNullHandling()
        {
            var req = new RegisterRequest
            {
                username = "testuser",
                email = "test@example.com",
                password = "password123",
                yearOfBirth = 2000,
                country = "ES",
                region = "CT",
                zip = "08001"
            };

            string json = req.ToJson();

            Assert.IsTrue(json.Contains("testuser"));
            Assert.IsTrue(json.Contains("test@example.com"));
            Assert.IsTrue(json.Contains("2000"));
            Assert.IsTrue(json.Contains("ES"));
            Assert.IsTrue(json.Contains("CT"));
            Assert.IsTrue(json.Contains("08001"));
        }

        [Test]
        public void RegisterRequest_ToJson_SkipsNullOptionalFields()
        {
            var req = new RegisterRequest
            {
                username = "testuser",
                email = "test@example.com",
                password = "password123"
            };

            string json = req.ToJson();

            Assert.IsFalse(json.Contains("yearOfBirth"));
            Assert.IsFalse(json.Contains("country"));
            Assert.IsFalse(json.Contains("region"));
            Assert.IsFalse(json.Contains("zip"));
            Assert.IsTrue(json.Contains("testuser"));
        }

        [Test]
        public void ForgotPasswordRequest_ToJson_Serializes()
        {
            var req = new ForgotPasswordRequest { email = "user@example.com" };

            string json = req.ToJson();

            Assert.IsTrue(json.Contains("user@example.com"));
            Assert.IsTrue(json.Contains("email"));
        }
    }

    [TestFixture]
    public class ProfileUpdateDtoTests
    {
        [Test]
        public void ProfileUpdateRequest_ToJson_SerializesNestedPreferences()
        {
            var req = new ProfileUpdateRequest
            {
                gender = "MALE",
                activityLevel = "ACTIVE",
                preferences = new ProfileUpdatePreferences
                {
                    dietaryPreference = "VEGAN",
                    shoppingResponsibility = "PRIMARY"
                }
            };

            string json = req.ToJson();

            Assert.IsTrue(json.Contains("MALE"));
            Assert.IsTrue(json.Contains("ACTIVE"));
            Assert.IsTrue(json.Contains("VEGAN"));
            Assert.IsTrue(json.Contains("PRIMARY"));
            Assert.IsTrue(json.Contains("preferences"));
        }

        [Test]
        public void ProfileUpdateRequest_ToJson_SkipsNullNestedObject()
        {
            var req = new ProfileUpdateRequest
            {
                gender = "FEMALE"
            };

            string json = req.ToJson();

            Assert.IsTrue(json.Contains("FEMALE"));
            Assert.IsFalse(json.Contains("preferences"));
            Assert.IsFalse(json.Contains("settings"));
        }
    }

    [TestFixture]
    public class PantryModelTests
    {
        [Test]
        public void PantryItem_ExpiryDateTime_WithValidDate_Parses()
        {
            var item = new PantryItem { expiryDate = "2027-02-02T00:00:00.000Z" };

            Assert.IsNotNull(item.ExpiryDateTime);
            Assert.AreEqual(2027, item.ExpiryDateTime.Value.Year);
            Assert.AreEqual(2, item.ExpiryDateTime.Value.Month);
            Assert.AreEqual(2, item.ExpiryDateTime.Value.Day);
        }

        [Test]
        public void PantryItem_ExpiryDateTime_WithEmptyDate_ReturnsNull()
        {
            var item = new PantryItem { expiryDate = "" };

            Assert.IsNull(item.ExpiryDateTime);
        }

        [Test]
        public void PantryItem_ExpiryDateTime_WithInvalidDate_ReturnsNull()
        {
            var item = new PantryItem { expiryDate = "not-a-date" };

            Assert.IsNull(item.ExpiryDateTime);
        }

        [Test]
        public void PantryItem_ExpiryDateTime_WithNullDate_ReturnsNull()
        {
            var item = new PantryItem();

            Assert.IsNull(item.ExpiryDateTime);
        }
    }
}
