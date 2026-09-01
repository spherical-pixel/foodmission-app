using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class SurveyService : ISurveyService
    {
        private readonly IStoreService _storeService;

        public SurveyService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService.GetAppState();
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string ResolveLang(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
                return lang;

            AppState s = _storeService.GetAppState();
            if (!string.IsNullOrEmpty(s.lang) && s.lang != "none")
                return s.lang;

            return "en";
        }

        public async Task<(SurveyDto[] Result, ApiErrorResponse Error)> GetSurveysAsync(string lang = null)
        {
            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetSurveysAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyDto[]>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyDto[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(SurveyDto Result, ApiErrorResponse Error)> GetSurveyBySlugAsync(string slug, string lang = null)
        {
            if (string.IsNullOrEmpty(slug))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys/by-slug/{Uri.EscapeDataString(slug)}?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetSurveyBySlugAsync({slug})"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyDto>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyDto: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(SurveyDto Result, ApiErrorResponse Error)> GetSurveyByIdAsync(string id, string lang = null)
        {
            if (string.IsNullOrEmpty(id))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys/{Uri.EscapeDataString(id)}?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetSurveyByIdAsync({id})"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyDto>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyDto: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(SurveyResponseDto Result, ApiErrorResponse Error)> SubmitSurveyResponseAsync(string surveyId, SubmitSurveyResponseDto dto)
        {
            if (string.IsNullOrEmpty(surveyId) || dto == null)
                return (null, new ApiErrorResponse { message = "Invalid survey submission parameters" });

            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys/{Uri.EscapeDataString(surveyId)}/responses";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(dto.ToJson());

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SubmitSurveyResponseAsync({surveyId})"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyResponseDto>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyResponseDto: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(SurveyResponseDto Result, ApiErrorResponse Error)> GetUserSurveyResponseAsync(string surveyId, string lang = null)
        {
            if (string.IsNullOrEmpty(surveyId))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys/{Uri.EscapeDataString(surveyId)}/responses/me?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetUserSurveyResponseAsync({surveyId})"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyResponseDto>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyResponseDto: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(SurveyResponseDto[] Result, ApiErrorResponse Error)> GetUserSurveyResponsesForSurveyAsync(string surveyId, string lang = null)
        {
            if (string.IsNullOrEmpty(surveyId))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys/{Uri.EscapeDataString(surveyId)}/responses/me/all?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetUserSurveyResponsesForSurveyAsync({surveyId})"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyResponseDto[]>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyResponseDto[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(SurveyResponseDto[] Result, ApiErrorResponse Error)> GetAllUserResponsesAsync(string lang = null)
        {
            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/surveys/responses/user/all?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetAllUserResponsesAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<SurveyResponseDto[]>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize SurveyResponseDto[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
