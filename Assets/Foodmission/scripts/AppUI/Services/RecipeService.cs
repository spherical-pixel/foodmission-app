using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class RecipeService : IRecipeService
    {
        private readonly IStoreService _storeService;

        public RecipeService(IStoreService storeService)
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

        public async Task<(PaginatedRecipeResponse Result, ApiErrorResponse Error)> GetRecipesAsync(
            string search = null,
            string category = null,
            string cuisineType = null,
            string difficulty = null,
            int page = 1,
            int limit = 20)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/recipes?page={page}&limit={limit}");

            if (!string.IsNullOrEmpty(search))
                sb.Append($"&search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrEmpty(category))
                sb.Append($"&category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(cuisineType))
                sb.Append($"&cuisineType={Uri.EscapeDataString(cuisineType)}");
            if (!string.IsNullOrEmpty(difficulty))
                sb.Append($"&difficulty={Uri.EscapeDataString(difficulty)}");

            string url = sb.ToString();

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetRecipesAsync"));

            string raw = request.downloadHandler.text;
            return (JsonConvert.DeserializeObject<PaginatedRecipeResponse>(raw), null);
        }

        public async Task<(Recipe Result, ApiErrorResponse Error)> GetRecipeAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return (null, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/recipes/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetRecipeAsync {id}"));

            string rawJson = request.downloadHandler.text;
            Debug.Log($"[RecipeService] GetRecipeAsync raw response for {id}:\n{rawJson}");
            var recipe = JsonConvert.DeserializeObject<Recipe>(rawJson);
            Debug.Log($"[RecipeService] GetRecipeAsync parsed recipe: title='{recipe?.title}', category='{recipe?.category}', cuisineType='{recipe?.cuisineType}', servings={recipe?.servings}");
            return (recipe, null);
        }

        public async Task<(PaginatedRecipeResponse Result, ApiErrorResponse Error)> GetMyRecipesAsync(
            string search = null,
            int page = 1,
            int limit = 20)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/recipes/me?page={page}&limit={limit}");

            if (!string.IsNullOrEmpty(search))
                sb.Append($"&search={Uri.EscapeDataString(search)}");

            string url = sb.ToString();

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMyRecipesAsync"));

            string raw = request.downloadHandler.text;
            return (JsonConvert.DeserializeObject<PaginatedRecipeResponse>(raw), null);
        }

        public async Task<(Recipe Result, ApiErrorResponse Error)> CreateRecipeAsync(CreateRecipeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.title))
                return (null, new ApiErrorResponse { message = "Title is required" });

            string url = $"{ApiConfig.BaseUrl}/api/v1/recipes";

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string jsonBody = JsonConvert.SerializeObject(req, settings);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] CreateRecipeAsync"));

            return (JsonConvert.DeserializeObject<Recipe>(request.downloadHandler.text), null);
        }

        public async Task<(Recipe Result, ApiErrorResponse Error)> UpdateRecipeAsync(string id, CreateRecipeRequest req)
        {
            if (string.IsNullOrEmpty(id))
                return (null, new ApiErrorResponse { message = "Recipe id is required" });
            if (req == null)
                return (null, new ApiErrorResponse { message = "Request body is required" });

            string url = $"{ApiConfig.BaseUrl}/api/v1/recipes/{Uri.EscapeDataString(id)}";

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string jsonBody = JsonConvert.SerializeObject(req, settings);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using UnityWebRequest request = new UnityWebRequest(url, "PATCH");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateRecipeAsync {id}"));

            return (JsonConvert.DeserializeObject<Recipe>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteRecipeAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return (false, new ApiErrorResponse { message = "Recipe id is required" });

            string url = $"{ApiConfig.BaseUrl}/api/v1/recipes/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = UnityWebRequest.Delete(url);
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteRecipeAsync {id}"));

            return (true, null);
        }

        public async Task<(MultipleRecommendationResponse Result, ApiErrorResponse Error)> GetRecommendationsAsync(
            int expiringWithinDays = 7,
            int limit = 10,
            int offset = 0)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/recipes/me/recommendations" +
                         $"?expiringWithinDays={expiringWithinDays}&limit={limit}&offset={offset}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetRecommendationsAsync"));

            return (JsonConvert.DeserializeObject<MultipleRecommendationResponse>(
                request.downloadHandler.text), null);
        }
    }
}
