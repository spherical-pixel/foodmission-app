

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
        private VisualElement _headerRow;
        private Heading _heading;
        private Unity.AppUI.UI.Button _editButton;
        private Unity.AppUI.UI.Button _removeButton;
        private Unity.AppUI.UI.Text _mealName;
        private VisualElement _itemsContainer;
        private Unity.AppUI.UI.Text _badge;

        public Unity.AppUI.UI.Button EditButton => _editButton;
        public Unity.AppUI.UI.Button RemoveButton => _removeButton;

        public FMMealLogCard()
        {
            this.AddToClassList("fm-meal-card");

            _headerRow = new VisualElement();
            _headerRow.AddToClassList("fm-meal-card-header-row");

            _heading = new Heading();
            _heading.size = HeadingSize.S;
            _heading.AddToClassList("bold-text");
            _heading.AddToClassList("fm-meal-card-heading");
            _heading.style.flexGrow = 1;
            _heading.style.flexShrink = 1;
            _headerRow.Add(_heading);

            var actionsContainer = new VisualElement();
            actionsContainer.style.width = Length.Percent(100);
            actionsContainer.style.flexDirection = FlexDirection.Row;
            actionsContainer.style.alignItems = Align.FlexEnd;
            actionsContainer.style.justifyContent = Justify.FlexEnd;

            _editButton = new Unity.AppUI.UI.Button();
            _editButton.quiet = true;
            _editButton.leadingIcon = "fm-edit";
            _editButton.size = Size.S;
            _editButton.AddToClassList("fm-icon-button-item-list");
            _editButton.AddToClassList("fm-meal-card-edit-btn");
            actionsContainer.Add(_editButton);

            _removeButton = new Unity.AppUI.UI.Button();
            _removeButton.quiet = true;
            _removeButton.leadingIcon = "fm-trash";
            _removeButton.size = Size.S;
            _removeButton.AddToClassList("fm-icon-button-item-list");
            _removeButton.AddToClassList("fm-meal-card-remove-btn");
            actionsContainer.Add(_removeButton);

            //_headerRow.Add(actionsContainer);


            this.Add(_headerRow);

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

            this.Add(actionsContainer);

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
                if (_mealLogData.meal != null && !string.IsNullOrEmpty(_mealLogData.meal.name))
                {
                    _mealName.text = _mealLogData.meal.name;
                }
                else
                {
                    _mealName.text = $"{emoji} {label}";
                }

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

                        if (item.quantity.HasValue && item.quantity.Value > 0)
                        {
                            string unitStr = !string.IsNullOrEmpty(item.unit) ? $" {item.unit}" : "";
                            Unity.AppUI.UI.Text qtyLabel = new Unity.AppUI.UI.Text();
                            qtyLabel.AddToClassList("fm-meal-card-item-qty");
                            qtyLabel.text = $"{item.quantity.Value}{unitStr}";
                            row.Add(qtyLabel);
                        }

                        _itemsContainer.Add(row);
                    }
                    _itemsContainer.style.display = DisplayStyle.Flex;
                }
                else if ((_mealLogData.flags != null && _mealLogData.flags.Length > 0) ||
                         (_mealLogData.swaps != null && _mealLogData.swaps.Length > 0))
                {
                    if (_mealLogData.flags != null)
                    {
                        foreach (string flag in _mealLogData.flags)
                        {
                            VisualElement row = new VisualElement();
                            row.AddToClassList("fm-meal-card-item-row");
                            Unity.AppUI.UI.Text nameLabel = new Unity.AppUI.UI.Text();
                            nameLabel.AddToClassList("fm-meal-card-item-name");
                            nameLabel.text = $"• {MealLogHelpers.GetDisplayNameForFlag(flag)}";
                            row.Add(nameLabel);
                            _itemsContainer.Add(row);
                        }
                    }
                    if (_mealLogData.swaps != null)
                    {
                        foreach (string swap in _mealLogData.swaps)
                        {
                            VisualElement row = new VisualElement();
                            row.AddToClassList("fm-meal-card-item-row");
                            Unity.AppUI.UI.Text nameLabel = new Unity.AppUI.UI.Text();
                            nameLabel.AddToClassList("fm-meal-card-item-name");
                            nameLabel.text = $"• {ActivityEventMapper.GetSwapDisplayName(swap)}";
                            row.Add(nameLabel);
                            _itemsContainer.Add(row);
                        }
                    }
                    _itemsContainer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    Unity.AppUI.UI.Text emptyLabel = new Unity.AppUI.UI.Text();
                    emptyLabel.AddToClassList("fm-meal-card-item-empty");
                    emptyLabel.text = "@UI:txtNO_ITEMS_SPECIFIED";
                    _itemsContainer.Add(emptyLabel);
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