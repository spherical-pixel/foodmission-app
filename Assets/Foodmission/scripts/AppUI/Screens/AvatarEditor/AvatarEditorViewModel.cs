using Unity.AppUI.MVVM;

using UnityEngine;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class AvatarEditorViewModel : ViewModelBase
    {
        [ObservableProperty]
        private RenderTexture _avatarRenderTexture;

        private readonly IAvatarService _avatarService;
        public IAvatarService AvatarService => _avatarService;

        public AvatarEditorViewModel(IStoreService storeService, IAvatarService avatarService) : base(storeService)
        {
            _avatarService = avatarService;

            AvatarRenderTexture = avatarService.FullBodyAvatarRenderTexture;

            if (_avatarService.HasSavedConfig)
            {
                _avatarService.LoadSavedConfig();
            }
            else
            {
                _avatarService.SetRandomConfig();
            }

            if (AvatarRenderTexture == null)
            {
                Debug.LogError($"[{GetType().Name}] Full-body avatar render texture is null!");
            }
            else
            {
                Debug.Log($"[{GetType().Name}] Avatar render texture assigned successfully.");
            }
        }

        public AvatarConfig GetAvatarConfig()
        {
            return _avatarService.GetCurrentAvatarConfig;
        }

        public async System.Threading.Tasks.Task SaveAvatarAsync(bool hasAvatar = true)
        {
            if (_avatarService != null)
            {
                await _avatarService.SaveCurrentConfigAsync(hasAvatar);
            }
        }

        public async System.Threading.Tasks.Task SetHasAvatarAsync(bool hasAvatar)
        {
            if (_avatarService != null)
            {
                await _avatarService.SetHasAvatarAsync(hasAvatar);
            }
        }
    }
}
