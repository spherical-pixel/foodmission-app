using System;
using System.Linq;
using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// "Contact support" mail link. The subject carries app version and platform so support knows the build.
    /// </summary>
    public static class SupportContact
    {
        public const string Email = "feedback@foodmission.eu";

        public static string BuildMailtoUrl(string subject, string appVersion, string platform)
        {
            string fullSubject = string.IsNullOrWhiteSpace(subject) ? "FOODMISSION" : subject.Trim();
            string details = string.Join(", ", new[] { appVersion, platform }
                .Where(p => !string.IsNullOrWhiteSpace(p)));

            if (details.Length > 0)
            {
                fullSubject += " (" + details + ")";
            }

            // Obtenemos el tipo de conexión actual automáticamente
            string networkType = Application.internetReachability switch
            {
                NetworkReachability.ReachableViaLocalAreaNetwork => "Wi-Fi",
                NetworkReachability.ReachableViaCarrierDataNetwork => "Mobile Data",
                _ => "Unknown"
            };

            string body =
                "Hello Foodmission Team,\n\n" +
                "1. Contact Information:\n" +
                "- Return Email (if different from sender): \n\n" +
                "2. System & Environment Information:\n" +
                $"- OS & Version: {SystemInfo.operatingSystem}\n" +
                $"- Device Model: {SystemInfo.deviceModel}\n" +
                $"- App Version & Build Number: {appVersion}\n" +
                $"- Network Connection: {networkType}\n\n" +
                "3. Report Details:\n" +
                "- Type of Report (Bug / Crash / UI Issue / Feature Request / Feedback): \n" +
                "- Severity (Low / Medium / High): \n" +
                "- Description: \n\n" +
                "4. Steps to Reproduce:\n" +
                "1. \n" +
                "2. \n" +
                "3. \n\n" +
                "- Expected Behavior: \n" +
                "- Actual Behavior: \n\n" +
                "5. Attachments:\n" +
                "(Please attach screenshots or video recordings if applicable)\n\n" +
                "Thank you!";

            return $"mailto:{Email}?subject={Uri.EscapeDataString(fullSubject)}&body={Uri.EscapeDataString(body)}";
        }
    }
}
