using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class ShoppingList
    {
        public string id;
        public string name;
        public string description;
        public string userGroupId;
    }

    [Serializable]
    public class ShoppingListItem
    {
        public string id;
        public string shoppingListId;
        public string foodId;
        public float quantity;
        public string unit;     // PIECES, G, KG, ML, L, CUPS
        public string notes;
        public bool @checked;
    }

    // JsonUtility cannot deserialize top-level JSON arrays.
    // Use: JsonUtility.FromJson<ShoppingListArrayWrapper>("{\"items\":" + json + "}")
    [Serializable]
    public class ShoppingListArrayWrapper
    {
        public ShoppingList[] items;
    }

    [Serializable]
    public class ShoppingListItemArrayWrapper
    {
        public ShoppingListItem[] items;
    }

    // View-only enriched model for UI — not serialized, not from API
    public class ShoppingListItemView
    {
        public ShoppingListItem Item;
        public string FoodName;
        public string FoodImageUrl;
        public string[] FoodBrands;
    }
}
