using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Mission prompts are a question followed by an explanation ("How many…? Look for…").
    /// Short places, like the check-in summary, show only the question.
    /// </summary>
    public static class MissionPromptText
    {
        // Greek writes the question mark as ';' (or U+037E)
        private static readonly char[] s_QuestionMarks = { '?', ';', ';', '？' };

        /// <summary>The text up to and including its first question mark; the whole text when it has none.</summary>
        public static string Question(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return prompt;
            }
            int end = prompt.IndexOfAny(s_QuestionMarks);
            return end < 0 ? prompt : prompt.Substring(0, end + 1);
        }
    }
}
