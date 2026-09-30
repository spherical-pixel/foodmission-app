using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    public class UnitCatalog : IUnitCatalog
    {
        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "g", UnitCodes.G }, { "gr", UnitCodes.G }, { "gram", UnitCodes.G }, { "grams", UnitCodes.G },
            { "kg", UnitCodes.Kg }, { "kgs", UnitCodes.Kg }, { "kilogram", UnitCodes.Kg }, { "kilograms", UnitCodes.Kg },
            { "ml", UnitCodes.Ml }, { "milliliter", UnitCodes.Ml }, { "milliliters", UnitCodes.Ml },
            { "l", UnitCodes.L }, { "liter", UnitCodes.L }, { "liters", UnitCodes.L }, { "litre", UnitCodes.L }, { "litres", UnitCodes.L },
            { "cup", UnitCodes.Cups }, { "cups", UnitCodes.Cups },
            { "piece", UnitCodes.Pieces }, { "pieces", UnitCodes.Pieces }, { "pcs", UnitCodes.Pieces }, { "unit", UnitCodes.Pieces }, { "units", UnitCodes.Pieces }
        };

        private static readonly Regex MeasureRegex = new(@"^(\d*[.,]?\d+)\s*(.*)$", RegexOptions.Compiled);

        private static IUnitCatalog s_fallback;

        /// <summary>
        /// Registered instance, or a process-wide fallback with the base codes (static components, EditMode code paths).
        /// </summary>
        public static IUnitCatalog Current =>
            App.current?.services?.GetService<IUnitCatalog>() ?? (s_fallback ??= new UnitCatalog(null));

        private readonly ICatalogService _catalogService;
        private List<string> _codes = new(UnitCodes.All);
        private List<string> _labels = new(UnitCodes.All);
        private string _loadedLang;

        public UnitCatalog(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public IReadOnlyList<string> Codes => _codes;
        public IReadOnlyList<string> Labels => _labels;

        public string GetLabel(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "";
            }

            int idx = _codes.FindIndex(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
            if (idx < 0 || idx >= _labels.Count)
            {
                return code;
            }

            // Without a real catalog label (fallback lists) keep the caller's text as is.
            string label = _labels[idx];
            bool hasRealLabel = !string.IsNullOrEmpty(label) && !string.Equals(label, _codes[idx], StringComparison.Ordinal);
            return hasRealLabel ? label : code;
        }

        public string ResolveCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string text = raw.Trim();

            int idx = _codes.FindIndex(c => string.Equals(c, text, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                return _codes[idx];
            }

            idx = _labels.FindIndex(l => string.Equals(l, text, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && idx < _codes.Count)
            {
                return _codes[idx];
            }

            return Aliases.TryGetValue(text, out string alias) ? alias : null;
        }

        public bool TryParseMeasure(string measure, out float quantity, out string unit)
        {
            bool parsed = TryParseMeasureCore(measure, out quantity, out string resolved);
            unit = resolved ?? UnitCodes.Default;
            return parsed;
        }

        public (float Quantity, string Unit) ResolveMeasure(string measure, float? storedQuantity, string storedUnit)
        {
            if (storedQuantity.HasValue && !string.IsNullOrEmpty(storedUnit))
            {
                return (storedQuantity.Value, storedUnit);
            }

            string fallbackUnit = string.IsNullOrEmpty(storedUnit) ? UnitCodes.Default : storedUnit;
            if (TryParseMeasureCore(measure, out float quantity, out string resolved))
            {
                return (quantity, resolved ?? fallbackUnit);
            }
            return (storedQuantity ?? 1f, fallbackUnit);
        }

        /// <summary>Parses the leading number; <paramref name="unit"/> is null when the unit text is not recognised.</summary>
        private bool TryParseMeasureCore(string measure, out float quantity, out string unit)
        {
            quantity = 1f;
            unit = null;

            if (string.IsNullOrWhiteSpace(measure))
            {
                return false;
            }

            Match match = MeasureRegex.Match(measure.Trim());
            if (!match.Success)
            {
                return false;
            }

            if (!float.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return false;
            }

            quantity = parsed;
            unit = ResolveCode(match.Groups[2].Value);
            return true;
        }

        public async Task LoadAsync(string lang)
        {
            if (_catalogService == null || (_loadedLang != null && _loadedLang == lang))
            {
                return;
            }

            try
            {
                var (units, error) = await _catalogService.GetUnitsAsync(lang);
                if (error != null || units == null || units.Length == 0)
                {
                    Debug.LogWarning($"[UnitCatalog] Units not available (lang={lang}); keeping current list.");
                    return;
                }

                List<string> codes = new(units.Length);
                List<string> labels = new(units.Length);
                foreach (CatalogItem unit in units)
                {
                    if (string.IsNullOrEmpty(unit?.code))
                    {
                        continue;
                    }
                    codes.Add(unit.code);
                    labels.Add(string.IsNullOrEmpty(unit.label) ? unit.code : unit.label);
                }

                if (codes.Count == 0)
                {
                    return;
                }

                _codes = codes;
                _labels = labels;
                _loadedLang = lang;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnitCatalog] LoadAsync failed: {ex.Message}");
            }
        }
    }
}
