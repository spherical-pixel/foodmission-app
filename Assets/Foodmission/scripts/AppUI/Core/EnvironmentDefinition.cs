using System;

namespace eu.foodmission.platform
{
    [Serializable]
    public class EnvironmentDefinition
    {
        public string Name;
        public string ApiBaseUrl;
        public string AuthBaseUrl;
        public bool UseDirectOpenFoodFactsClient;
        public string OpenFoodFactsBaseUrl;
        public int OpenFoodFactsSearchMinIntervalMs;
        public int OpenFoodFactsSearchCacheTtlMinutes;
        public int OpenFoodFactsSearchPageSize;
    }
}
