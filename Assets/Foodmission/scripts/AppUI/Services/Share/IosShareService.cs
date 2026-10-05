using System;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// iOS share sheet (UIActivityViewController) through Plugins/iOS/FMShare.mm.
    /// "Save image" in the sheet needs NSPhotoLibraryAddUsageDescription in Info.plist once images are shared from the UI.
    /// </summary>
    public class IosShareService : IShareService
    {
        public Task<bool> ShareAsync(ShareContent content)
        {
            if (content == null || content.IsEmpty)
            {
                return Task.FromResult(false);
            }

            string imagePath = content.Image != null
                ? SpriteToPngExporter.Export(content.Image, SpriteToPngExporter.DefaultDirectory)
                : null;

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                return Task.FromResult(_FMShare(content.Text, content.Subject, imagePath));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IosShareService] Share failed: {ex.Message}");
                return Task.FromResult(false);
            }
#else
            Debug.LogError($"[IosShareService] Only available on iOS devices (image: {imagePath ?? "none"})");
            return Task.FromResult(false);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        // FMShare.mm returns a 1-byte C++ bool; the default marshaling would read a 4-byte BOOL.
        [System.Runtime.InteropServices.DllImport("__Internal")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U1)]
        private static extern bool _FMShare(string text, string subject, string imagePath);
#endif
    }
}
