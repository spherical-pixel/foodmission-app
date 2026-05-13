using UnityEngine;

namespace eu.foodmission.platform
{
    public static class AndroidSystemBar
    {
        private static AndroidJavaProxy _visibilityListener;

        public static void ShowAndSetTransparent()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                RunOnUiThread(() =>
                {
                    try
                    {
                        var window = GetWindow();
                        if (window == null) return;

                        ApplyTransparent(window);
                        RegisterResetWatchdog(window);

                        Debug.Log("[AndroidSystemBar] System bars set to transparent");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[AndroidSystemBar] UI thread error: {e.Message}");
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidSystemBar] Failed: {e.Message}");
            }
#endif
        }

        private static void ApplyTransparent(AndroidJavaObject window)
        {
            window.Call("clearFlags", 1024);
            window.Call("setStatusBarColor", ColorToARGB(Color.clear));
            window.Call("setNavigationBarColor", ColorToARGB(Color.clear));
        }

        private static void RegisterResetWatchdog(AndroidJavaObject window)
        {
            using (var decorView = window.Call<AndroidJavaObject>("getDecorView"))
            {
                if (decorView == null) return;

                _visibilityListener = new SystemUiVisibilityChangeListener();
                decorView.Call("setOnSystemUiVisibilityChangeListener", _visibilityListener);
                Debug.Log("[AndroidSystemBar] Reset watchdog registered");
            }
        }

        private class SystemUiVisibilityChangeListener : AndroidJavaProxy
        {
            public SystemUiVisibilityChangeListener()
                : base("android.view.View$OnSystemUiVisibilityChangeListener")
            {
            }

            private void onSystemUiVisibilityChange(int visibility)
            {
                using (var window = GetWindow())
                {
                    ApplyTransparent(window);
                }
            }
        }

        private static AndroidJavaObject GetWindow()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                return activity.Call<AndroidJavaObject>("getWindow");
            }
        }

        private static void RunOnUiThread(System.Action action)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
            }
        }

        private static int ColorToARGB(Color32 color)
        {
            int value = 0;
            value |= color.a << 24;
            value |= color.r << 16;
            value |= color.g << 8;
            value |= color.b;
            return value;
        }

        public static void SetAppearance(bool lightIcons)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                RunOnUiThread(() =>
                {
                    try
                    {
                        var window = GetWindow();
                        if (window == null) return;

                        int apiLevel;
                        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        {
                            apiLevel = version.GetStatic<int>("SDK_INT");
                        }

                        if (apiLevel >= 30)
                        {
                            using (var insetsController = window.Call<AndroidJavaObject>("getInsetsController"))
                            {
                                int appearanceLightStatusBars = 0x00000010;
                                insetsController.Call("setSystemBarsAppearance",
                                    lightIcons ? appearanceLightStatusBars : 0,
                                    appearanceLightStatusBars);
                            }
                        }
                        else
                        {
                            using (var decorView = window.Call<AndroidJavaObject>("getDecorView"))
                            {
                                int flags = decorView.Call<int>("getSystemUiVisibility");
                                int lightStatusBar = 0x00002000;
                                decorView.Call("setSystemUiVisibility",
                                    lightIcons ? (flags | lightStatusBar) : (flags & ~lightStatusBar));
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[AndroidSystemBar] Appearance error: {e.Message}");
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidSystemBar] SetAppearance failed: {e.Message}");
            }
#endif
        }
    }
}
