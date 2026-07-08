using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class TemplateService : ITemplateService
    {
        private readonly Dictionary<string, VisualTreeAsset> _cache = new();

        private static readonly string[] AllAddresses =
        {
            TemplateAddresses.Home,
            TemplateAddresses.Login,
            TemplateAddresses.Register,
            TemplateAddresses.ForgotPassword,
            TemplateAddresses.Profile,
            TemplateAddresses.Settings,
            TemplateAddresses.OnboardingProfile,
            TemplateAddresses.CompleteWelcome,
            TemplateAddresses.ShoppingList,
            TemplateAddresses.ShoppingListDetail,
            TemplateAddresses.EditProfile,
            TemplateAddresses.AvatarEditor,
            TemplateAddresses.Pantry,
            TemplateAddresses.PantryItemDetail,
            TemplateAddresses.MealLog,
            TemplateAddresses.FoodWaste,
            TemplateAddresses.FoodWasteAdd,
            TemplateAddresses.Groups,
            TemplateAddresses.GroupsCreate,
            TemplateAddresses.GroupsJoin,
            TemplateAddresses.GroupDetail,
            TemplateAddresses.OnboardingGroups,
            TemplateAddresses.AvatarEditorPanelItem,
            TemplateAddresses.ForceUpdate,
            TemplateAddresses.FoodInfo
        };

        public async Task PreloadAllAsync()
        {
            Task[] tasks = AllAddresses.Select(LoadOneAsync).ToArray();
            await Task.WhenAll(tasks);
        }

        private async Task LoadOneAsync(string address)
        {
            AsyncOperationHandle<VisualTreeAsset> handle =
                Addressables.LoadAssetAsync<VisualTreeAsset>(address);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _cache[address] = handle.Result;
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Failed to load template: {address}");
            }
        }

        public VisualTreeAsset Get(string address)
        {
            if (_cache.TryGetValue(address, out VisualTreeAsset asset))
            {
                return asset;
            }

            Debug.LogError($"[{GetType().Name}] Template not in cache: {address} — was PreloadAllAsync called?");
            return null;
        }
    }
}
