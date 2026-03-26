using System;
using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Service for detecting and managing on-screen keyboard visibility and dimensions.
    /// Provides platform-specific implementations for iOS and Android.
    /// </summary>
    public interface IKeyboardService : IDisposable
    {
        /// <summary>
        /// Gets the current height of the on-screen keyboard in pixels.
        /// Returns 0 if the keyboard is not visible.
        /// </summary>
        float KeyboardHeight { get; }

        /// <summary>
        /// Gets a value indicating whether the on-screen keyboard is currently visible.
        /// </summary>
        bool IsKeyboardVisible { get; }

        /// <summary>
        /// Event fired when the keyboard becomes visible.
        /// Provides the keyboard height in pixels.
        /// </summary>
        event Action<float> KeyboardShown;

        /// <summary>
        /// Event fired when the keyboard is hidden.
        /// </summary>
        event Action KeyboardHidden;

        /// <summary>
        /// Initializes the keyboard service.
        /// Should be called once during app startup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the keyboard height as a ratio of the screen height (0.0 to 1.0).
        /// Useful for calculating padding percentages.
        /// </summary>
        float GetKeyboardHeightRatio();
    }
}
