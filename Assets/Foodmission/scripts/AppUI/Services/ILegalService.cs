using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface ILegalService
    {
        Task<(LegalDocument Result, ApiErrorResponse Error)> GetLatestDocumentAsync(string docType, string locale = null);
        Task<(LegalDocument[] Result, ApiErrorResponse Error)> GetRequiredDocumentsAsync(string locale = null);
        Task<(LegalConsentStatus Result, ApiErrorResponse Error)> GetConsentStatusAsync(string locale = null);
        Task<(AcceptLegalConsentResponse Result, ApiErrorResponse Error)> AcceptConsentAsync(string documentKey);
    }
}
