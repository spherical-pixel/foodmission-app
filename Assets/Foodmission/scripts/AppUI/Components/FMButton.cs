using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Properties;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMButton : Unity.AppUI.UI.Button
    {
        
        public FMButton()
        {
            passMask = Passes.Clear | Passes.Outline | Passes.OutsetShadows | Passes.InsetShadows;
            RegisterCallback<FocusInEvent>(OnFocusIn);
            
        }
        private void OnFocusIn(FocusInEvent evt)
        {
            passMask = Passes.Clear | Passes.Outline | Passes.OutsetShadows;
        }
    }
}