using System;

namespace eu.foodmission.platform
{
    public enum BarcodeScanOutcome
    {
        Found,
        NotFound,
        NoProductInfo,
        Busy,
        Failed
    }

    public static class BarcodeScan
    {
        // Name the backend stores for OFF entries without any name (empty stubs)
        public const string BackendUnknownProductName = "Unknown Product";

        /// <summary>
        /// ZXing returns an EAN-13 starting with 0 as a 12-digit UPC-A; OFF and the backend key it by the 13 digits.
        /// </summary>
        public static string Normalize(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return barcode;
            }

            string trimmed = barcode.Trim();
            if (trimmed.Length == 12 && IsAllDigits(trimmed))
            {
                return "0" + trimmed;
            }

            return trimmed;
        }

        public static BarcodeScanOutcome Classify(FoodProduct product, ApiErrorResponse error)
        {
            if (error != null)
            {
                int code = error.statusCode;
                if (code == 404)
                {
                    return BarcodeScanOutcome.NotFound;
                }

                // 0 = no response (timeout, offline); OFF answers 429/503 often
                if (code == 0 || code == 408 || code == 429 || code >= 500)
                {
                    return BarcodeScanOutcome.Busy;
                }

                return BarcodeScanOutcome.Failed;
            }

            if (product == null)
            {
                return BarcodeScanOutcome.NotFound;
            }

            if (string.IsNullOrWhiteSpace(product.name)
                || string.Equals(product.name.Trim(), BackendUnknownProductName, StringComparison.OrdinalIgnoreCase))
            {
                return BarcodeScanOutcome.NoProductInfo;
            }

            return BarcodeScanOutcome.Found;
        }

        private static bool IsAllDigits(string value)
        {
            foreach (char c in value)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
