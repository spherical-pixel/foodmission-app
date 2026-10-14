using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Pure evaluation of <see cref="ChecklistSpec"/> items, shared by the check-in screen, the builder and the planner.</summary>
    public static class ChecklistRule
    {
        public static bool Counts(ChecklistSpec spec, IEnumerable<int> selected) =>
            spec != null && MissingCount(spec, selected) == 0 && MissingRequired(spec, selected).Count == 0;

        /// <summary>How many more distinct options the item needs.</summary>
        public static int MissingCount(ChecklistSpec spec, IEnumerable<int> selected) =>
            Math.Max(0, spec.MinDistinct - Valid(spec, selected).Count());

        public static IReadOnlyList<int> MissingRequired(ChecklistSpec spec, IEnumerable<int> selected)
        {
            var valid = new HashSet<int>(Valid(spec, selected));
            return spec.Required.Where(r => !valid.Contains(r)).ToList();
        }

        /// <summary>Option indexes whose value (array mode) is one of <paramref name="values"/>, ignoring case.</summary>
        public static HashSet<int> IndexesOf(ChecklistSpec spec, IEnumerable<string> values)
        {
            var wanted = new HashSet<string>((values ?? Array.Empty<string>()).Where(v => v != null).Select(v => v.ToUpperInvariant()));
            var indexes = new HashSet<int>();
            for (int i = 0; i < spec.Options.Count; i++)
            {
                string value = spec.ValueOf(i);
                if (value != null && wanted.Contains(value.ToUpperInvariant()))
                {
                    indexes.Add(i);
                }
            }
            return indexes;
        }

        /// <summary>Values (array mode) of the selected options, in option order.</summary>
        public static string[] ValuesOf(ChecklistSpec spec, IEnumerable<int> selected) =>
            Valid(spec, selected).OrderBy(i => i).Select(spec.ValueOf).Where(v => v != null).ToArray();

        private static IEnumerable<int> Valid(ChecklistSpec spec, IEnumerable<int> selected) =>
            (selected ?? Array.Empty<int>()).Where(i => i >= 0 && i < spec.Options.Count).Distinct();
    }
}
