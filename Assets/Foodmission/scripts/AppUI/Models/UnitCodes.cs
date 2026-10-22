using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Unit codes accepted by the backend (Prisma enum <c>Unit</c>). Labels come from <see cref="IUnitCatalog"/>.
    /// </summary>
    public static class UnitCodes
    {
        public const string Pieces = "PIECES";
        public const string G = "G";
        public const string Kg = "KG";
        public const string Ml = "ML";
        public const string L = "L";
        public const string Cups = "CUPS";

        public const string Default = Pieces;

        public static readonly string[] All = { Pieces, G, Kg, Ml, L, Cups };

        /// <summary>
        /// Converts between units of the same kind (G↔KG, ML↔L; same code → same value).
        /// Returns false for units that can't be compared (pieces vs grams, cups vs millilitres…).
        /// </summary>
        public static bool TryConvert(float quantity, string from, string to, out float result)
        {
            result = 0f;
            string f = string.IsNullOrEmpty(from) ? Default : from;
            string t = string.IsNullOrEmpty(to) ? Default : to;
            if (f == t)
            {
                result = quantity;
                return true;
            }
            if (!TryGetBase(f, out string fBase, out float fFactor) || !TryGetBase(t, out string tBase, out float tFactor) || fBase != tBase)
            {
                return false;
            }
            result = quantity * fFactor / tFactor;
            return true;
        }

        /// <summary>
        /// Shopping list items only take whole quantities ≥ 1 (backend <c>@IsInt() @Min(1)</c>): fractional KG/L
        /// become G/ML so no amount is lost, anything else is rounded up.
        /// </summary>
        public static int ToWholeQuantity(float quantity, string unit, out string wholeUnit)
        {
            wholeUnit = string.IsNullOrEmpty(unit) ? Default : unit;
            bool fractional = Math.Abs(quantity - (float)Math.Round(quantity)) > 0.001f;
            if (fractional && (wholeUnit == Kg || wholeUnit == L))
            {
                quantity *= 1000f;
                wholeUnit = wholeUnit == Kg ? G : Ml;
            }
            // Tolerance so float noise (250.00002) doesn't round up to the next unit
            return Math.Max(1, (int)Math.Ceiling(quantity - 0.001f));
        }

        private static bool TryGetBase(string code, out string baseCode, out float factor)
        {
            switch (code)
            {
                case G: baseCode = G; factor = 1f; return true;
                case Kg: baseCode = G; factor = 1000f; return true;
                case Ml: baseCode = Ml; factor = 1f; return true;
                case L: baseCode = Ml; factor = 1000f; return true;
                default: baseCode = null; factor = 0f; return false;
            }
        }
    }
}
