using System;

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
        public string foodId;           // non-empty if added via OpenFoodFacts product
        public string foodCategoryId;   // non-empty if added via NEVO food category
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
                if (string.IsNullOrEmpty(expiryDate)) return null;
                if (DateTime.TryParse(expiryDate, out DateTime result)) return result;
                return null;
            }
        }

        public FoodItem food;           // populated by backend $include
        public FoodCategory foodCategory; // populated by backend $include
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
