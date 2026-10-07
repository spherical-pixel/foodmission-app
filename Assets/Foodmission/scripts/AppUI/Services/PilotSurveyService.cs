using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public Func<DateTime> NowLocal { get; set; } = () => DateTime.Now;

        private string TodayLocal => NowLocal().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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

        private string CycleStorageKey => CycleStorageKeyFor(CurrentUserId);

        /// <summary>Local storage key of a user's survey cycle state.</summary>
        public static string CycleStorageKeyFor(string userId) => $"pilot_cycle_state_{userId}";

        /// <summary>Dev time travel: moves the cycle start and active days <paramref name="days"/> days back. Empty or corrupt state is returned unchanged.</summary>
        public static string ShiftStoredDates(string json, int days)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            try
            {
                var state = JsonConvert.DeserializeObject<PilotSurveyCycleState>(json);
                if (state == null)
                {
                    return json;
                }

                state.cycleStartDate = ShiftDay(state.cycleStartDate, days);
                if (state.activeDatesInCycle != null)
                {
                    state.activeDatesInCycle = state.activeDatesInCycle.ConvertAll(d => ShiftDay(d, days));
                }
                if (!string.IsNullOrEmpty(state.lastSurveyDay))
                {
                    state.lastSurveyDay = ShiftDay(state.lastSurveyDay, days);
                }
                if (state.postponedUntil != null)
                {
                    foreach (PostponedSurvey p in state.postponedUntil)
                    {
                        if (p != null)
                        {
                            p.day = ShiftDay(p.day, days);
                        }
                    }
                }
                return JsonConvert.SerializeObject(state);
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private static string ShiftDay(string day, int days)
        {
            if (!DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                return day;
            }
            return parsed.AddDays(-days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
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
            return Task.FromResult(HasAcceptedPilotConsent());
        }

        private bool HasAcceptedPilotConsent()
        {
            if (DebugBypassEligibility)
                return true;

            if (!IsPilotCountry())
                return false;

            string consentVal = _localStorageService.GetValue<string>(ConsentStorageKey, "");
            if (bool.TryParse(consentVal, out bool accepted) && accepted)
            {
                return true;
            }

            AppState s = _storeService?.GetAppState();
            if (s != null && s.pilotConsentAccepted)
            {
                _localStorageService.SetValue<string>(ConsentStorageKey, "true");
                return true;
            }

            return false;
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
            PilotSurveyCycleState local = null;
            string raw = _localStorageService.GetValue<string>(CycleStorageKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    local = JsonConvert.DeserializeObject<PilotSurveyCycleState>(raw);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to deserialize PilotSurveyCycleState: {ex.Message}");
                }
            }

            // AppState holds the server copy restored on login (and every local save)
            PilotSurveyCycleState server = _storeService?.GetAppState()?.pilotSurveyCycleState;
            PilotSurveyCycleState merged = Merge(local, server);
            if (merged == null)
            {
                // Default initial state
                merged = Normalize(new PilotSurveyCycleState
                {
                    currentCycle = 1,
                    cycleStartDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
                SaveCycleState(merged);
                return merged;
            }

            string mergedJson = JsonConvert.SerializeObject(merged);
            if (server == null || mergedJson != JsonConvert.SerializeObject(Normalize(server.Copy())))
            {
                // The server (or AppState) is behind: store and sync the merge
                SaveCycleState(merged);
            }
            else if (raw != mergedJson)
            {
                _localStorageService.SetValue<string>(CycleStorageKey, mergedJson);
            }
            return merged;
        }

        private static string LatestDay(string a, string b)
        {
            return string.CompareOrdinal(a ?? "", b ?? "") >= 0 ? (a ?? "") : (b ?? "");
        }

        private static PilotSurveyCycleState Normalize(PilotSurveyCycleState state)
        {
            state.activeDatesInCycle ??= new List<string>();
            state.completedSlugsInCycle ??= new List<string>();
            state.skippedSlugsInCycle ??= new List<string>();
            state.postponedUntil ??= new List<PostponedSurvey>();
            state.lastSurveyDay ??= "";
            return state;
        }

        /// <summary>
        /// Combines this device's cycle with the server's (restored on login), so no device repeats or loses surveys:
        /// a higher cycle wins outright; within the same cycle days and answered/skipped surveys are united.
        /// </summary>
        public static PilotSurveyCycleState Merge(PilotSurveyCycleState local, PilotSurveyCycleState server)
        {
            if (local == null)
            {
                return server == null ? null : Normalize(server.Copy());
            }
            if (server == null)
            {
                return Normalize(local.Copy());
            }
            string lastSurveyDay = LatestDay(local.lastSurveyDay, server.lastSurveyDay);
            if (local.currentCycle != server.currentCycle)
            {
                PilotSurveyCycleState winner = Normalize((local.currentCycle > server.currentCycle ? local : server).Copy());
                winner.lastSurveyDay = lastSurveyDay;
                return winner;
            }

            PilotSurveyCycleState a = Normalize(local.Copy());
            PilotSurveyCycleState b = Normalize(server.Copy());
            return new PilotSurveyCycleState
            {
                currentCycle = a.currentCycle,
                lastSurveyDay = lastSurveyDay,
                cycleStartDate = new[] { a.cycleStartDate, b.cycleStartDate }
                    .Where(d => !string.IsNullOrEmpty(d)).OrderBy(d => d, StringComparer.Ordinal).FirstOrDefault() ?? "",
                activeDatesInCycle = a.activeDatesInCycle.Union(b.activeDatesInCycle).OrderBy(d => d, StringComparer.Ordinal).ToList(),
                completedSlugsInCycle = a.completedSlugsInCycle.Union(b.completedSlugsInCycle).ToList(),
                skippedSlugsInCycle = a.skippedSlugsInCycle.Union(b.skippedSlugsInCycle).ToList(),
                postponedUntil = a.postponedUntil.Concat(b.postponedUntil)
                    .Where(p => p != null && !string.IsNullOrEmpty(p.slug))
                    .GroupBy(p => p.slug)
                    .Select(g => new PostponedSurvey { slug = g.Key, day = g.Select(p => p.day ?? "").OrderBy(d => d, StringComparer.Ordinal).Last() })
                    .ToList()
            };
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
            string pendingKey = PendingSyncKeyFor(CurrentUserId);
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
                        pilotConsentAccepted = consent,
                        // Non-nullable bool, always serialized: without this the PATCH turns it off
                        autoAddToPantry = appState.userAutoAddToPantry
                    }
                };

                var (success, error) = await auth.UpdateProfileAsync(req);
                if (success)
                {
                    _localStorageService.DeleteValue(pendingKey);
                }
                else
                {
                    _localStorageService.SetValue<string>(pendingKey, "true");
                    Debug.LogWarning($"[{GetType().Name}] Pilot cycle sync failed, will retry on the next Home entry: {error?.message}");
                }
            }
            catch (Exception ex)
            {
                _localStorageService.SetValue<string>(pendingKey, "true");
                Debug.LogWarning($"[{GetType().Name}] Could not sync pilot survey cycle state to preferences: {ex.Message}");
            }
        }

        /// <summary>Local flag set when the last cycle PATCH failed; cleared when one succeeds.</summary>
        public static string PendingSyncKeyFor(string userId) => $"pilot_cycle_pending_sync_{userId}";

        public void OnHomeEntered()
        {
            if (!IsPilotCountry())
            {
                return;
            }

            bool pending = _localStorageService.GetValue<string>(PendingSyncKeyFor(CurrentUserId), "") == "true";
            int before = GetActiveDaysCountInCurrentCycle();
            // Days before the pilot consent don't count: they would make several surveys due at once on accepting
            if (HasAcceptedPilotConsent())
            {
                RecordDailyUsage();
            }
            if (pending && GetActiveDaysCountInCurrentCycle() == before)
            {
                // RecordDailyUsage saved (and synced) nothing today: send the state that failed before
                SaveCycleState(GetCurrentCycleState());
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
            state.postponedUntil.Clear();

            SaveCycleState(state);
            Debug.Log($"[{GetType().Name}] Advanced to Survey Cycle {state.currentCycle}");
        }

        public void ResetCycles()
        {
            _localStorageService.DeleteValue(CycleStorageKey);
            _localStorageService.DeleteValue(ConsentStorageKey);
            // Otherwise the next read would merge the AppState copy back in
            _storeService?.store?.Dispatch(AppActions.setPilotCycleState.Invoke(null));
        }

        public void PostponeSurvey(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return;

            var state = GetCurrentCycleState();
            string tomorrow = NowLocal().Date.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            state.postponedUntil.RemoveAll(p => p == null || p.slug == slug);
            state.postponedUntil.Add(new PostponedSurvey { slug = slug, day = tomorrow });
            state.lastSurveyDay = TodayLocal;
            SaveCycleState(state);
        }

        public void SkipSurvey(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return;

            var state = GetCurrentCycleState();
            if (!state.skippedSlugsInCycle.Contains(slug))
            {
                state.skippedSlugsInCycle.Add(slug);
            }
            state.lastSurveyDay = TodayLocal;
            SaveCycleState(state);

            CheckAndAdvanceCycleIfCompleted(state);
        }

        public async Task<bool> MarkSurveyCompletedAsync(string slug, string surveyId)
        {
            if (string.IsNullOrEmpty(slug)) return false;

            var state = GetCurrentCycleState();
            if (!state.completedSlugsInCycle.Contains(slug))
            {
                state.completedSlugsInCycle.Add(slug);
            }
            state.lastSurveyDay = TodayLocal;
            SaveCycleState(state);

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

            var state = GetCurrentCycleState();
            int activeDays = state.activeDatesInCycle.Count;
            int daysSinceStart = GetDaysSinceCurrentCycleStart();

            //Debug.Log($"[{GetType().Name}] 📊 Estado del Ciclo {state.currentCycle}: Días activos={activeDays}, Días transcurridos={daysSinceStart}, Completadas=[{string.Join(", ", state.completedSlugsInCycle)}], Saltadas=[{string.Join(", ", state.skippedSlugsInCycle)}]");

            // Never two surveys the same day: answering, declining or postponing one closes the day
            if (state.lastSurveyDay == TodayLocal)
            {
                return null;
            }

            foreach (var rule in s_Rules)
            {
                if (state.completedSlugsInCycle.Contains(rule.Slug) || state.skippedSlugsInCycle.Contains(rule.Slug))
                {
                    continue;
                }

                // The first pending survey of the cycle: every later one waits for it to be answered or declined
                if (IsPostponed(state, rule.Slug))
                {
                    return null;
                }

                bool daysOk = activeDays >= rule.MinActiveDaysInCycle;
                bool elapsedOk = daysSinceStart >= rule.MinDaysSinceCycleStart;
                if (!daysOk || !elapsedOk)
                {
                    Debug.Log($"[{GetType().Name}] ⏳ Regla '{rule.Slug}' aún no cumple requisitos: (Días activos: {activeDays}/{rule.MinActiveDaysInCycle}, Días transcurridos: {daysSinceStart}/{rule.MinDaysSinceCycleStart})");
                    return null;
                }

                var (survey, error) = await _surveyService.GetSurveyBySlugAsync(rule.Slug, lang);
                if (survey != null && survey.questions != null && survey.questions.Length > 0)
                {
                    return survey;
                }
                if (error != null)
                {
                    // Try again later: skipping ahead on a network error would break the order
                    Debug.LogWarning($"[{GetType().Name}] ⚠️ Error del backend al obtener survey '{rule.Slug}': {error.message} (status: {error.statusCode})");
                    return null;
                }

                // Missing on the backend (or without questions): it can never be answered, so it doesn't block the next one
                Debug.LogWarning($"[{GetType().Name}] ⚠️ La encuesta '{rule.Slug}' no existe en el backend o no tiene preguntas.");
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

            state.lastSurveyDay = "";
            SaveCycleState(state);
            Debug.Log($"[{GetType().Name}] [DEBUG] Set activeDays={activeDaysCount}, daysSinceStart={daysSinceStart}, startDate={state.cycleStartDate}");
        }

        private bool IsPostponed(PilotSurveyCycleState state, string slug)
        {
            string today = TodayLocal;
            return state.postponedUntil.Any(p => p != null && p.slug == slug && string.CompareOrdinal(today, p.day ?? "") < 0);
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
            state.postponedUntil.Clear();
            state.lastSurveyDay = "";
            SaveCycleState(state);
            Debug.Log($"[{GetType().Name}] [DEBUG] Reset completed/skipped surveys in Cycle {state.currentCycle}");
        }
    }
}
