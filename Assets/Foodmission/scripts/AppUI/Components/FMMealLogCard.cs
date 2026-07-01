

using System;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FMMealLogCard : ExVisualElement
    {
        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("mealLog-Data")]
        [CreateProperty]
        public MealLog MealLogData
        {
            get => _mealLogData;
            set
            {
                _mealLogData = value;
                UpdateMealLogData();
            }
        }

        [UxmlAttribute("typeLabel")]
        [CreateProperty]
        public string TypeLabel
        {
            get => _typeLabel;
            set
            {
                _typeLabel = value;
                UpdateMealLogData();
            }
        }


        /* ========= INTERNAL ELEMENTS ========= */
        private MealLog _mealLogData;
        private string _typeLabel;
        private Heading _heading;
        private Unity.AppUI.UI.Text _mealName;
        private Unity.AppUI.UI.Text _mealInfo;
        private Unity.AppUI.UI.Text _badge;

        public FMMealLogCard()
        {
            this.AddToClassList("fm-meal-card");

            _heading = new Heading();
            _heading.size = HeadingSize.M;
            _heading.AddToClassList("bold-text");
            _heading.AddToClassList("fm-meal-card-heading");
            _heading.style.paddingBottom = 8;
            this.Add(_heading);

            _mealName = new Unity.AppUI.UI.Text();
            _mealName.AddToClassList("fm-meal-card-text");
            this.Add(_mealName);

            _mealInfo = new Unity.AppUI.UI.Text();
            _mealInfo.AddToClassList("fm-meal-card-text");
            this.Add(_mealInfo);

            _badge = new Unity.AppUI.UI.Text();
            _badge.AddToClassList("fm-ml-card-badge");
            this.Add(_badge);

            UpdateMealLogData();

        }

        private void UpdateMealLogData()
        {
            if( _mealLogData != null)
            {
                string emoji = MealLogHelpers.GetEmojiForTypeOfMeal(_mealLogData.typeOfMeal);
                string label = _typeLabel ?? _mealLogData.typeOfMeal;
                int calories = (int)(_mealLogData.meal?.calories ?? 0f);
                int protein = (int)(_mealLogData.meal?.proteins ?? 0f);

                _heading.text = $"{emoji} {label} - {DateTime.Parse(_mealLogData.timestamp).ToLocalTime():g}";
                _mealName.text = $"{_mealLogData.meal?.name ?? "Meal"}";

                if (_mealLogData.meal != null)
                {
                    _mealInfo.text = $"{calories} kcal | {protein}g protein";
                    _mealInfo.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _mealInfo.style.display = DisplayStyle.None;
                }
                
                _badge.RemoveFromClassList("fm-ml-card-badge--pantry");
                _badge.RemoveFromClassList("fm-ml-card-badge--out");

                if( _mealLogData.mealFromPantry)
                {
                    _badge.text = "@UI:From_Pantry";
                    _badge.AddToClassList("fm-ml-card-badge--pantry");
                    
                }
                else if( _mealLogData.eatenOut)
                {
                    _badge.text = "@UI:Eaten_Out";
                    _badge.AddToClassList("fm-ml-card-badge--out");
                    
                }
                else
                {
                    _badge.text = "";
                }

            }
        }
        
    }
}