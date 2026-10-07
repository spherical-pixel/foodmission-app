using System;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Helpers for screens that resolve topic or dimension ids (banners) through <see cref="IDimensionService"/>.</summary>
    public static class DimensionCatalog
    {
        /// <summary>
        /// Waits for the dimension catalog (no-op once loaded). Screens opened directly (Home, a deep link) can't rely on
        /// the splash preload having finished. A failure only means default banners, so it never throws.
        /// </summary>
        public static async Task EnsureLoadedAsync(IDimensionService dimensionService)
        {
            if (dimensionService == null)
            {
                return;
            }

            try
            {
                await dimensionService.PreloadAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DimensionCatalog] Loading dimensions failed: {ex.Message}");
            }
        }
    }
}
