using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace eu.foodmission.platform.Editor
{
    /// <summary>
    /// Simulates the passing of days for the logged-in user: runs _desarrollo/local-env/advance-day.sh on the local
    /// backend and moves the dates the app keeps on the device by the same amount (check-in nudge, survey cycle,
    /// acknowledged failures, celebration cursor). Works in and out of Play Mode; in Play Mode re-enter Home.
    /// </summary>
    public static class DevTimeTravelMenu
    {
        private const string ScriptRelativePath = "_desarrollo/local-env/advance-day.sh";
        private const string LocalEnvironmentName = "Local";
        private const int ScriptTimeoutMs = 60000;

        [MenuItem("Foodmission/Dev/Advance 1 day")]
        private static void Advance1() => Advance(1);

        [MenuItem("Foodmission/Dev/Advance 3 days")]
        private static void Advance3() => Advance(3);

        [MenuItem("Foodmission/Dev/Advance 7 days")]
        private static void Advance7() => Advance(7);

        public static void Advance(int days)
        {
            try
            {
                string environment = ApiConfig.Environments[ApiConfig.ActiveEnvironmentIndex].Name;
                if (environment != LocalEnvironmentName)
                {
                    Show($"The app points to '{environment}'. advance-day.sh only works on the local backend: switch the API environment to Local first.");
                    return;
                }

                var storage = new LocalStorageService();
                AppState state = storage.GetValue<AppState>(StoreService.APP_STATE_KEY, null);
                if (string.IsNullOrEmpty(state?.userId) || string.IsNullOrEmpty(state.userEmail))
                {
                    Show("No logged-in user with an email in the saved app state. Log in on the Local environment first.");
                    return;
                }

                string script = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", ScriptRelativePath);
                if (!File.Exists(script))
                {
                    Show($"Script not found: {script}");
                    return;
                }

                if (!RunScript(script, state.userEmail, days, out string output))
                {
                    Show($"advance-day.sh failed, local dates were not changed.\n\n{Tail(output, 1200)}");
                    return;
                }

                List<string> shifted = ShiftLocalDates(storage, state.userId, days);
                string backend = string.Join("\n", output.Split('\n').Where(l => l.StartsWith("UPDATE", StringComparison.Ordinal) || l.StartsWith("User:", StringComparison.Ordinal)));
                string local = shifted.Count > 0 ? string.Join("\n", shifted) : "(no local state to shift)";
                Debug.Log($"[DevTimeTravelMenu] Advanced {days} day(s) for {state.userEmail}\n{backend}\nLocal: {string.Join(", ", shifted)}");
                Show($"Advanced {days} day(s) for {state.userEmail}.\n\nBackend:\n{backend}\n\nLocal:\n{local}\n\nIn Play Mode, leave Home and enter it again.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevTimeTravelMenu] Advance failed: {ex}");
                Show($"Advance failed: {ex.Message}");
            }
        }

        private static List<string> ShiftLocalDates(LocalStorageService storage, string userId, int days)
        {
            var shifted = new List<string>();
            ShiftStored(storage, MissionNudgeService.StorageKey(userId), days, MissionNudgeService.ShiftStoredDates, "check-in nudge", shifted);
            ShiftStored(storage, MissionFailureService.StorageKey(userId), days, MissionFailureService.ShiftStoredDates, "acknowledged failures", shifted);
            ShiftStored(storage, PilotSurveyService.CycleStorageKeyFor(userId), days, PilotSurveyService.ShiftStoredDates, "survey cycle", shifted);
            ShiftStored(storage, DailyFoodFactService.StorageKey(userId), days, DailyFoodFactService.ShiftStoredDates, "daily food fact", shifted);

            // The celebration cursor is a raw PlayerPrefs key (no FM_ prefix)
            string cursorKey = HomeScreenViewModel.CelebrationCursorKey(userId);
            string cursor = PlayerPrefs.GetString(cursorKey, "");
            string movedCursor = HomeScreenViewModel.ShiftCelebrationCursor(cursor, days);
            if (movedCursor != cursor)
            {
                PlayerPrefs.SetString(cursorKey, movedCursor);
                shifted.Add("celebration cursor");
            }

            PlayerPrefs.Save();
            return shifted;
        }

        private static void ShiftStored(LocalStorageService storage, string key, int days, Func<string, int, string> shift, string label, List<string> shifted)
        {
            string json = storage.GetValue<string>(key, null);
            string moved = shift(json, days);
            if (moved != json)
            {
                storage.SetValue(key, moved);
                shifted.Add(label);
            }
        }

        private static bool RunScript(string script, string email, int days, out string output)
        {
            var info = new ProcessStartInfo("/bin/bash", $"{Quote(script)} {Quote(email)} {days} --apply")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(script) ?? ""
            };
            // The Editor does not inherit the login shell PATH: docker lives in one of these
            info.Environment["PATH"] = "/usr/local/bin:/opt/homebrew/bin:/Applications/Docker.app/Contents/Resources/bin:"
                + (Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin");

            using (var process = Process.Start(info))
            {
                if (process == null)
                {
                    output = "Could not start bash.";
                    return false;
                }

                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                process.OutputDataReceived += (_, e) => { if (e.Data != null) { stdout.AppendLine(e.Data); } };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) { stderr.AppendLine(e.Data); } };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(ScriptTimeoutMs))
                {
                    process.Kill();
                    output = "Timed out after 60 s.";
                    return false;
                }
                process.WaitForExit();

                output = stdout.ToString() + stderr;
                // The script rolls back with exit 0 when the user does not exist
                return process.ExitCode == 0 && output.Contains("COMMIT");
            }
        }

        private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static string Tail(string text, int max) => text.Length <= max ? text : "…" + text.Substring(text.Length - max);

        private static void Show(string message) => EditorUtility.DisplayDialog("Dev time travel", message, "OK");
    }
}
