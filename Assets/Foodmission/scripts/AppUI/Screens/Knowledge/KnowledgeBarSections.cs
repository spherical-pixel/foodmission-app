using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    public sealed class KnowledgeBarRow
    {
        public string ContentType;
        public string Code;
        public string Title;
        public bool Finished;
        public bool CanOpen;
    }

    public sealed class KnowledgeBarSection
    {
        public string ContentType;
        /// <summary>UI table key for the section header.</summary>
        public string TitleKey;
        public int Finished;
        public int Total;
        public IReadOnlyList<KnowledgeBarRow> Rows = Array.Empty<KnowledgeBarRow>();
    }

    /// <summary>Groups a knowledge bar's items by content type for the detail screen (pure, no UI).</summary>
    public static class KnowledgeBarSections
    {
        public static readonly string[] Order =
        {
            QuestContentType.Mission, QuestContentType.Challenge, QuestContentType.Quiz, QuestContentType.FoodFact,
        };

        private static readonly Dictionary<string, string> TitleKeys = new Dictionary<string, string>
        {
            { QuestContentType.Mission, "MISSIONS" },
            { QuestContentType.Challenge, "KNOWLEDGE_SECTION_CHALLENGES" },
            { QuestContentType.Quiz, "NAV_QUIZZES" },
            { QuestContentType.FoodFact, "NAV_FOOD_FACTS" },
        };

        private static readonly Dictionary<string, string> Icons = new Dictionary<string, string>
        {
            { QuestContentType.Mission, "🎯" },
            { QuestContentType.Challenge, "🏆" },
            { QuestContentType.Quiz, "❓" },
            { QuestContentType.FoodFact, "💡" },
        };

        public static string IconFor(string contentType)
        {
            return contentType != null && Icons.TryGetValue(contentType, out string icon) ? icon : "";
        }

        public static IReadOnlyList<KnowledgeBarSection> Build(KnowledgeItem[] items)
        {
            var result = new List<KnowledgeBarSection>();
            if (items == null)
            {
                return result;
            }

            foreach (string type in Order)
            {
                List<KnowledgeItem> ofType = items.Where(i => i != null && i.contentType == type).ToList();
                if (ofType.Count == 0)
                {
                    continue;
                }

                bool canOpen = type == QuestContentType.Quiz || type == QuestContentType.FoodFact;
                // Open items first so what's left to do is on top; backend order kept inside each group.
                List<KnowledgeBarRow> rows = ofType.Where(i => !i.finished).Concat(ofType.Where(i => i.finished))
                    .Select(i => new KnowledgeBarRow
                    {
                        ContentType = type,
                        Code = i.contentCode ?? "",
                        Title = string.IsNullOrEmpty(i.title) ? i.contentCode ?? "" : i.title,
                        Finished = i.finished,
                        CanOpen = canOpen && !string.IsNullOrEmpty(i.contentCode),
                    })
                    .ToList();

                result.Add(new KnowledgeBarSection
                {
                    ContentType = type,
                    TitleKey = TitleKeys[type],
                    Finished = rows.Count(r => r.Finished),
                    Total = rows.Count,
                    Rows = rows,
                });
            }
            return result;
        }

        /// <summary>Section to open on entry: the first with open items, else the first; -1 when there are none.</summary>
        public static int FirstExpandedIndex(IReadOnlyList<KnowledgeBarSection> sections)
        {
            if (sections == null || sections.Count == 0)
            {
                return -1;
            }
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].Finished < sections[i].Total)
                {
                    return i;
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// Which sections the user left open per bar, for this app session. The detail screen gets a new instance on
    /// every enter (e.g. back from a quiz), so without this the user's open section would collapse each time.
    /// </summary>
    public static class KnowledgeBarExpansion
    {
        private static readonly Dictionary<string, HashSet<string>> s_ExpandedByKind = new Dictionary<string, HashSet<string>>();

        public static void Remember(string kind, string contentType, bool expanded)
        {
            if (kind == null || contentType == null)
            {
                return;
            }
            if (!s_ExpandedByKind.TryGetValue(kind, out HashSet<string> expandedTypes))
            {
                expandedTypes = new HashSet<string>();
                s_ExpandedByKind[kind] = expandedTypes;
            }
            if (expanded)
            {
                expandedTypes.Add(contentType);
            }
            else
            {
                expandedTypes.Remove(contentType);
            }
        }

        /// <summary>Sections to expand: the remembered ones, or the first with open items when nothing was remembered for the kind.</summary>
        public static IEnumerable<int> ExpandedIndices(string kind, IReadOnlyList<KnowledgeBarSection> sections)
        {
            if (kind != null && s_ExpandedByKind.TryGetValue(kind, out HashSet<string> expandedTypes))
            {
                var indices = new List<int>();
                for (int i = 0; i < sections.Count; i++)
                {
                    if (expandedTypes.Contains(sections[i].ContentType))
                    {
                        indices.Add(i);
                    }
                }
                return indices;
            }
            int first = KnowledgeBarSections.FirstExpandedIndex(sections);
            return first >= 0 ? new[] { first } : Array.Empty<int>();
        }

        public static void Clear()
        {
            s_ExpandedByKind.Clear();
        }
    }

    /// <summary>Bar name/emoji shared by Home and the detail screen (same keys as the Home status bars).</summary>
    public static class KnowledgeBarText
    {
        public static string NameKey(string kind)
        {
            switch (kind)
            {
                case KnowledgeKind.Health:
                    return "T_B_HEALTH";
                case KnowledgeKind.FoodChoices:
                    return "T_B_FOOD_CHOICES";
                case KnowledgeKind.FoodWaste:
                    return "T_B_FOOD_AND_WASTE";
                default:
                    return null;
            }
        }

        public static string Emoji(string kind)
        {
            switch (kind)
            {
                case KnowledgeKind.Health:
                    return "❤️";
                case KnowledgeKind.FoodChoices:
                    return "🛒";
                case KnowledgeKind.FoodWaste:
                    return "♻️";
                default:
                    return "";
            }
        }
    }
}
