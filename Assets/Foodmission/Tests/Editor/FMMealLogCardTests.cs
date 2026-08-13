using NUnit.Framework;
using UnityEngine.UIElements;
using eu.foodmission.platform.Components;
using Unity.AppUI.UI;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMMealLogCardTests
    {
        [Test]
        public void FMMealLogCard_WithItemsAndQuantities_RendersRowsAndQuantities()
        {
            var log = new MealLog
            {
                id = "log-100",
                typeOfMeal = "LUNCH",
                timestamp = "2026-08-13T12:00:00Z",
                mealFromPantry = true,
                eatenOut = false,
                meal = new Meal
                {
                    id = "meal-100",
                    name = "Tostada con Aguacate",
                    items = new[]
                    {
                        new MealItemDetail
                        {
                            id = "item-1",
                            quantity = 200,
                            unit = "g",
                            foodProduct = new MealItemFoodProduct { name = "Pan de molde" }
                        },
                        new MealItemDetail
                        {
                            id = "item-2",
                            quantity = 1,
                            unit = "ud",
                            genericFood = new MealItemGenericFood { foodName = "Aguacate" }
                        }
                    }
                }
            };

            var card = new FMMealLogCard
            {
                MealLogData = log,
                TypeLabel = "Almuerzo"
            };

            var itemsContainer = card.Q(className: "fm-meal-card-items");
            Assert.IsNotNull(itemsContainer, "itemsContainer visual element should exist");
            Assert.AreEqual(DisplayStyle.Flex, itemsContainer.style.display.value);

            var rows = itemsContainer.Query(className: "fm-meal-card-item-row").ToList();
            Assert.AreEqual(2, rows.Count, "Should render 2 item rows");

            var name1 = rows[0].Q<Text>(className: "fm-meal-card-item-name");
            var qty1 = rows[0].Q<Text>(className: "fm-meal-card-item-qty");
            Assert.IsNotNull(name1);
            Assert.IsNotNull(qty1);
            Assert.AreEqual("• Pan de molde", name1.text);
            Assert.AreEqual("200 g", qty1.text);

            var name2 = rows[1].Q<Text>(className: "fm-meal-card-item-name");
            var qty2 = rows[1].Q<Text>(className: "fm-meal-card-item-qty");
            Assert.IsNotNull(name2);
            Assert.IsNotNull(qty2);
            Assert.AreEqual("• Aguacate", name2.text);
            Assert.AreEqual("1 ud", qty2.text);
        }

        [Test]
        public void FMMealLogCard_WithItemsWithoutQuantities_RendersNamesOnly()
        {
            var log = new MealLog
            {
                id = "log-101",
                typeOfMeal = "DINNER",
                timestamp = "2026-08-13T20:00:00Z",
                meal = new Meal
                {
                    id = "meal-101",
                    name = "Ensalada",
                    items = new[]
                    {
                        new MealItemDetail
                        {
                            id = "item-3",
                            quantity = null,
                            genericFood = new MealItemGenericFood { foodName = "Lechuga" }
                        }
                    }
                }
            };

            var card = new FMMealLogCard { MealLogData = log };

            var itemsContainer = card.Q(className: "fm-meal-card-items");
            Assert.IsNotNull(itemsContainer);

            var row = itemsContainer.Q(className: "fm-meal-card-item-row");
            Assert.IsNotNull(row);

            var name = row.Q<Text>(className: "fm-meal-card-item-name");
            var qty = row.Q<Text>(className: "fm-meal-card-item-qty");

            Assert.IsNotNull(name);
            Assert.AreEqual("• Lechuga", name.text);
            Assert.IsNull(qty, "Quantity badge should not be rendered when quantity is null");
        }

        [Test]
        public void FMMealLogCard_WithoutItems_DisplaysEmptyPlaceholder()
        {
            var log = new MealLog
            {
                id = "log-102",
                typeOfMeal = "SNACK",
                timestamp = "2026-08-13T17:00:00Z",
                meal = new Meal
                {
                    id = "meal-102",
                    name = "Manzana",
                    items = System.Array.Empty<MealItemDetail>()
                }
            };

            var card = new FMMealLogCard { MealLogData = log };

            var itemsContainer = card.Q(className: "fm-meal-card-items");
            Assert.IsNotNull(itemsContainer);

            var emptyLabel = itemsContainer.Q<Text>(className: "fm-meal-card-item-empty");
            Assert.IsNotNull(emptyLabel);
            Assert.AreEqual("@UI:txtNO_ITEMS_SPECIFIED", emptyLabel.text);
        }

        [Test]
        public void FMMealLogCard_SetItems_DynamicallyUpdatesCard()
        {
            var log = new MealLog
            {
                id = "log-103",
                typeOfMeal = "BREAKFAST",
                timestamp = "2026-08-13T08:00:00Z",
                meal = new Meal
                {
                    id = "meal-103",
                    name = "Café con leche"
                }
            };

            var card = new FMMealLogCard { MealLogData = log };

            var initialEmpty = card.Q<Text>(className: "fm-meal-card-item-empty");
            Assert.IsNotNull(initialEmpty);

            card.SetItems(new[]
            {
                new MealItemDetail
                {
                    id = "item-4",
                    quantity = 150,
                    unit = "ml",
                    foodProduct = new MealItemFoodProduct { name = "Leche entera" }
                }
            });

            var updatedRow = card.Q(className: "fm-meal-card-item-row");
            Assert.IsNotNull(updatedRow);
            var updatedName = updatedRow.Q<Text>(className: "fm-meal-card-item-name");
            var updatedQty = updatedRow.Q<Text>(className: "fm-meal-card-item-qty");
            Assert.AreEqual("• Leche entera", updatedName.text);
            Assert.AreEqual("150 ml", updatedQty.text);
        }
    }
}
