using System;
using System.Globalization;

using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Development-only tool to test missions over several days: shifts the timestamp of every meal log the app creates
    /// by <see cref="Days"/> days. Only applied in the Editor and development builds (see MealLogService.CreateAsync).
    /// The backend only uses it for metadata.mealDayBucket; rules windowed on createdAt or dayBucket are not affected.
    /// </summary>
    public static class DevMealDayOffset
    {
        public const int MaxDays = 6;

        /// <summary>Offset in days (0 = no shift). Kept in memory only, so it resets to 0 on every Play session.</summary>
        public static int Days { get; set; }

        public static void Cycle()
        {
            Days = Days >= MaxDays ? 0 : Days + 1;
        }

        /// <summary>Returns a shifted copy (the request is not mutated, so a retry never shifts twice).</summary>
        public static CreateMealLogRequest ApplyTo(CreateMealLogRequest request, DateTime utcNow)
        {
            if (request == null || Days == 0)
            {
                return request;
            }

            var copy = JsonConvert.DeserializeObject<CreateMealLogRequest>(JsonConvert.SerializeObject(request));
            DateTime baseUtc = string.IsNullOrEmpty(request.timestamp)
                ? utcNow
                : DateTime.Parse(request.timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            copy.timestamp = baseUtc.AddDays(Days).ToString("o", CultureInfo.InvariantCulture);
            return copy;
        }
    }
}
