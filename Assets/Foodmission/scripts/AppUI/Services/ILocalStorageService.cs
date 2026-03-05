namespace eu.foodmission.platform
{
    /// <summary>
    /// Interface for local storage service.
    /// Provides simple data persistence with error handling.
    /// </summary>
    public interface ILocalStorageService
    {
        /// <summary>
        /// Gets a value from local storage.
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// Saves a value to local storage.
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Deletes a value from local storage.
        /// </summary>
        void DeleteValue(string key);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        bool HasValue(string key);
    }
}
