

using System;
using System.Collections.Generic;
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
        private VisualElement _itemsContainer;
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
            _mealName.AddToClassList("fm-meal-card-title");
            this.Add(_mealName);

            _itemsContainer = new VisualElement();
            _itemsContainer.AddToClassList("fm-meal-card-items");
            this.Add(_itemsContainer);

            _badge = new Unity.AppUI.UI.Text();
            _badge.AddToClassList("fm-ml-card-badge");
            this.Add(_badge);

            UpdateMealLogData();
        }

        public void SetItems(MealItemDetail[] items)
        {
            if (_mealLogData?.meal != null)
            {
                _mealLogData.meal.items = items;
            }
            UpdateMealLogData();
        }

        private void UpdateMealLogData()
        {
            if (_mealLogData != null)
            {
                string emoji = MealLogHelpers.GetEmojiForTypeOfMeal(_mealLogData.typeOfMeal);
                string label = _typeLabel ?? _mealLogData.typeOfMeal;

                _heading.text = $"{emoji} {label} - {DateTime.Parse(_mealLogData.timestamp).ToLocalTime():g}";
                _mealName.text = $"{_mealLogData.meal?.name ?? "Meal"}";

                _itemsContainer.Clear();

                MealItemDetail[] items = _mealLogData.meal?.items;
                if (items != null && items.Length > 0)
                {
                    foreach (MealItemDetail item in items)
                    {
                        VisualElement row = new VisualElement();
                        row.AddToClassList("fm-meal-card-item-row");

                        string itemName = item.foodProduct?.name ?? item.genericFood?.foodName ?? item.notes;
                        if (string.IsNullOrEmpty(itemName))
                        {
                            itemName = "@UI:UNKNOWN";
                        }

                        Unity.AppUI.UI.Text nameLabel = new Unity.AppUI.UI.Text();
                        nameLabel.AddToClassList("fm-meal-card-item-name");
                        nameLabel.text = $"• {itemName}";
                        row.Add(nameLabel);

                        // By now we're not showing quuantities
                        // if (item.quantity.HasValue && item.quantity.Value > 0)
                        // {
                        //     string unitStr = !string.IsNullOrEmpty(item.unit) ? $" {item.unit}" : "";
                        //     Unity.AppUI.UI.Text qtyLabel = new Unity.AppUI.UI.Text();
                        //     qtyLabel.AddToClassList("fm-meal-card-item-qty");
                        //     qtyLabel.text = $"{item.quantity.Value}{unitStr}";
                        //     row.Add(qtyLabel);
                        // }

                        _itemsContainer.Add(row);
                    }
                    _itemsContainer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    // Unity.AppUI.UI.Text emptyLabel = new Unity.AppUI.UI.Text();
                    // emptyLabel.AddToClassList("fm-meal-card-item-empty");
                    // emptyLabel.text = "@UI:txtNO_ITEMS_SPECIFIED";
                    // _itemsContainer.Add(emptyLabel);
                    _itemsContainer.style.display = DisplayStyle.Flex;
                }

                _badge.RemoveFromClassList("fm-ml-card-badge--pantry");
                _badge.RemoveFromClassList("fm-ml-card-badge--out");

                if (_mealLogData.mealFromPantry)
                {
                    _badge.text = "@UI:From_Pantry";
                    _badge.AddToClassList("fm-ml-card-badge--pantry");
                }
                else if (_mealLogData.eatenOut)
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