using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Editor and unsupported platforms: logs what would be shared and exports the image so it can be inspected on disk.
    /// </summary>
    public class EditorShareService : IShareService
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
            Debug.Log($"[EditorShareService] Subject: {content.Subject}\nText:\n{content.Text}\nImage: {imagePath ?? "none"}");
            return Task.FromResult(true);
        }
    }
}
