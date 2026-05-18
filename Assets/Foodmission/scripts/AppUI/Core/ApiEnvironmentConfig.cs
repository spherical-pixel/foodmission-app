using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    [CreateAssetMenu(menuName = "Foodmission/API Environment Config", fileName = "ApiEnvironmentConfig")]
    public class ApiEnvironmentConfig : ScriptableObject
    {
        [SerializeField] private int m_ActiveIndex;
        [SerializeField] private List<EnvironmentDefinition> m_Environments = new()
        {
            new()
            {
                Name = "Staging",
                ApiBaseUrl = "https://staging.api.foodmission.eu",
                AuthBaseUrl = "https://staging.auth.foodmission.eu",
            },
            new()
            {
                Name = "Test",
                ApiBaseUrl = "https://test.api.foodmission.eu",
                AuthBaseUrl = "https://test.auth.foodmission.eu",
            },
            new()
            {
                Name = "Local",
                ApiBaseUrl = "http://localhost:3000",
                AuthBaseUrl = "http://localhost:8080",
            },
        };

        public int ActiveIndex => m_ActiveIndex;
        public IReadOnlyList<EnvironmentDefinition> Environments => m_Environments;

        public EnvironmentDefinition ActiveEnvironment
        {
            get
            {
                if (m_Environments == null || m_Environments.Count == 0)
                    return null;
                if (m_ActiveIndex < 0 || m_ActiveIndex >= m_Environments.Count)
                    return m_Environments[0];
                return m_Environments[m_ActiveIndex];
            }
        }
    }
}
