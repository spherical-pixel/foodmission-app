using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace eu.foodmission.platform
{
    public enum AvatarMood
    {
        Happy = 1,
        Neutral = 0,
        Sad = -1
    }

    public interface IAvatarService
    {
        Task InitializeAsync();

        bool IsInitialized { get; }

        RenderTexture AvatarCameraRenderTexture { get; }
        RenderTexture FullBodyAvatarRenderTexture { get; }

        AvatarController AvatarController { get; }

        void SetAvatarCameraActive(bool active);
        void SetFullBodyCameraActive(bool active);

        void SetRandomConfig();
        void SetAvatarConfig(AvatarConfig config);
        AvatarConfig GetCurrentAvatarConfig { get; }
        AvatarConfig GetDefaultConfig();

        List<Color> GetColorPalette(AvatarEditorItemEnum itemType);
        int GetMaxPartCount(AvatarEditorItemEnum itemType);
        bool HasSavedConfig { get; }
        bool HasAvatar { get; }
        event System.Action OnFaceTextureChanged;
        Texture2D GetFaceTexture(bool allowFallback = false);
        Task<Texture2D> EnsureFaceTextureAsync();
        Task CaptureAndSaveFaceTextureAsync();
        void ClearFaceTexture();
        void SaveCurrentConfig();
        Task SaveCurrentConfigAsync(bool hasAvatar = true);
        Task SetHasAvatarAsync(bool hasAvatar);
        void LoadSavedConfig();
    }
}
