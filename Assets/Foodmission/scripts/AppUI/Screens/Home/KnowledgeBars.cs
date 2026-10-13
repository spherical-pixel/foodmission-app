using System.Globalization;
using System.Text;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Pure mapping from AppState.knowledgeProgress to the Home status bars.</summary>
    public static class KnowledgeBars
    {
        /// <summary>Bar value 0–1 for a kind; 0 when the kind is missing.</summary>
        public static float Value(AppState state, string kind)
        {
            KnowledgeProgress bar = Find(state, kind);
            return bar == null ? 0f : Mathf.Clamp01(bar.percentComplete / 100f);
        }

        public static KnowledgeProgress Find(AppState state, string kind)
        {
            if (state?.knowledgeProgress == null)
            {
                return null;
            }
            foreach (KnowledgeProgress bar in state.knowledgeProgress)
            {
                if (bar != null && bar.kind == kind)
                {
                    return bar;
                }
            }
            return null;
        }

        /// <summary>Value-equal key; AppState.Copy clones arrays on every dispatch.</summary>
        public static string Signature(AppState state)
        {
            var sb = new StringBuilder();
            foreach (KnowledgeProgress bar in state?.knowledgeProgress ?? new KnowledgeProgress[0])
            {
                if (bar == null)
                {
                    continue;
                }
                sb.Append(bar.kind).Append('|').Append(bar.finishedItems).Append('|').Append(bar.totalItems).Append('|')
                  .Append(bar.percentComplete.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }
            return sb.ToString();
        }
    }
}
