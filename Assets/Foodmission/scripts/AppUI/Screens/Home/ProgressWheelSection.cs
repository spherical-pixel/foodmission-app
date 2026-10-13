using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace eu.foodmission.platform
{
    public enum ProgressWheelSectionState { Hidden, Loading, Grid, AllHidden }

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

        /// <summary>The backend always returns the four wheels (BEGINNER before the survey), so no wheels means not loaded yet.</summary>
        public static ProgressWheelSectionState ResolveState(int total, int visible, bool isLoading)
        {
            if (total > 0)
            {
                return visible > 0 ? ProgressWheelSectionState.Grid : ProgressWheelSectionState.AllHidden;
            }
            return isLoading ? ProgressWheelSectionState.Loading : ProgressWheelSectionState.Hidden;
        }

        /// <summary>The segment the wheels run at (backend `profile`): BEGINNER before the survey, updated after a stage-5 promotion.</summary>
        public static string SegmentOf(IEnumerable<ProgressWheel> wheels)
        {
            return wheels?.FirstOrDefault(w => w != null && !string.IsNullOrEmpty(w.profile))?.profile ?? "";
        }

        public static ProgressWheelSectionModel Build(AppState state, bool isLoading)
        {
            ProgressWheel[] all = state?.progressWheels?.Where(w => w != null).ToArray() ?? new ProgressWheel[0];
            string[] hidden = state?.hiddenProgressWheels ?? new string[0];
            IReadOnlyList<ProgressWheel> visible = ProgressWheelService.GetVisible(all, hidden);

            return new ProgressWheelSectionModel
            {
                State = ResolveState(all.Length, visible.Count, isLoading),
                Visible = visible,
                All = all,
                Hidden = hidden,
                Segment = SegmentOf(all),
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
                  .Append(w.sustainabilityTargetPercent.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                  .Append(w.label).Append('|').Append(w.unit).Append('|').Append(w.stageTitle).Append(';');
            }
            sb.Append('#').Append(string.Join(",", state.hiddenProgressWheels ?? new string[0]));
            sb.Append('#').Append(state.lang).Append('#').Append(state.scale);
            return sb.ToString();
        }

        /// <summary>Wheels that get a toggle in the Personalizar dialog: one per non-empty kind, backend order.</summary>
        public static IReadOnlyList<ProgressWheel> CustomizableWheels(IEnumerable<ProgressWheel> wheels)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<ProgressWheel>();
            foreach (ProgressWheel wheel in wheels ?? Array.Empty<ProgressWheel>())
            {
                if (wheel != null && !string.IsNullOrEmpty(wheel.kind) && seen.Add(wheel.kind))
                {
                    result.Add(wheel);
                }
            }
            return result;
        }

        public static string LockedKind(IReadOnlyCollection<string> enabledKinds)
        {
            return enabledKinds != null && enabledKinds.Count == 1 ? enabledKinds.First() : null;
        }
    }
}
