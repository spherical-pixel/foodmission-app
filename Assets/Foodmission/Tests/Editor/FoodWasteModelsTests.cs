using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodWasteModelsTests
    {
        [Test]
        public void FoodWaste_Roundtrips_Via_JsonUtility()
        {
            var waste = new FoodWaste
            {
                id = "waste-1",
                userId = "user-1",
                pantryItemId = "pantry-1",
                foodProductId = "food-1",
                quantity = 2.5f,
                unit = "KG",
                wasteReason = "EXPIRED",
                detectionMethod = "AUTOMATIC",
                notes = "Found expired",
                costEstimate = 5.99f,
                carbonFootprint = 1.25f,
                wastedAt = "2026-05-09T10:00:00"
            };

            string json = JsonUtility.ToJson(waste);
            var result = JsonUtility.FromJson<FoodWaste>(json);

            Assert.AreEqual("waste-1", result.id);
            Assert.AreEqual("EXPIRED", result.wasteReason);
            Assert.AreEqual("AUTOMATIC", result.detectionMethod);
            Assert.AreEqual(2.5f, result.quantity);
            Assert.AreEqual(5.99f, result.costEstimate);
            Assert.AreEqual(1.25f, result.carbonFootprint);
        }

        [Test]
        public void PaginatedFoodWasteResponse_Deserializes()
        {
            string json = "{\"data\":[" +
                "{\"id\":\"w1\",\"userId\":\"u1\",\"pantryItemId\":\"p1\",\"foodProductId\":\"f1\"," +
                "\"quantity\":1.5,\"unit\":\"KG\",\"wasteReason\":\"EXPIRED\",\"detectionMethod\":\"AUTOMATIC\"," +
                "\"wastedAt\":\"2026-05-09T10:00:00\",\"createdAt\":\"\",\"updatedAt\":\"\"}," +
                "{\"id\":\"w2\",\"userId\":\"u1\",\"pantryItemId\":\"p2\",\"foodProductId\":\"f2\"," +
                "\"quantity\":0.5,\"unit\":\"G\",\"wasteReason\":\"SPOILED\",\"detectionMethod\":\"MANUAL\"," +
                "\"wastedAt\":\"2026-05-08T15:00:00\",\"createdAt\":\"\",\"updatedAt\":\"\"}]," +
                "\"total\":2,\"page\":1,\"limit\":20,\"totalPages\":1}";

            var response = JsonUtility.FromJson<PaginatedFoodWasteResponse>(json);

            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.data.Length);
            Assert.AreEqual("EXPIRED", response.data[0].wasteReason);
            Assert.AreEqual("MANUAL", response.data[1].detectionMethod);
            Assert.AreEqual(1.5f, response.data[0].quantity);
            Assert.AreEqual(1, response.totalPages);
        }

        [Test]
        public void BatchWasteResult_Deserializes()
        {
            string json = "{\"successes\":[" +
                "{\"id\":\"w1\",\"userId\":\"u1\",\"pantryItemId\":\"p1\"," +
                "\"quantity\":1,\"unit\":\"KG\",\"wasteReason\":\"EXPIRED\",\"detectionMethod\":\"AUTOMATIC\"," +
                "\"wastedAt\":\"2026-05-09T10:00:00\",\"createdAt\":\"\",\"updatedAt\":\"\"}]," +
                "\"errors\":[{\"pantryItemId\":\"p2\",\"error\":\"Not found\"}]," +
                "\"total\":2,\"successCount\":1,\"errorCount\":1}";

            var result = JsonUtility.FromJson<BatchWasteResult>(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.successes.Length);
            Assert.AreEqual(1, result.errors.Length);
            Assert.AreEqual("Not found", result.errors[0].error);
            Assert.AreEqual(2, result.total);
            Assert.AreEqual(1, result.successCount);
            Assert.AreEqual(1, result.errorCount);
        }

        [Test]
        public void CreateFoodWasteRequest_ToJsonBody_Produces_Valid_Json()
        {
            var request = new CreateFoodWasteRequest
            {
                pantryItemId = "pantry-1",
                quantity = 1.5f,
                wasteReason = "EXPIRED",
                detectionMethod = "MANUAL",
                notes = "Test waste",
                costEstimate = 3.50f,
                wastedAt = "2026-05-09T10:00:00Z"
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"pantryItemId\":\"pantry-1\"", json);
            StringAssert.Contains("\"quantity\":1.5", json);
            StringAssert.Contains("\"wasteReason\":\"EXPIRED\"", json);
            StringAssert.Contains("\"detectionMethod\":\"MANUAL\"", json);
            StringAssert.Contains("\"notes\":\"Test waste\"", json);
        }

        [Test]
        public void BatchWasteRequest_ToJsonBody_Produces_Valid_Json()
        {
            var request = new BatchWasteRequest
            {
                items = new[]
                {
                    new BatchWasteItemRequest
                    {
                        pantryItemId = "p1",
                        quantity = 1.0f,
                        notes = "Expired milk"
                    },
                    new BatchWasteItemRequest
                    {
                        pantryItemId = "p2",
                        quantity = 0.5f
                    }
                }
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"items\":", json);
            StringAssert.Contains("\"pantryItemId\":\"p1\"", json);
            StringAssert.Contains("\"pantryItemId\":\"p2\"", json);
            StringAssert.Contains("\"quantity\":1.0", json);
            StringAssert.Contains("\"notes\":\"Expired milk\"", json);
        }

        [Test]
        public void CreateFoodWasteRequest_NullFields_Are_Omitted()
        {
            var request = new CreateFoodWasteRequest
            {
                pantryItemId = "pantry-1",
                wasteReason = "EXPIRED",
                detectionMethod = "MANUAL"
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"pantryItemId\":\"pantry-1\"", json);
            StringAssert.DoesNotContain("\"notes\"", json);
            StringAssert.DoesNotContain("\"costEstimate\"", json);
        }

        [Test]
        public void ExpiredPantryItem_Deserializes()
        {
            string json = "{\"pantryItemId\":\"p1\",\"foodProductId\":\"f1\",\"quantity\":2.0," +
                "\"unit\":\"KG\",\"expiryDate\":\"2026-05-01T00:00:00\"," +
                "\"suggestedWasteReason\":\"EXPIRED\",\"suggestedDetectionMethod\":\"AUTOMATIC\"}";

            var item = JsonUtility.FromJson<ExpiredPantryItem>(json);

            Assert.IsNotNull(item);
            Assert.AreEqual("p1", item.pantryItemId);
            Assert.AreEqual(2.0f, item.quantity);
            Assert.AreEqual("EXPIRED", item.suggestedWasteReason);
            Assert.AreEqual("AUTOMATIC", item.suggestedDetectionMethod);
        }
    }
}
