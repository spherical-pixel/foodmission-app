// UxmlDestinationTemplate.cs
using System;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    [Serializable]
    public class UxmlDestinationTemplate : DefaultDestinationTemplate
    {
        [SerializeField]
        [Tooltip("The UXML asset to instantiate when the destination is reached.")]
        VisualTreeAsset m_UxmlAsset;

        public VisualTreeAsset uxmlAsset
        {
            get => m_UxmlAsset;
            set => m_UxmlAsset = value;
        }

        public override INavigationScreen CreateScreen(NavHost host)
        {
            // Instantiate the screen here, or use a factory method if you have a more complex setup
            // (like caching screen per destination).
            var screen = new UxmlNavigationScreen(uxmlAsset, host);
            return screen;
        }
    }
}
