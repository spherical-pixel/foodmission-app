namespace eu.foodmission.platform
{
    /// <summary>Binds the user's levels and the dimension catalogue for one list rebuild.</summary>
    public sealed class LevelGate
    {
        private readonly AppState _state;
        private readonly IDimensionService _dimensions;

        public LevelGate(AppState state, IDimensionService dimensions)
        {
            _state = state;
            _dimensions = dimensions;
        }

        public Dimension DimensionById(string dimensionId)
        {
            return string.IsNullOrEmpty(dimensionId) ? null : _dimensions?.GetDimension(dimensionId);
        }

        public Dimension DimensionForTopic(string topicId)
        {
            return string.IsNullOrEmpty(topicId) ? null : _dimensions?.GetDimensionForTopic(topicId);
        }

        /// <summary>The user's level in the dimension, or null when the dimension is unknown (fail open).</summary>
        public string UserLevel(Dimension dimension)
        {
            if (dimension == null || string.IsNullOrEmpty(dimension.code))
            {
                return null;
            }
            return DimensionLevels.GetLevel(_state, dimension.code);
        }

        /// <summary>Null when the item can be opened.</summary>
        public LevelLock GetLock(Dimension dimension, string itemLevel, bool started)
        {
            if (DevUnlocks.All)
            {
                return null;
            }

            string userLevel = UserLevel(dimension);
            if (!LevelAccess.IsLockedByLevel(itemLevel, userLevel, started))
            {
                return null;
            }

            return new LevelLock
            {
                ItemLevel = ContentLevel.Normalize(itemLevel),
                UserLevel = userLevel,
                DimensionCode = dimension.code,
                DimensionName = dimension.name
            };
        }
    }
}
