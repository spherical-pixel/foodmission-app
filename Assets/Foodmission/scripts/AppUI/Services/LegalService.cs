using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class LegalService : ILegalService
    {
        private readonly IStoreService _storeService;

        public LegalService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService?.GetAppState();
                if (s == null || string.IsNullOrEmpty(s.accessToken)) return string.Empty;
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string ResolveLocale(string locale)
        {
            Debug.Log($"[{GetType()}] ResolveLocale - 1 - locale:{locale}");
            if (!string.IsNullOrEmpty(locale))
            {
                Debug.Log($"[{GetType()}] ResolveLocale - 2 - locale:{locale}");
                return locale;
            }

            AppState s = _storeService?.GetAppState();
            if (!string.IsNullOrEmpty(s?.lang) && s.lang != "none")
            {
                Debug.Log($"[{GetType()}] ResolveLocale - 3 - locale:{s.lang}");
                return s.lang;
            }

            Debug.Log($"[{GetType()}] ResolveLocale - 4 - locale:{locale}");
            return "en";
        }

        public async Task<(LegalDocument Result, ApiErrorResponse Error)> GetLatestDocumentAsync(string docType, string locale = null)
        {
            if (string.IsNullOrEmpty(docType))
                return (null, new ApiErrorResponse { message = "Document type is required" });

            string effectiveLocale = ResolveLocale(locale);
            string url = $"{ApiConfig.BaseUrl}/api/v1/legal/documents/latest/{Uri.EscapeDataString(docType)}?locale={Uri.EscapeDataString(effectiveLocale)}";
            Debug.Log($"[{GetType().ToString()}] GetLatestDocumentAsync - sending -> " + url);

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Accept", "application/json");

            string auth = AuthHeader;
            if (!string.IsNullOrEmpty(auth))
            {
                request.SetRequestHeader("Authorization", auth);
            }

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetLatestDocumentAsync {docType}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var doc = JsonConvert.DeserializeObject<LegalDocument>(raw);
                return (doc, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize LegalDocument {docType}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(LegalDocument[] Result, ApiErrorResponse Error)> GetRequiredDocumentsAsync(string locale = null)
        {
            string effectiveLocale = ResolveLocale(locale);
            string url = $"{ApiConfig.BaseUrl}/api/v1/legal/documents/required?locale={Uri.EscapeDataString(effectiveLocale)}";
            Debug.Log($"[{GetType().ToString()}] GetRequiredDocumentsAsync - sending -> " + url);

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Accept", "application/json");

            string auth = AuthHeader;
            if (!string.IsNullOrEmpty(auth))
            {
                request.SetRequestHeader("Authorization", auth);
            }

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetRequiredDocumentsAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var docs = JsonConvert.DeserializeObject<LegalDocument[]>(raw);
                return (docs, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize required LegalDocuments: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(LegalConsentStatus Result, ApiErrorResponse Error)> GetConsentStatusAsync(string locale = null)
        {
            string effectiveLocale = ResolveLocale(locale);
            string url = $"{ApiConfig.BaseUrl}/api/v1/legal/consents/me/status?locale={Uri.EscapeDataString(effectiveLocale)}";
            Debug.Log($"[{GetType().ToString()}] GetConsentStatusAsync - sending -> " + url);

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
            {
                return (null, new ApiErrorResponse { message = "Authentication required" });
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetConsentStatusAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var status = JsonConvert.DeserializeObject<LegalConsentStatus>(raw);
                return (status, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize LegalConsentStatus: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(AcceptLegalConsentResponse Result, ApiErrorResponse Error)> AcceptConsentAsync(string documentKey)
        {
            if (string.IsNullOrEmpty(documentKey))
                return (null, new ApiErrorResponse { message = "Document key is required" });

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
            {
                return (null, new ApiErrorResponse { message = "Authentication required" });
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/legal/consents/me/accept";
            var reqDto = new AcceptLegalConsentRequest { documentKey = documentKey };
            byte[] body = reqDto.ToJsonBody();

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] AcceptConsentAsync {documentKey}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<AcceptLegalConsentResponse>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize AcceptLegalConsentResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
