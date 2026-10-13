using System;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>Loads the three knowledge bars into AppState.knowledgeProgress.</summary>
    public interface IKnowledgeProgressService
    {
        bool IsLoading { get; }
        event Action LoadingChanged;

        /// <summary>Fetches the bars; on error the cached ones stay. Results from a previous session are dropped.</summary>
        Task RefreshAsync();
    }
}
