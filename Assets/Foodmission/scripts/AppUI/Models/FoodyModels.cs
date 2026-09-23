using System;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class FoodyItemType
    {
        public const string Antennas = "ANTENNAS";
        public const string Ears = "EARS";
        public const string Glasses = "GLASSES";

        public static string Normalize(string type)
        {
            if (string.IsNullOrEmpty(type)) return string.Empty;
            string upper = type.Trim().ToUpperInvariant();
            return upper;
        }
    }

    [Serializable]
    public class FoodyItem
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("code")]
        public string code;

        [JsonProperty("type")]
        public string type;

        [JsonProperty("slot")]
        public int slot;

        [JsonProperty("cost")]
        public int cost;

        [JsonProperty("locked")]
        public bool locked;

        [JsonProperty("owned")]
        public bool owned;

        [JsonProperty("equipped")]
        public bool equipped;

        [JsonProperty("available")]
        public bool available;
    }

    [Serializable]
    public class FoodyLoadout
    {
        [JsonProperty("antennas")]
        public FoodyItem antennas;

        [JsonProperty("ears")]
        public FoodyItem ears;

        [JsonProperty("glasses")]
        public FoodyItem glasses;

        public FoodyItem GetItemByType(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            return FoodyItemType.Normalize(type) switch
            {
                FoodyItemType.Antennas => antennas,
                FoodyItemType.Ears => ears,
                FoodyItemType.Glasses => glasses,
                _ => null
            };
        }

        public void SetItemByType(string type, FoodyItem item)
        {
            if (string.IsNullOrEmpty(type)) return;
            switch (FoodyItemType.Normalize(type))
            {
                case FoodyItemType.Antennas:
                    antennas = item;
                    break;
                case FoodyItemType.Ears:
                    ears = item;
                    break;
                case FoodyItemType.Glasses:
                    glasses = item;
                    break;
            }
        }
    }

    [Serializable]
    public class FoodyPurchaseResponse
    {
        [JsonProperty("item")]
        public FoodyItem item;

        [JsonProperty("pricePaid")]
        public int pricePaid;

        [JsonProperty("pointsBalance")]
        public int pointsBalance;
    }
}
