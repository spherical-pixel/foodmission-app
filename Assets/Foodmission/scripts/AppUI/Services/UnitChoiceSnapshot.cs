using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Fixed copy of the unit codes and labels taken when a unit dropdown is built, so the selected index
    /// always maps to the code that was shown even if <see cref="IUnitCatalog"/> reloads meanwhile.
    /// </summary>
    public sealed class UnitChoiceSnapshot
    {
        private readonly List<string> _codes;
        private readonly List<string> _labels;

        private UnitChoiceSnapshot(IReadOnlyList<string> codes, IReadOnlyList<string> labels)
        {
            _codes = new List<string>(codes);
            _labels = new List<string>(labels);
        }

        public static UnitChoiceSnapshot From(IUnitCatalog catalog)
        {
            return new UnitChoiceSnapshot(catalog.Codes, catalog.Labels);
        }

        /// <summary>Labels in dropdown order (a mutable list, as UI Toolkit dropdowns expect).</summary>
        public List<string> Labels => _labels;

        public int IndexOf(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return -1;
            }
            return _codes.FindIndex(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
        }

        public string CodeAt(int index)
        {
            return index >= 0 && index < _codes.Count ? _codes[index] : UnitCodes.Default;
        }
    }
}
