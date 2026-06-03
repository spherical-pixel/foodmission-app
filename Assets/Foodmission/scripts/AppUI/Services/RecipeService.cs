using System;
using System.Text;
using System.Threading.Tasks;

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
            return (JsonUtility.FromJson<PaginatedRecipeResponse>(raw), null);
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

            return (JsonUtility.FromJson<Recipe>(request.downloadHandler.text), null);
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
            return (JsonUtility.FromJson<PaginatedRecipeResponse>(raw), null);
        }
    }
}
