using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace eu.foodmission.platform.Editor
{
    public static class ExportLocalizationToJson
    {
        private const string OutputPath = "version-check/localization-overrides.json";

        [MenuItem("Foodmission/Export Localization/To JSON")]
        public static void Export()
        {
            try
            {
                ExportImpl();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ExportLocalizationToJson] Export failed: {ex.Message}");
                EditorUtility.DisplayDialog("Export Failed", $"Could not export localization: {ex.Message}", "OK");
            }
        }

        private static void ExportImpl()
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                Debug.LogError("[ExportLocalizationToJson] LocalizationSettings.AvailableLocales is null");
                return;
            }

            var collections = LocalizationEditorSettings.GetStringTableCollections();
            var strings = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            foreach (var collection in collections)
            {
                var tableName = collection.TableCollectionName;
                var tableStrings = new Dictionary<string, Dictionary<string, string>>();

                foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    var localeCode = locale.Identifier.Code;
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    if (table == null) continue;

                    var localeEntries = new Dictionary<string, string>();
                    foreach (var entry in table.Values)
                    {
                        var key = collection.SharedData.GetKey(entry.KeyId);
                        if (!string.IsNullOrEmpty(key))
                        {
                            localeEntries[key] = entry.Value;
                        }
                    }
                    tableStrings[localeCode] = localeEntries;
                }
                strings[tableName] = tableStrings;
            }

            int version = 1;
            if (File.Exists(OutputPath))
            {
                try
                {
                    var existing = Newtonsoft.Json.JsonConvert.DeserializeObject<ExportData>(File.ReadAllText(OutputPath));
                    if (existing != null)
                        version = existing.version + 1;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ExportLocalizationToJson] Could not read existing file for version increment: {ex.Message}");
                }
            }

            var export = new ExportData
            {
                version = version,
                minAppVersion = Application.version,
                generated = System.DateTime.Now.ToString("yyyy-MM-dd"),
                strings = strings
            };

            var dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(export, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(OutputPath, json);
            Debug.Log($"[ExportLocalizationToJson] Exported v{version} ({strings.Count} tables) to {OutputPath}");
            AssetDatabase.Refresh();
        }

        [System.Serializable]
        private class ExportData
        {
            public int version;
            public string minAppVersion;
            public string generated;
            public Dictionary<string, Dictionary<string, Dictionary<string, string>>> strings;
        }
    }
}
