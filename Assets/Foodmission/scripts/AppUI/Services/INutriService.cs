using System.Threading.Tasks;
using UnityEngine;

namespace eu.foodmission.platform
{
    public enum NutriMood
    {
        Neutral = 0,
        Happy = 1,
        VeryHappy = 2,
        Bored = -1,
        Sick = -2,
        Dirty = -3
    }

    public enum NutriAction
    {
        Idle,
        Talking,
        Greeting,
        LookingDown,
        Celebration
    }

    public interface INutriService
    {
        Task InitializeAsync();
        void SetActive(bool active);
        void SetCameraActive(bool active);
        void SetMood(NutriMood mood);
        void SetAction(NutriAction nutriAction);
        NutriMood CurrentMood { get; }
        bool IsInitialized { get; }

        RenderTexture NutriCameraRenderTexture { get; }
    }
}