using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IWhatsNewService
    {
        Task<(bool ShouldShow, string ReleaseNotes)> CheckShouldShowAsync();
        Task MarkAsSeenAsync();
    }
}
