using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace eu.foodmission.platform
{
    public enum AvatarMood
    {
        Neutral,
        Happy,
        VeryHappy,
        Bored,
        Speaking,
        Idle
    }

    public interface IAvatarService
    {
        Task InitializeAsync();
        
        bool IsInitialized { get; }
        
        RenderTexture AvatarCameraRenderTexture { get; }
        RenderTexture FullBodyAvatarRenderTexture { get; }

        void SetAvatarCameraActive(bool active);
        void SetFullBodyCameraActive(bool active);

        void SetRandomConfig();
        void SetAvatarConfig(AvatarConfig config);
        AvatarConfig GetCurrentAvatarConfig{get;}

        List<Color> GetColorPalette(AvatarEditorItemEnum itemType);
        int GetMaxPartCount(AvatarEditorItemEnum itemType);
        bool HasSavedConfig { get; }
        bool HasAvatar { get; }
        event System.Action OnFaceTextureChanged;
        Texture2D GetFaceTexture();
        Task<Texture2D> EnsureFaceTextureAsync();
        Task CaptureAndSaveFaceTextureAsync();
        void ClearFaceTexture();
        void SaveCurrentConfig();
        Task SaveCurrentConfigAsync(bool hasAvatar = true);
        Task SetHasAvatarAsync(bool hasAvatar);
        void LoadSavedConfig();
    }
}
