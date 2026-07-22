using System.Threading.Tasks;
using UnityEngine;

namespace eu.foodmission.platform
{
    public interface IImageService
    {
        Task<Texture2D> LoadImageAsync(string url);
        void ClearCache();
    }
}
