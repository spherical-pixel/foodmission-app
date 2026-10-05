using System;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Android share sheet through Intent.ACTION_SEND + createChooser. Images go through the FileProvider declared in
    /// Plugins/Android/FMShare.androidlib (authority "&lt;package&gt;.fmshare", only cache/share/ is exposed).
    /// </summary>
    public class AndroidShareService : IShareService
    {
        private const string AuthoritySuffix = ".fmshare";
        private const int FlagGrantReadUriPermission = 1;

        public Task<bool> ShareAsync(ShareContent content)
        {
            if (content == null || content.IsEmpty)
            {
                return Task.FromResult(false);
            }

            string imagePath = content.Image != null
                ? SpriteToPngExporter.Export(content.Image, SpriteToPngExporter.DefaultDirectory)
                : null;

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent", intentClass.GetStatic<string>("ACTION_SEND")))
                {
                    if (!string.IsNullOrWhiteSpace(content.Text))
                    {
                        Release(intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), content.Text));
                    }
                    if (!string.IsNullOrWhiteSpace(content.Subject))
                    {
                        Release(intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), content.Subject));
                    }

                    if (imagePath != null)
                    {
                        string authority = activity.Call<string>("getPackageName") + AuthoritySuffix;
                        using (var file = new AndroidJavaObject("java.io.File", imagePath))
                        using (var fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider"))
                        using (var uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", activity, authority, file))
                        using (var clipDataClass = new AndroidJavaClass("android.content.ClipData"))
                        using (var clip = clipDataClass.CallStatic<AndroidJavaObject>("newRawUri", "", uri))
                        {
                            Release(intent.Call<AndroidJavaObject>("setType", "image/png"));
                            Release(intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri));
                            intent.Call("setClipData", clip);
                            Release(intent.Call<AndroidJavaObject>("addFlags", FlagGrantReadUriPermission));
                        }
                    }
                    else
                    {
                        Release(intent.Call<AndroidJavaObject>("setType", "text/plain"));
                    }

                    string title = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHARE");
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, title))
                    {
                        activity.Call("startActivity", chooser);
                    }
                }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AndroidShareService] Share failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        // Builder-style Intent methods return the Intent; dispose the extra local reference.
        private static void Release(AndroidJavaObject obj)
        {
            obj?.Dispose();
        }
    }
}
