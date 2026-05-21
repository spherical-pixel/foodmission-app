using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class ShoppingList
    {
        public string id;
        public string userId;
        public string title;
    }

    [Serializable]
    public class ShoppingListItem
    {
        public string id;
        public string shoppingListId;
        public string foodProductId;
        public float quantity;
        public string unit;     // PIECES, G, KG, ML, L, CUPS
        public string notes;
        public bool @checked;
        public FoodProduct foodProduct;
    }

    [Serializable]
    public class ShoppingListPagedResponse
    {
        public ShoppingList[] data;
    }

    [Serializable]
    public class ShoppingListItemPagedResponse
    {
        public ShoppingListItem[] data;
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
