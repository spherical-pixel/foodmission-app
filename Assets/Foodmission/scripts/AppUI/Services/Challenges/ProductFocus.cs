using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>What a product challenge asks the user to look at in the product detail.</summary>
    [Flags]
    public enum ProductFocus
    {
        None = 0,
        Sugar = 1,
        Salt = 2,
        Fat = 4,
        Fibre = 8,
        Protein = 16,
        /// <summary>Every nutrient row and macro card.</summary>
        Nutrients = 32,
        Nova = 64,
        /// <summary>Nutri-Score and Green-Score.</summary>
        Scores = 128,
        Ingredients = 256,
        Origin = 512,
        Labels = 1024
    }

    /// <summary>Which product-detail keys (UI.csv keys of rows, plus the badge/section keys below) a focus covers. Pure.</summary>
    public static class ProductFocusMap
    {
        public const string NovaKey = "NOVA";
        public const string NutriScoreKey = "NUTRISCORE";
        public const string EcoScoreKey = "ECOSCORE";
        public const string IngredientsKey = "INGREDIENTS";

        private static readonly (ProductFocus Focus, string[] Keys)[] s_Keys =
        {
            (ProductFocus.Sugar, new[] { "NUTR_SUGARS", "NUTR_ADDED_SUGARS", "TL_SUGARS" }),
            (ProductFocus.Salt, new[] { "NUTR_SALT", "NUTR_SODIUM", "TL_SALT" }),
            (ProductFocus.Fat, new[] { "NUTR_FAT", "NUTR_SATURATED_FAT", "NUTR_MONO_UNSATURATED_FAT", "NUTR_POLY_UNSATURATED_FAT",
                "NUTR_OMEGA3_FAT", "NUTR_OMEGA6_FAT", "NUTR_TRANS_FAT", "TL_FAT", "TL_SATURATED_FAT" }),
            (ProductFocus.Fibre, new[] { "NUTR_FIBER", "NUTR_CARBOHYDRATES" }),
            (ProductFocus.Protein, new[] { "NUTR_PROTEINS", "NUTR_PROTEINS_ANIMAL", "NUTR_PROTEINS_PLANT" }),
            (ProductFocus.Nova, new[] { NovaKey }),
            (ProductFocus.Scores, new[] { NutriScoreKey, EcoScoreKey }),
            (ProductFocus.Ingredients, new[] { IngredientsKey }),
            (ProductFocus.Origin, new[] { "META_ORIGINS", "META_COUNTRIES", "META_MANUFACTURING" }),
            (ProductFocus.Labels, new[] { "META_LABELS", "META_MANUFACTURING" }),
        };

        public static bool Matches(ProductFocus focus, string key)
        {
            if (focus == ProductFocus.None || string.IsNullOrEmpty(key))
            {
                return false;
            }
            if ((focus & ProductFocus.Nutrients) != 0 && key.StartsWith("NUTR_", StringComparison.Ordinal))
            {
                return true;
            }
            foreach ((ProductFocus flag, string[] keys) in s_Keys)
            {
                if ((focus & flag) != 0 && Array.IndexOf(keys, key) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>False when the product shows nothing the focus covers (the card then says to check the packaging).</summary>
        public static bool HasAny(ProductFocus focus, IEnumerable<string> availableKeys)
        {
            if (focus == ProductFocus.None)
            {
                return true;
            }
            return availableKeys != null && availableKeys.Any(k => Matches(focus, k));
        }
    }
}
