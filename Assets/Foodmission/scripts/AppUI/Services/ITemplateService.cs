using System.Threading.Tasks;

using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public interface ITemplateService
    {
        Task PreloadAllAsync();
        VisualTreeAsset Get(string address);
    }
}
