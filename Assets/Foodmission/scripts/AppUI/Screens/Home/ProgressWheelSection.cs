using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace eu.foodmission.platform
{
    public enum ProgressWheelSectionState { Hidden, Loading, Grid, AllHidden, SurveyCta }

    public sealed class ProgressWheelSectionModel
    {
        public ProgressWheelSectionState State;
        public IReadOnlyList<ProgressWheel> Visible = Array.Empty<ProgressWheel>();
        public IReadOnlyList<ProgressWheel> All = Array.Empty<ProgressWheel>();
        public IReadOnlyList<string> Hidden = Array.Empty<string>();
        public string Segment = "";
        public bool ForceSingleColumn;
    }

    /// <summary>Pure mapping from AppState to the Home progress wheels section.</summary>
    public static class ProgressWheelSection
    {
        public const string LargeScale = "large";

        public static ProgressWheelSectionState ResolveState(int total, int visible, bool isLoading, bool surveyComplete)
        {
            if (total > 0)
            {
                return visible > 0 ? ProgressWheelSectionState.Grid : ProgressWheelSectionState.AllHidden;
            }
            if (isLoading)
            {
                return ProgressWheelSectionState.Loading;
            }
            return surveyComplete ? ProgressWheelSectionState.Hidden : ProgressWheelSectionState.SurveyCta;
        }

        public static ProgressWheelSectionModel Build(AppState state, bool isLoading)
        {
            ProgressWheel[] all = state?.progressWheels?.Where(w => w != null).ToArray() ?? new ProgressWheel[0];
            string[] hidden = state?.hiddenProgressWheels ?? new string[0];
            IReadOnlyList<ProgressWheel> visible = ProgressWheelService.GetVisible(all, hidden);
            bool surveyComplete = state?.userOnboardingSurvey != null && state.userOnboardingSurvey.IsComplete();

            return new ProgressWheelSectionModel
            {
                State = ResolveState(all.Length, visible.Count, isLoading, surveyComplete),
                Visible = visible,
                All = all,
                Hidden = hidden,
                Segment = state?.userSegment ?? "",
                ForceSingleColumn = string.Equals(state?.scale, LargeScale, StringComparison.Ordinal),
            };
        }

        /// <summary>
        /// Value-equal key of everything the section renders. AppState.Copy clones arrays on every dispatch,
        /// so a reference-based selector would re-render the section for unrelated actions.
        /// </summary>
        public static string Signature(AppState state)
        {
            if (state == null)
            {
                return "";
            }
            var sb = new StringBuilder();
            foreach (ProgressWheel w in state.progressWheels ?? new ProgressWheel[0])
            {
                if (w == null)
                {
                    continue;
                }
                sb.Append(w.kind).Append('|').Append(w.profile).Append('|').Append(w.stage).Append('|')
                  .Append(w.percentComplete.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                  .Append(w.accumulatedValue.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                  .Append(w.targetValue.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                  .Append(w.allTimeTotal.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                  .Append(w.sustainabilityTargetPercent.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }
            sb.Append('#').Append(string.Join(",", state.hiddenProgressWheels ?? new string[0]));
            sb.Append('#').Append(state.userSegment).Append('#').Append(state.lang).Append('#').Append(state.scale);
            sb.Append('#').Append(state.userOnboardingSurvey != null && state.userOnboardingSurvey.IsComplete());
            return sb.ToString();
        }

        public static string LockedKind(IReadOnlyCollection<string> enabledKinds)
        {
            return enabledKinds != null && enabledKinds.Count == 1 ? enabledKinds.First() : null;
        }
    }
}
