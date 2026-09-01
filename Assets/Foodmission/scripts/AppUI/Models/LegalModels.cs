using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class LegalDocType
    {
        public const string TermsOfService = "TERMS_OF_SERVICE";
        public const string PrivacyPolicy = "PRIVACY_POLICY";
    }

    [Serializable]
    public class LegalDocument
    {
        public string key;
        public string docType;
        public string version;
        public string title;
        public string content;
        public string locale;
        public string updatedAt;
    }

    [Serializable]
    public class PendingLegalConsent
    {
        public string docType;
        public string documentKey;
        public string requiredVersion;
        public string locale;
        public bool accepted;
        public string acceptedVersion;
        public string acceptedAt;
    }

    [Serializable]
    public class LegalConsentStatus
    {
        public bool mustAccept;
        public PendingLegalConsent[] documents;
    }

    [Serializable]
    public class AcceptLegalConsentRequest
    {
        public string documentKey;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(json);
        }
    }

    [Serializable]
    public class AcceptLegalConsentResponse
    {
        public bool accepted;
        public string userId;
        public string documentKey;
        public string docType;
        public string version;
        public string locale;
        public string acceptedAt;
    }
}
