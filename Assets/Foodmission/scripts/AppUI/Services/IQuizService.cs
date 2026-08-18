using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IQuizService
    {
        Task<(PaginatedQuizResponse Result, ApiErrorResponse Error)> GetQuizzesAsync(
            QuizFilterParams filters = null,
            int page = 1,
            int limit = 10,
            string lang = null);

        Task<(Quiz Result, ApiErrorResponse Error)> GetQuizAsync(
            string codeOrId,
            string lang = null);

        Task<(QuizProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(
            string lang = null);

        Task<(QuizProgress Result, ApiErrorResponse Error)> GetQuizProgressAsync(
            string codeOrId,
            string lang = null);

        Task<(QuizProgress Result, ApiErrorResponse Error)> SubmitQuizAnswerAsync(
            string codeOrId,
            string selectedLabel,
            string lang = null);
    }
}
