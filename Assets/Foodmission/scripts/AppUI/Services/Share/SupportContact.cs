using System;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// "Contact support" mail link. The subject carries app version and platform so support knows the build.
    /// </summary>
    public static class SupportContact
    {
        public const string Email = "foodmission@devilishgames.com";

        public static string BuildMailtoUrl(string subject, string appVersion, string platform)
        {
            string fullSubject = string.IsNullOrWhiteSpace(subject) ? "FOODMISSION" : subject.Trim();
            string details = string.Join(", ", new[] { appVersion, platform }
                .Where(p => !string.IsNullOrWhiteSpace(p)));
            if (details.Length > 0)
            {
                fullSubject += " (" + details + ")";
            }
            // EscapeDataString encodes spaces as %20; '+' would show up literally in mail clients
            return "mailto:" + Email + "?subject=" + Uri.EscapeDataString(fullSubject);
        }
    }
}
