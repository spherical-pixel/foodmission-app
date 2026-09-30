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
    }
}
