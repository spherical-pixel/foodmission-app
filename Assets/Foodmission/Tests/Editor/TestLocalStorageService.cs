using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    public class TestLocalStorageService : ILocalStorageService
    {
        private readonly Dictionary<string, string> _store = new();
        private readonly Dictionary<string, object> _objectStore = new();

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_store.TryGetValue(key, out var v))
            {
                return (T)System.Convert.ChangeType(v, typeof(T));
            }
            if (_objectStore.TryGetValue(key, out var ov))
            {
                return (T)ov;
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            if (value is string s)
            {
                _store[key] = s;
            }
            else
            {
                _objectStore[key] = value;
            }
        }

        public void DeleteValue(string key)
        {
            _store.Remove(key);
            _objectStore.Remove(key);
        }

        public bool HasValue(string key) => _store.ContainsKey(key) || _objectStore.ContainsKey(key);

        public string GetString(string key, string defaultValue = "")
            => _store.TryGetValue(key, out var v) ? v : defaultValue;

        public void SetString(string key, string value)
            => _store[key] = value;

        public int GetInt(string key, int defaultValue = 0)
            => _store.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : defaultValue;

        public void SetInt(string key, int value)
            => _store[key] = value.ToString();

        public float GetFloat(string key, float defaultValue = 0f)
            => _store.TryGetValue(key, out var v) && float.TryParse(v, out var f) ? f : defaultValue;

        public void SetFloat(string key, float value)
            => _store[key] = value.ToString();

        public bool GetBool(string key, bool defaultValue = false)
            => _store.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : defaultValue;

        public void SetBool(string key, bool value)
            => _store[key] = value.ToString();

        public string GetObject<T>(string key) where T : class
            => _objectStore.TryGetValue(key, out var v) ? JsonUtility.ToJson(v) : null;

        public void SetObject(string key, object value)
            => _objectStore[key] = value;

        public void DeleteKey(string key)
        {
            _store.Remove(key);
            _objectStore.Remove(key);
        }

        public bool HasKey(string key) => _store.ContainsKey(key) || _objectStore.ContainsKey(key);

        public void Save() { }

        public void DeleteAll()
        {
            _store.Clear();
            _objectStore.Clear();
        }
    }
}
