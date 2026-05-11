using System;
using System.Text;
using Newtonsoft.Json;

using UnityEngine;

namespace eu.foodmission.platform
{
    public static class WasteReason
    {
        public const string Expired = "EXPIRED";
        public const string Spoiled = "SPOILED";
        public const string Overcooked = "OVERCOOKED";
        public const string Unwanted = "UNWANTED";
        public const string PortionTooLarge = "PORTION_TOO_LARGE";
        public const string Other = "OTHER";

        public static readonly string[] All = { Expired, Spoiled, Overcooked, Unwanted, PortionTooLarge, Other };
    }

    public static class DetectionMethod
    {
        public const string Automatic = "AUTOMATIC";
        public const string Manual = "MANUAL";

        public static readonly string[] All = { Automatic, Manual };
    }

    [Serializable]
    public class FoodWaste
    {
        public string id;
        public string userId;
        public string pantryItemId;
        public string foodId;
        public string foodCategoryId;
        public float quantity;
        public string unit;
        public string wasteReason;
        public string detectionMethod;
        public string notes;
        public float costEstimate;
        public float carbonFootprint;
        public string wastedAt;
        public string createdAt;
        public string updatedAt;
        public FoodItem food;
        public FoodCategory foodCategory;
    }

    [Serializable]
    public class PaginatedFoodWasteResponse
    {
        public FoodWaste[] data;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }

    public class CreateFoodWasteRequest
    {
        [JsonProperty("pantryItemId")]
        public string pantryItemId;

        [JsonProperty("quantity")]
        public float? quantity;

        [JsonProperty("unit")]
        public string unit;

        [JsonProperty("wasteReason")]
        public string wasteReason;

        [JsonProperty("detectionMethod")]
        public string detectionMethod;

        [JsonProperty("notes")]
        public string notes;

        [JsonProperty("costEstimate")]
        public float? costEstimate;

        [JsonProperty("carbonFootprint")]
        public float? carbonFootprint;

        [JsonProperty("wastedAt")]
        public string wastedAt;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    public class UpdateFoodWasteRequest
    {
        [JsonProperty("pantryItemId")]
        public string pantryItemId;

        [JsonProperty("foodId")]
        public string foodId;

        [JsonProperty("foodCategoryId")]
        public string foodCategoryId;

        [JsonProperty("quantity")]
        public float? quantity;

        [JsonProperty("unit")]
        public string unit;

        [JsonProperty("wasteReason")]
        public string wasteReason;

        [JsonProperty("notes")]
        public string notes;

        [JsonProperty("costEstimate")]
        public float? costEstimate;

        [JsonProperty("carbonFootprint")]
        public float? carbonFootprint;

        [JsonProperty("wastedAt")]
        public string wastedAt;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    // ── Batch Waste ────────────────────────────────────────────────────

    public class BatchWasteItemRequest
    {
        [JsonProperty("pantryItemId")]
        public string pantryItemId;

        [JsonProperty("quantity")]
        public float? quantity;

        [JsonProperty("unit")]
        public string unit;

        [JsonProperty("costEstimate")]
        public float? costEstimate;

        [JsonProperty("notes")]
        public string notes;
    }

    public class BatchWasteRequest
    {
        [JsonProperty("items")]
        public BatchWasteItemRequest[] items;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    [Serializable]
    public class BatchWasteResult
    {
        public FoodWaste[] successes;
        public BatchWasteErrorItem[] errors;
        public int total;
        public int successCount;
        public int errorCount;
    }

    [Serializable]
    public class BatchWasteErrorItem
    {
        public string pantryItemId;
        public string error;
    }

    // ── Expired Items ──────────────────────────────────────────────────

    [Serializable]
    public class ExpiredPantryItem
    {
        public string pantryItemId;
        public string foodId;
        public float quantity;
        public string unit;
        public string expiryDate;
        public FoodItem food;
        public string suggestedWasteReason;
        public string suggestedDetectionMethod;
    }

    // ── Statistics ─────────────────────────────────────────────────────

    [Serializable]
    public class FoodWasteStatistics
    {
        public float totalWaste;
        public float totalCost;
        public float totalCarbon;
        public WasteByReason[] wasteByReason;
        public WasteByMethod[] wasteByMethod;
        public MostWastedFood[] mostWastedFoods;
        public string dateFrom;
        public string dateTo;
    }

    [Serializable]
    public class WasteByReason
    {
        public string reason;
        public int count;
    }

    [Serializable]
    public class WasteByMethod
    {
        public string method;
        public int count;
    }

    [Serializable]
    public class MostWastedFood
    {
        public string foodId;
        public string foodName;
        public float totalQuantity;
        public int count;
    }

    // ── Trends ─────────────────────────────────────────────────────────

    [Serializable]
    public class FoodWasteTrends
    {
        public WasteTrendDataPoint[] data;
        public string dateFrom;
        public string dateTo;
        public string interval;
    }

    [Serializable]
    public class WasteTrendDataPoint
    {
        public string date;
        public float totalWaste;
        public float totalCost;
        public float totalCarbon;
        public int count;
    }
}
