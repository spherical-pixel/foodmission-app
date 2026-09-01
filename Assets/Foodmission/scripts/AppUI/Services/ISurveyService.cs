using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface ISurveyService
    {
        Task<(SurveyDto[] Result, ApiErrorResponse Error)> GetSurveysAsync(string lang = null);
        Task<(SurveyDto Result, ApiErrorResponse Error)> GetSurveyBySlugAsync(string slug, string lang = null);
        Task<(SurveyDto Result, ApiErrorResponse Error)> GetSurveyByIdAsync(string id, string lang = null);
        Task<(SurveyResponseDto Result, ApiErrorResponse Error)> SubmitSurveyResponseAsync(string surveyId, SubmitSurveyResponseDto dto);
        Task<(SurveyResponseDto Result, ApiErrorResponse Error)> GetUserSurveyResponseAsync(string surveyId, string lang = null);
        Task<(SurveyResponseDto[] Result, ApiErrorResponse Error)> GetUserSurveyResponsesForSurveyAsync(string surveyId, string lang = null);
        Task<(SurveyResponseDto[] Result, ApiErrorResponse Error)> GetAllUserResponsesAsync(string lang = null);
    }
}
