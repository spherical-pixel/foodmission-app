using System;
using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Local storage service using PlayerPrefs.
    /// Provides data persistence with error handling.
    /// </summary>
    public class LocalStorageService : ILocalStorageService
    {
        /// <summary>
        /// Prefix for all Foodmission keys to avoid collisions.
        /// </summary>
        private const string KEY_PREFIX = "FM_";

        /// <summary>
        /// Gets a value from local storage.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="defaultValue">Default value if it doesn't exist</param>
        /// <returns>The stored value or the default value</returns>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            var fullKey = GetFullKey(key);

            try
            {
                if (!PlayerPrefs.HasKey(fullKey))
                {
                    return defaultValue;
                }

                var json = PlayerPrefs.GetString(fullKey);

                if (string.IsNullOrEmpty(json))
                {
                    return defaultValue;
                }

                // For primitive types, use directly
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return ConvertPrimitive<T>(json, defaultValue);
                }

                // For complex objects, use JsonUtility
                var result = JsonUtility.FromJson<T>(json);
                return result ?? defaultValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalStorageService] Error reading key '{fullKey}': {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Saves a value to local storage.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value to save</param>
        public void SetValue<T>(string key, T value)
        {
            var fullKey = GetFullKey(key);

            try
            {
                if (value == null)
                {
                    PlayerPrefs.DeleteKey(fullKey);
                    PlayerPrefs.Save();
                    Debug.Log($"[LocalStorageService] Deleted key '{fullKey}' (null value)");
                    return;
                }

                string json;

                // For primitive types, convert directly
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    json = value.ToString();
                }
                else
                {
                    // For complex objects, use JsonUtility
                    json = JsonUtility.ToJson(value);
                }

                PlayerPrefs.SetString(fullKey, json);
                PlayerPrefs.Save();

                Debug.Log($"[LocalStorageService] Saved key '{fullKey}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalStorageService] Error saving key '{fullKey}': {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a value from local storage.
        /// </summary>
        /// <param name="key">The key of the value to delete</param>
        public void DeleteValue(string key)
        {
            var fullKey = GetFullKey(key);

            try
            {
                PlayerPrefs.DeleteKey(fullKey);
                PlayerPrefs.Save();
                Debug.Log($"[LocalStorageService] Deleted key '{fullKey}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalStorageService] Error deleting key '{fullKey}': {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists</returns>
        public bool HasValue(string key)
        {
            return PlayerPrefs.HasKey(GetFullKey(key));
        }

        /// <summary>
        /// Deletes all Foodmission keys.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                // PlayerPrefs doesn't support prefixes, so we just log
                Debug.LogWarning("[LocalStorageService] ClearAll not implemented for PlayerPrefs. Use DeleteValue for specific keys.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalStorageService] Error clearing storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the full key with prefix.
        /// </summary>
        private static string GetFullKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
            return KEY_PREFIX + key;
        }

        /// <summary>
        /// Converts a string to a primitive type.
        /// </summary>
        private static T ConvertPrimitive<T>(string value, T defaultValue)
        {
            try
            {
                var type = typeof(T);

                if (type == typeof(string))
                    return (T)(object)value;
                if (type == typeof(int))
                    return (T)(object)int.Parse(value);
                if (type == typeof(float))
                    return (T)(object)float.Parse(value);
                if (type == typeof(bool))
                    return (T)(object)bool.Parse(value);
                if (type == typeof(long))
                    return (T)(object)long.Parse(value);
                if (type == typeof(double))
                    return (T)(object)double.Parse(value);

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
