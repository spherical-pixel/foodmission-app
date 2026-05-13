using System.Threading.Tasks;
using UnityEngine;

namespace eu.foodmission.platform
{
    public enum NutriMood
    {
        Neutral,
        Happy,
        VeryHappy,
        Bored,
        Sick,
        Dirty,
        Talking,
        Celebration,
        Greeting,
        LookingDown
    }

    public interface INutriService
    {
        Task InitializeAsync();
        void SetActive(bool active);
        void SetCameraActive(bool active);
        void SetMood(NutriMood mood);
        NutriMood CurrentMood { get; }
        bool IsInitialized { get; }

        RenderTexture NutriCameraRenderTexture { get; }
    }
}