using System;
using System.Collections.Generic;
using System.Globalization;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Day labels with weekday, day and month ("Monday, November 2", "lunes 2 de noviembre").
    /// Patterns are fixed per app language: Mono's culture data is unreliable (nb MonthDayPattern gives "2.november.",
    /// sl has no genitive month names).
    /// </summary>
    public static class DayFormat
    {
        private static readonly Dictionary<string, (string Long, string Short)> s_Patterns = new Dictionary<string, (string, string)>
        {
            { "en", ("dddd, MMMM d", "ddd, MMM d") },
            { "es", ("dddd d 'de' MMMM", "ddd d MMM") },
            { "de", ("dddd, d. MMMM", "ddd, d. MMM") },
            { "no", ("dddd d. MMMM", "ddd d. MMM") },
            { "nl", ("dddd d MMMM", "ddd d MMM") },
            { "it", ("dddd d MMMM", "ddd d MMM") },
            { "el", ("dddd d MMMM", "ddd d MMM") },
            { "pl", ("dddd, d MMMM", "ddd d MMM") },
            { "sl", ("dddd, d. MMMM", "ddd d. MMM") }
        };

        private static readonly string[] s_SlovenianGenitiveMonths =
        {
            "januarja", "februarja", "marca", "aprila", "maja", "junija",
            "julija", "avgusta", "septembra", "oktobra", "novembra", "decembra", ""
        };

        /// <summary>Weekday, day and month for sentences and list rows.</summary>
        public static string Long(DateTime day, IFormatProvider provider)
        {
            CultureInfo culture = ToCulture(provider);
            return day.ToString(PatternsFor(culture).Long, FormatInfoFor(culture));
        }

        /// <summary>Abbreviated weekday, day and month for chips.</summary>
        public static string Short(DateTime day, IFormatProvider provider)
        {
            CultureInfo culture = ToCulture(provider);
            return day.ToString(PatternsFor(culture).Short, FormatInfoFor(culture));
        }

        /// <summary>First letter upper-cased, for labels that start a line ("lunes" → "Lunes").</summary>
        public static string Capitalize(string text, IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return char.ToUpper(text[0], ToCulture(provider)) + text.Substring(1);
        }

        private static CultureInfo ToCulture(IFormatProvider provider) => provider as CultureInfo ?? CultureInfo.CurrentCulture;

        private static string LanguageOf(CultureInfo culture)
        {
            string language = culture.TwoLetterISOLanguageName;
            return language == "nb" || language == "nn" ? "no" : language;
        }

        private static (string Long, string Short) PatternsFor(CultureInfo culture) =>
            s_Patterns.TryGetValue(LanguageOf(culture), out var patterns) ? patterns : s_Patterns["en"];

        private static DateTimeFormatInfo FormatInfoFor(CultureInfo culture)
        {
            if (LanguageOf(culture) != "sl")
            {
                return culture.DateTimeFormat;
            }
            var info = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
            info.MonthGenitiveNames = s_SlovenianGenitiveMonths;
            return info;
        }
    }
}
