using System;
using System.Globalization;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Pure text formatting for food waste records. Callers resolve localized strings and unit labels.
    /// </summary>
    public static class FoodWasteFormatter
    {
        public static string GetDisplayName(FoodWaste waste, string unknown)
        {
            if (!string.IsNullOrEmpty(waste?.foodProduct?.name))
            {
                return waste.foodProduct.name;
            }
            if (!string.IsNullOrEmpty(waste?.genericFood?.foodName))
            {
                return waste.genericFood.foodName;
            }
            return unknown;
        }

        public static string GetReasonKey(string wasteReason)
        {
            return wasteReason switch
            {
                WasteReason.Expired => "REASON_EXPIRED",
                WasteReason.Spoiled => "REASON_SPOILED",
                WasteReason.Overcooked => "REASON_OVERCOOKED",
                WasteReason.Unwanted => "REASON_UNWANTED",
                WasteReason.PortionTooLarge => "REASON_PORTION_LARGE",
                WasteReason.Other => "REASON_OTHER",
                _ => null
            };
        }

        public static string FormatQuantity(float quantity, string unitLabel)
        {
            string qty = quantity.ToString("0.##", CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(unitLabel) ? qty : $"{qty} {unitLabel}";
        }

        public static string FormatDetail(float quantity, string unitLabel, string reasonText)
        {
            string qty = FormatQuantity(quantity, unitLabel);
            return string.IsNullOrEmpty(reasonText) ? qty : $"{qty} · {reasonText}";
        }

        public static string FormatMonth(string monthKey, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(monthKey))
            {
                return null;
            }
            if (!DateTime.TryParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime month))
            {
                return null;
            }

            CultureInfo c = culture ?? CultureInfo.InvariantCulture;
            string text = month.ToString("MMMM yyyy", c);
            return text.Length > 0 ? char.ToUpper(text[0], c) + text.Substring(1) : text;
        }

        public static string FormatShortDate(string isoDate, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(isoDate))
            {
                return "";
            }
            // Instants are shown in the device's local time.
            if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime date))
            {
                return "";
            }
            return date.ToString("d MMM", culture ?? CultureInfo.InvariantCulture);
        }
    }
}
