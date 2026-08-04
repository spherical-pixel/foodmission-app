using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IEventService
    {
        string CurrentSessionId { get; }
        Task<(UserEvent Result, ApiErrorResponse Error)> RecordClientEventAsync(CreateClientEventRequest request);
        Task TrackSessionStartAsync();
        Task TrackSessionEndAsync();
    }
}
