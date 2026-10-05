using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IShareService
    {
        /// <summary>
        /// Opens the native share sheet. Returns false when the content is empty or the sheet could not be opened;
        /// it does not report whether the user actually shared.
        /// </summary>
        Task<bool> ShareAsync(ShareContent content);
    }
}
