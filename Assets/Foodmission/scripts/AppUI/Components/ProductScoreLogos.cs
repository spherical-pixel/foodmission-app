using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    public enum ProductScore { NutriScore, Nova, GreenScore }

    /// <summary>
    /// Official score logos (Open Food Facts icons in graphics/scores, classes in Foodmission_Global_Styles.uss) and the
    /// Nutri explanation for each score. A value is a normalized key: "a".."e" (Nutri-Score), "1".."4" (NOVA),
    /// "a-plus", "a".."f" (Green-Score, formerly Eco-Score), or "unknown" / "not-applicable".
    /// </summary>
    public static class ProductScoreLogos
    {
        public const string Unknown = "unknown";
        public const string NotApplicable = "not-applicable";
        public const string LogoClass = "fm-score-logo";

        public static string NutriScoreValue(string grade)
        {
            string g = Normalize(grade);
            return g.Length == 1 && g[0] >= 'a' && g[0] <= 'e' ? g : SpecialValue(g);
        }

        public static string NovaValue(int group) => group >= 1 && group <= 4 ? group.ToString() : Unknown;

        public static string GreenScoreValue(string grade)
        {
            string g = Normalize(grade);
            if (g == "a-plus" || g == "a+")
            {
                return "a-plus";
            }
            return g.Length == 1 && g[0] >= 'a' && g[0] <= 'f' ? g : SpecialValue(g);
        }

        /// <summary>USS class with the logo for the value, e.g. fm-score-logo--green-score-a-plus.</summary>
        public static string ClassFor(ProductScore score, string value)
        {
            return score switch
            {
                ProductScore.NutriScore => $"{LogoClass}--nutriscore-{value}",
                ProductScore.Nova => $"{LogoClass}--nova-{value}",
                _ => $"{LogoClass}--green-score-{value}"
            };
        }

        /// <summary>Score name and grade as on the logo ("Nutri-Score C", "NOVA 3", "Green-Score A+"), or null without a grade.</summary>
        public static string DisplayName(ProductScore score, string value)
        {
            if (!HasGrade(value))
            {
                return null;
            }
            string grade = value == "a-plus" ? "A+" : value.ToUpperInvariant();
            return score switch
            {
                ProductScore.NutriScore => "Nutri-Score " + grade,
                ProductScore.Nova => "NOVA " + grade,
                _ => "Green-Score " + grade
            };
        }

        public static string InfoKey(ProductScore score)
        {
            return score switch
            {
                ProductScore.NutriScore => "SCORE_INFO_NUTRISCORE",
                ProductScore.Nova => "SCORE_INFO_NOVA",
                _ => "SCORE_INFO_GREEN_SCORE"
            };
        }

        /// <summary>Second paragraph of the explanation: the product's grade, no data, or not applicable.</summary>
        public static string ValueKey(string value)
        {
            if (value == NotApplicable)
            {
                return "SCORE_INFO_NOT_APPLICABLE";
            }
            return HasGrade(value) ? "SCORE_INFO_THIS_PRODUCT" : "SCORE_INFO_UNKNOWN";
        }

        /// <summary>NOVA is a tall logo, Nutri-Score and Green-Score wide ones.</summary>
        public static string SizeClass(ProductScore score) => score == ProductScore.Nova ? $"{LogoClass}--tall" : $"{LogoClass}--wide";

        /// <summary>The logo on its card, styled like the product detail strip (for the Nutri explanation).</summary>
        public static VisualElement CreateLogo(ProductScore score, string value)
        {
            var card = new VisualElement();
            card.AddToClassList("fm-score-card");
            card.AddToClassList("fm-score-card--dialog");
            var logo = new VisualElement();
            logo.AddToClassList(LogoClass);
            logo.AddToClassList(SizeClass(score));
            logo.AddToClassList(ClassFor(score, value));
            card.Add(logo);
            return card;
        }

        public static bool HasGrade(string value) => !string.IsNullOrEmpty(value) && value != Unknown && value != NotApplicable;

        private static string Normalize(string grade) => grade?.Trim().ToLowerInvariant() ?? "";

        private static string SpecialValue(string g) => g == NotApplicable ? NotApplicable : Unknown;
    }
}
