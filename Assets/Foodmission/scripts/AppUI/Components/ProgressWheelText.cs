using System;
using System.Collections.Generic;

using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform.Components
{
    /// <summary>Localization keys and display text for progress wheels. Backend label/stageTitle are English only.</summary>
    public static class ProgressWheelText
    {
        public const int StagesPerProfile = 5;

        private static readonly string[] s_Kinds = { "CO2_REDUCTION", "ENERGY_REDUCTION", "WATER_SAVINGS", "LAND_USE_REDUCTION" };
        private static readonly string[] s_Segments = { "BEGINNER", "INTERMEDIATE", "ADVANCED" };

        private static readonly Dictionary<string, string> s_ColorClasses = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "CO2_REDUCTION", "fm-wheel-card--co2" },
            { "ENERGY_REDUCTION", "fm-wheel-card--energy" },
            { "WATER_SAVINGS", "fm-wheel-card--water" },
            { "LAND_USE_REDUCTION", "fm-wheel-card--land" },
        };

        private static readonly string[] s_SectionKeys =
        {
            "WHEELS_SECTION_TITLE", "WHEELS_SECTION_HEADER", "WHEELS_CUSTOMIZE", "WHEELS_CUSTOMIZE_TITLE",
            "WHEELS_ALL_HIDDEN", "WHEELS_SURVEY_CTA_TEXT", "WHEELS_SURVEY_CTA_BUTTON",
            "WHEEL_PERCENT", "WHEEL_STAGE", "WHEEL_VALUE", "WHEEL_TARGET", "WHEEL_TOTAL",
        };

        public const string DefaultColorClass = "fm-wheel-card--default";

        public static IReadOnlyList<string> KnownKinds => s_Kinds;
        public static IReadOnlyList<string> KnownSegments => s_Segments;

        private static bool IsKnownKind(string kind) => !string.IsNullOrEmpty(kind) && Array.IndexOf(s_Kinds, kind) >= 0;
        private static bool IsKnownSegment(string segment) => !string.IsNullOrEmpty(segment) && Array.IndexOf(s_Segments, segment) >= 0;

        public static string NameKey(string kind) => IsKnownKind(kind) ? $"WHEEL_{kind}" : null;
        public static string UnitKey(string kind) => IsKnownKind(kind) ? $"WHEEL_UNIT_{kind}" : null;
        public static string SegmentKey(string segment) => IsKnownSegment(segment) ? $"SEGMENT_{segment}" : null;

        public static string StageTitleKey(string segment, int stage)
        {
            if (!IsKnownSegment(segment) || stage < 1 || stage > StagesPerProfile)
            {
                return null;
            }
            return $"WHEEL_STAGE_{segment}_{stage}";
        }

        public static string ColorClass(string kind)
        {
            return kind != null && s_ColorClasses.TryGetValue(kind, out string cls) ? cls : DefaultColorClass;
        }

        public static float ClampPercent(float percent)
        {
            if (float.IsNaN(percent))
            {
                return 0f;
            }
            return Math.Max(0f, Math.Min(100f, percent));
        }

        public static IEnumerable<string> AllKeys()
        {
            foreach (string key in s_SectionKeys)
            {
                yield return key;
            }
            foreach (string kind in s_Kinds)
            {
                yield return NameKey(kind);
                yield return UnitKey(kind);
            }
            foreach (string segment in s_Segments)
            {
                yield return SegmentKey(segment);
                for (int stage = 1; stage <= StagesPerProfile; stage++)
                {
                    yield return StageTitleKey(segment, stage);
                }
            }
        }

        public static string Localize(string key, params object[] args)
        {
            return args != null && args.Length > 0
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, args)
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }

        public static string Name(ProgressWheel w)
        {
            string key = NameKey(w?.kind);
            return key != null ? Localize(key) : (w?.label ?? w?.kind ?? "");
        }

        public static string Unit(ProgressWheel w)
        {
            string key = UnitKey(w?.kind);
            return key != null ? Localize(key) : (w?.unit ?? "");
        }

        public static string StageTitle(ProgressWheel w)
        {
            string key = StageTitleKey(w?.profile, w?.stage ?? 0);
            return key != null ? Localize(key) : (w?.stageTitle ?? "");
        }

        public static string StageLine(ProgressWheel w) => Localize("WHEEL_STAGE", w?.stage ?? 0, StagesPerProfile);
        public static string PercentText(ProgressWheel w) => Localize("WHEEL_PERCENT", ClampPercent(w?.percentComplete ?? 0f));
        public static string ValueLine(ProgressWheel w) => Localize("WHEEL_VALUE", w?.accumulatedValue ?? 0f, w?.targetValue ?? 0f, Unit(w));
        public static string TargetLine(ProgressWheel w) => Localize("WHEEL_TARGET", w?.sustainabilityTargetPercent ?? 0f);
        public static string TotalLine(ProgressWheel w) => Localize("WHEEL_TOTAL", w?.allTimeTotal ?? 0f, Unit(w));

        public static string SectionHeader(string segment)
        {
            string key = SegmentKey(segment);
            return key != null ? Localize("WHEELS_SECTION_HEADER", Localize(key)) : Localize("WHEELS_SECTION_TITLE");
        }
    }
}
