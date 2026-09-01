using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace eu.foodmission.platform
{
    public class PilotSurveyService : IPilotSurveyService
    {
        private readonly ISurveyService _surveyService;
        private readonly IStoreService _storeService;
        private readonly ILocalStorageService _localStorageService;
        private readonly IAuthService _authService;

        private static readonly HashSet<string> s_PilotCountryCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "de", "gr", "it", "nl", "no", "si"
        };

        private static readonly List<PilotSurveyRule> s_Rules = new List<PilotSurveyRule>
        {
            new PilotSurveyRule("second-use", minActiveDaysInCycle: 2),
            new PilotSurveyRule("third-use", minActiveDaysInCycle: 3),
            new PilotSurveyRule("fourth-use", minActiveDaysInCycle: 4),
            new PilotSurveyRule("fifth-use", minActiveDaysInCycle: 5),
            new PilotSurveyRule("sixth-use", minActiveDaysInCycle: 6),
            new PilotSurveyRule("seventh", minActiveDaysInCycle: 7),
            new PilotSurveyRule("after-1-mt-and-at-least-8th-use", minActiveDaysInCycle: 8, minDaysSinceCycleStart: 30),
            new PilotSurveyRule("after-1-m-and-at-least-9th-use", minActiveDaysInCycle: 9, minDaysSinceCycleStart: 30),
            new PilotSurveyRule("after-1-m-and-at-least-10th", minActiveDaysInCycle: 10, minDaysSinceCycleStart: 30),
            new PilotSurveyRule("end", minActiveDaysInCycle: 11, minDaysSinceCycleStart: 30, isEndSurvey: true)
        };

        private readonly HashSet<string> _postponedThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public PilotSurveyService(
            ISurveyService surveyService,
            IStoreService storeService,
            ILocalStorageService localStorageService,
            IAuthService authService = null)
        {
            _surveyService = surveyService;
            _storeService = storeService;
            _localStorageService = localStorageService;
            _authService = authService;
        }

        private string CurrentUserId
        {
            get
            {
                AppState s = _storeService?.GetAppState();
                return string.IsNullOrEmpty(s?.userId) ? "guest" : s.userId;
            }
        }

        private string CycleStorageKey => $"pilot_cycle_state_{CurrentUserId}";
        private string ConsentStorageKey => $"pilot_consent_accepted_{CurrentUserId}";

        public bool DebugBypassEligibility { get; set; } = false;
        private string _debugCountryOverride = null;

        public IReadOnlyList<PilotSurveyRule> GetRules() => s_Rules;

        public bool IsPilotCountry(string countryCode = null)
        {
            if (DebugBypassEligibility)
                return true;

            if (string.IsNullOrEmpty(countryCode))
            {
                countryCode = _debugCountryOverride ?? _storeService?.GetAppState()?.userCountry;
            }

            if (string.IsNullOrEmpty(countryCode))
                return false;

            return s_PilotCountryCodes.Contains(countryCode.Trim());
        }

        public Task<bool> HasAcceptedPilotConsentAsync()
        {
            if (DebugBypassEligibility)
                return Task.FromResult(true);

            if (!IsPilotCountry())
                return Task.FromResult(false);

            string consentVal = _localStorageService.GetValue<string>(ConsentStorageKey, "");
            if (bool.TryParse(consentVal, out bool accepted) && accepted)
            {
                return Task.FromResult(true);
            }

            AppState s = _storeService?.GetAppState();
            if (s != null && s.pilotConsentAccepted)
            {
                _localStorageService.SetValue<string>(ConsentStorageKey, "true");
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> AcceptPilotConsentAsync()
        {
            _localStorageService.SetValue<string>(ConsentStorageKey, "true");
            _storeService?.store?.Dispatch(AppActions.setPilotConsent.Invoke(true));
            Debug.Log($"[{GetType().Name}] Pilot consent accepted for user '{CurrentUserId}'.");

            var state = GetCurrentCycleState();
            SyncToPreferencesAsync(state, true);
            return Task.FromResult(true);
        }

        public PilotSurveyCycleState GetCurrentCycleState()
        {
            string raw = _localStorageService.GetValue<string>(CycleStorageKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    var state = JsonConvert.DeserializeObject<PilotSurveyCycleState>(raw);
                    if (state != null)
                    {
                        state.activeDatesInCycle ??= new List<string>();
                        state.completedSlugsInCycle ??= new List<string>();
                        state.skippedSlugsInCycle ??= new List<string>();
                        return state;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to deserialize PilotSurveyCycleState: {ex.Message}");
                }
            }

            // Fallback: Check if state was restored from server preferences into AppState
            AppState s = _storeService?.GetAppState();
            if (s?.pilotSurveyCycleState != null)
            {
                var state = s.pilotSurveyCycleState.Copy();
                state.activeDatesInCycle ??= new List<string>();
                state.completedSlugsInCycle ??= new List<string>();
                state.skippedSlugsInCycle ??= new List<string>();
                _localStorageService.SetValue<string>(CycleStorageKey, JsonConvert.SerializeObject(state));
                return state;
            }

            // Default initial state
            var newState = new PilotSurveyCycleState
            {
                currentCycle = 1,
                cycleStartDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                activeDatesInCycle = new List<string>(),
                completedSlugsInCycle = new List<string>(),
                skippedSlugsInCycle = new List<string>()
            };

            SaveCycleState(newState);
            return newState;
        }

        private void SaveCycleState(PilotSurveyCycleState state)
        {
            if (state == null) return;
            string json = JsonConvert.SerializeObject(state);
            _localStorageService.SetValue<string>(CycleStorageKey, json);
            _storeService?.store?.Dispatch(AppActions.setPilotCycleState.Invoke(state));

            bool consent = _localStorageService.GetValue<string>(ConsentStorageKey, "") == "true" ||
                           (_storeService?.GetAppState()?.pilotConsentAccepted ?? false);

            SyncToPreferencesAsync(state, consent);
        }

        private async void SyncToPreferencesAsync(PilotSurveyCycleState state, bool consent)
        {
            try
            {
                var auth = _authService ?? App.current?.services?.GetService<IAuthService>();
                if (auth == null) return;

                AppState appState = _storeService?.GetAppState();
                if (string.IsNullOrEmpty(appState?.accessToken) || string.IsNullOrEmpty(appState?.userId))
                    return;

                var req = new ProfileUpdateRequest
                {
                    preferences = new ProfileUpdatePreferences
                    {
                        pilotSurveyCycleState = state,
                        pilotConsentAccepted = consent
                    }
                };

                await auth.UpdateProfileAsync(req);
                //Debug.Log($"[{GetType().Name}] Synced pilot survey cycle state to user preferences on server");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Could not sync pilot survey cycle state to preferences: {ex.Message}");
            }
        }

        public void RecordDailyUsage()
        {
            if (!IsPilotCountry())
                return;

            var state = GetCurrentCycleState();
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(state.cycleStartDate))
            {
                state.cycleStartDate = today;
            }

            if (!state.activeDatesInCycle.Contains(today))
            {
                state.activeDatesInCycle.Add(today);
                SaveCycleState(state);
            }
        }

        public int GetActiveDaysCountInCurrentCycle()
        {
            var state = GetCurrentCycleState();
            return state.activeDatesInCycle.Count;
        }

        public int GetDaysSinceCurrentCycleStart()
        {
            var state = GetCurrentCycleState();
            if (string.IsNullOrEmpty(state.cycleStartDate))
                return 0;

            if (DateTime.TryParseExact(state.cycleStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate))
            {
                int days = (int)Math.Max(0, (DateTime.UtcNow.Date - startDate.Date).TotalDays);
                return days;
            }

            return 0;
        }

        public void AdvanceToNextCycle()
        {
            var state = GetCurrentCycleState();
            state.currentCycle++;
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            state.cycleStartDate = today;
            state.activeDatesInCycle.Clear();
            state.activeDatesInCycle.Add(today);
            state.completedSlugsInCycle.Clear();
            state.skippedSlugsInCycle.Clear();

            _postponedThisSession.Clear();
            SaveCycleState(state);
            Debug.Log($"[{GetType().Name}] Advanced to Survey Cycle {state.currentCycle}");
        }

        public void ResetCycles()
        {
            _localStorageService.DeleteValue(CycleStorageKey);
            _localStorageService.DeleteValue(ConsentStorageKey);
            _postponedThisSession.Clear();
        }

        public void PostponeSurvey(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return;
            _postponedThisSession.Add(slug);
        }

        public void SkipSurvey(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return;

            var state = GetCurrentCycleState();
            if (!state.skippedSlugsInCycle.Contains(slug))
            {
                state.skippedSlugsInCycle.Add(slug);
                SaveCycleState(state);
            }

            CheckAndAdvanceCycleIfCompleted(state);
        }

        public async Task<bool> MarkSurveyCompletedAsync(string slug, string surveyId)
        {
            if (string.IsNullOrEmpty(slug)) return false;

            var state = GetCurrentCycleState();
            if (!state.completedSlugsInCycle.Contains(slug))
            {
                state.completedSlugsInCycle.Add(slug);
                SaveCycleState(state);
            }

            CheckAndAdvanceCycleIfCompleted(state);
            return true;
        }

        private void CheckAndAdvanceCycleIfCompleted(PilotSurveyCycleState state)
        {
            // If the last survey ('end') has been completed or skipped, or all rules are processed, advance cycle
            bool endProcessed = state.completedSlugsInCycle.Contains("end") || state.skippedSlugsInCycle.Contains("end");
            if (endProcessed)
            {
                AdvanceToNextCycle();
                return;
            }

            bool allProcessed = true;
            foreach (var rule in s_Rules)
            {
                if (!state.completedSlugsInCycle.Contains(rule.Slug) && !state.skippedSlugsInCycle.Contains(rule.Slug))
                {
                    allProcessed = false;
                    break;
                }
            }

            if (allProcessed)
            {
                AdvanceToNextCycle();
            }
        }

        public async Task<SurveyDto> GetPendingPilotSurveyAsync(string lang = null)
        {
            string userCountry = _storeService?.GetAppState()?.userCountry ?? "(empty)";
            bool isPilot = IsPilotCountry();
            bool hasConsent = await HasAcceptedPilotConsentAsync();

            //Debug.Log($"[{GetType().Name}] 🔍 Evaluando encuesta pendiente: userCountry='{userCountry}', isPilotCountry={isPilot}, hasConsent={hasConsent}, DebugBypassEligibility={DebugBypassEligibility}");

            if (!isPilot)
            {
                //Debug.LogWarning($"[{GetType().Name}] ❌ Encuesta no aplicable: El país del usuario '{userCountry}' NO es un país piloto ({string.Join(", ", s_PilotCountryCodes)}). Puedes activar 'Bypass Elegibilidad' o pulsar 'Simular País DE' en el panel de pruebas de Home.");
                return null;
            }

            if (!hasConsent)
            {
                //Debug.LogWarning($"[{GetType().Name}] ❌ Encuesta no aplicable: El usuario no ha aceptado el consentimiento del piloto. Puedes pulsar 'Aceptar Consentimiento' en el panel de pruebas de Home.");
                return null;
            }

            RecordDailyUsage();

            var state = GetCurrentCycleState();
            int activeDays = state.activeDatesInCycle.Count;
            int daysSinceStart = GetDaysSinceCurrentCycleStart();

            //Debug.Log($"[{GetType().Name}] 📊 Estado del Ciclo {state.currentCycle}: Días activos={activeDays}, Días transcurridos={daysSinceStart}, Completadas=[{string.Join(", ", state.completedSlugsInCycle)}], Saltadas=[{string.Join(", ", state.skippedSlugsInCycle)}], PospuestasSesion=[{string.Join(", ", _postponedThisSession)}]");

            foreach (var rule in s_Rules)
            {
                if (state.completedSlugsInCycle.Contains(rule.Slug))
                {
                    //Debug.Log($"[{GetType().Name}] ⏭️ Regla '{rule.Slug}' ignorada: ya completada en ciclo.");
                    continue;
                }

                if (state.skippedSlugsInCycle.Contains(rule.Slug))
                {
                    //Debug.Log($"[{GetType().Name}] ⏭️ Regla '{rule.Slug}' ignorada: saltada en ciclo.");
                    continue;
                }

                if (_postponedThisSession.Contains(rule.Slug))
                {
                    //Debug.Log($"[{GetType().Name}] ⏭️ Regla '{rule.Slug}' ignorada: pospuesta en sesión actual.");
                    continue;
                }

                bool daysOk = activeDays >= rule.MinActiveDaysInCycle;
                bool elapsedOk = daysSinceStart >= rule.MinDaysSinceCycleStart;

                if (daysOk && elapsedOk)
                {
                    //Debug.Log($"[{GetType().Name}] ✅ Regla coincidente '{rule.Slug}' (mín días: {rule.MinActiveDaysInCycle}, mín transcurridos: {rule.MinDaysSinceCycleStart}). Solicitando encuesta al backend...");

                    var (survey, error) = await _surveyService.GetSurveyBySlugAsync(rule.Slug, lang);
                    if (survey != null && survey.questions != null && survey.questions.Length > 0)
                    {
                        //Debug.Log($"[{GetType().Name}] 🎉 Encuesta '{rule.Slug}' cargada con éxito ({survey.questions.Length} preguntas, id: {survey.id}).");
                        return survey;
                    }
                    else if (error != null)
                    {
                        Debug.LogWarning($"[{GetType().Name}] ⚠️ Error del backend al obtener survey '{rule.Slug}': {error.message} (status: {error.statusCode})");
                    }
                    else if (survey != null && (survey.questions == null || survey.questions.Length == 0))
                    {
                        Debug.LogWarning($"[{GetType().Name}] ⚠️ La encuesta '{rule.Slug}' existe en el backend pero no tiene preguntas.");
                    }
                }
                else
                {
                    Debug.Log($"[{GetType().Name}] ⏳ Regla '{rule.Slug}' aún no cumple requisitos: (Días activos: {activeDays}/{rule.MinActiveDaysInCycle}, Días transcurridos: {daysSinceStart}/{rule.MinDaysSinceCycleStart})");
                }
            }

            Debug.Log($"[{GetType().Name}] ℹ️ No hay encuestas pendientes para los criterios actuales.");
            return null;
        }

        public void SetDebugDays(int activeDaysCount, int daysSinceStart)
        {
            var state = GetCurrentCycleState();
            state.cycleStartDate = DateTime.UtcNow.AddDays(-daysSinceStart).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            state.activeDatesInCycle.Clear();

            for (int i = activeDaysCount - 1; i >= 0; i--)
            {
                string dateStr = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (!state.activeDatesInCycle.Contains(dateStr))
                {
                    state.activeDatesInCycle.Add(dateStr);
                }
            }

            _postponedThisSession.Clear();
            SaveCycleState(state);
            Debug.Log($"[{GetType().Name}] [DEBUG] Set activeDays={activeDaysCount}, daysSinceStart={daysSinceStart}, startDate={state.cycleStartDate}");
        }

        public void SetDebugUserCountry(string countryCode)
        {
            _debugCountryOverride = countryCode;
            Debug.Log($"[{GetType().Name}] [DEBUG] Debug country override set to '{countryCode}'");
        }

        public void ResetCycleSurveysOnly()
        {
            var state = GetCurrentCycleState();
            state.completedSlugsInCycle.Clear();
            state.skippedSlugsInCycle.Clear();
            _postponedThisSession.Clear();
            SaveCycleState(state);
            Debug.Log($"[{GetType().Name}] [DEBUG] Reset completed/skipped surveys in Cycle {state.currentCycle}");
        }
    }
}
