namespace eu.foodmission.platform
{
    public static class QuestExtensions
    {
        /// <summary>
        /// Name shown to the user for a quest. The backend's <c>title</c> is generic per dimension and level
        /// (e.g. "Diet changes - Beginner"), so the specific <c>name</c> wins; then title, then code.
        /// </summary>
        public static string GetDisplayName(this Quest quest)
        {
            if (quest == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(quest.name))
            {
                return quest.name;
            }

            if (!string.IsNullOrEmpty(quest.title))
            {
                return quest.title;
            }

            return quest.code ?? string.Empty;
        }
    }
}
