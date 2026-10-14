using System;
using System.Globalization;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class Pantry
    {
        public string id;       // pantryId — required for all item operations
        public string userId;
        public PantryItem[] items;  // embedded in GET /api/v1/pantry response
    }

    [Serializable]
    public class PantryItem
    {
        public string id;
        public string pantryId;
        public string foodProductId;       // non-empty if added via OpenFoodFacts product
        public string genericFoodId;       // non-empty if added via NEVO food category
        public float quantity;
        public string unit;             // PIECES, G, KG, ML, L, CUPS
        public string notes;
        public string location;
        public string expiryDate;       // ISO date string: "2026-05-30"
        public string expiryDateSource; // "manual" | "auto_foodkeeper" — populated by backend

        public DateTime? ExpiryDateTime
        {
            get
            {
                if (TryParseExpiryDate(expiryDate, out DateTime result)) return result;
                return null;
            }
        }

        /// <summary>
        /// Calendar day an item expires. Dates the user picks are stored as midnight UTC ("2026-05-30T00:00:00.000Z") and
        /// mean that day everywhere, so they aren't shifted to local time (west of UTC it became the day before);
        /// computed dates (purchase + shelf life) carry a time and are read in local time.
        /// </summary>
        public static bool TryParseExpiryDate(string value, out DateTime date)
        {
            date = default;
            if (string.IsNullOrEmpty(value) ||
                !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
            {
                return false;
            }

            date = utc.TimeOfDay == TimeSpan.Zero
                ? DateTime.SpecifyKind(utc.Date, DateTimeKind.Unspecified)
                : DateTime.SpecifyKind(utc.ToLocalTime().Date, DateTimeKind.Unspecified);
            return true;
        }

        public FoodProduct foodProduct;     // populated by backend $include
        public GenericFood genericFood;     // populated by backend $include
    }

    // JsonUtility cannot deserialize top-level JSON arrays
    [Serializable]
    public class PantryItemArrayWrapper
    {
        public PantryItem[] items;
    }

    [Serializable]
    public class PantryItemListResponse
    {
        public PantryItem[] data;
    }

    [Serializable]
    public class ExpiredPantryItemArrayWrapper
    {
        public ExpiredPantryItem[] items;
    }

    // View-only enriched model for UI — not serialized, not from API
    public class PantryItemView
    {
        public PantryItem Item;
        public string DisplayName;
        public string ImageUrl;
    }
}
