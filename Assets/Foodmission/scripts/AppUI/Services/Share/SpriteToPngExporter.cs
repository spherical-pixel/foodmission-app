using System;
using System.IO;

using UnityEngine;
using Object = UnityEngine.Object;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Writes a sprite (its rect only, also from atlases and non-readable textures) to a PNG for the share sheet.
    /// Each export deletes the previous files in the folder, so shared images do not pile up.
    /// </summary>
    public static class SpriteToPngExporter
    {
        public static string DefaultDirectory => Path.Combine(Application.temporaryCachePath, "share");

        public static string Export(Sprite sprite, string directory)
        {
            if (sprite == null || sprite.texture == null || string.IsNullOrEmpty(directory))
            {
                return null;
            }

            Texture2D copy = null;
            try
            {
                copy = CopyRect(sprite.texture, sprite.textureRect);
                byte[] png = copy.EncodeToPNG();

                Directory.CreateDirectory(directory);
                foreach (string old in Directory.GetFiles(directory))
                {
                    File.Delete(old);
                }

                string path = Path.Combine(directory, $"share_{DateTime.UtcNow.Ticks}.png");
                File.WriteAllBytes(path, png);
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteToPngExporter] Export failed: {ex.Message}");
                return null;
            }
            finally
            {
                if (copy != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(copy);
                    }
                    else
                    {
                        Object.DestroyImmediate(copy);
                    }
                }
            }
        }

        private static Texture2D CopyRect(Texture2D source, Rect rect)
        {
            int x = Mathf.FloorToInt(rect.x);
            int y = Mathf.FloorToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);

            if (source.isReadable)
            {
                result.SetPixels(source.GetPixels(x, y, width, height));
                result.Apply();
                return result;
            }

            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                result.ReadPixels(new Rect(x, y, width, height), 0, 0);
                result.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
            return result;
        }
    }
}
