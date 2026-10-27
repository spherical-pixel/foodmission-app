using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Meal flags and swaps the user can report on a meal log (Quick Meal Log and the detailed log's review step).
    /// Pure: works on the sections/items it is given; callers own the lists.
    /// </summary>
    public static class MealFacts
    {
        public static List<QuickMealSection> BuildStandardSections()
        {
            var sections = new List<QuickMealSection>();

            // 1. Hábitos y Plato Sostenible (expandido por defecto)
            var dietItems = new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_meat_free",
                    Icon = "🥗",
                    Prompt = "@UI:EVENTS_MEAT_FREE",
                    EventType = ClientEventTypes.MealMeatFree
                },
                new QuickMealCheckItem
                {
                    Id = "q_legumes",
                    Icon = "🫘",
                    Prompt = "@UI:EVENTS_LEGUMES_CONSUMED",
                    EventType = ClientEventTypes.MealLegumeConsumed
                },
                new QuickMealCheckItem
                {
                    Id = "q_vegan",
                    Icon = "🌿",
                    Prompt = "@UI:EVENTS_VEGAN_MEAL",
                    EventType = ClientEventTypes.MealVegan
                },
                new QuickMealCheckItem
                {
                    Id = "q_sustainable_plate",
                    Icon = "🍽️",
                    Prompt = "@UI:EVENTS_SUSTAINABLE_PLATE",
                    EventType = ClientEventTypes.MealSustainablePlate
                },
                new QuickMealCheckItem
                {
                    Id = "q_ancient_grain",
                    Icon = "🌾",
                    Prompt = "@UI:EVENTS_ANCIENT_GRAIN",
                    EventType = ClientEventTypes.MealAncientGrain
                },
                new QuickMealCheckItem
                {
                    Id = "q_alternative_staple",
                    Icon = "🥔",
                    Prompt = "@UI:EVENTS_ALTERNATIVE_STAPLE",
                    EventType = ClientEventTypes.MealAlternativeStaple
                },
                new QuickMealCheckItem
                {
                    Id = "q_meat_consumed",
                    Icon = "🥩",
                    Prompt = "@UI:EVENTS_MEAT_CONSUMED",
                    EventType = ClientEventTypes.MealMeatConsumed
                }
            };
            sections.Add(new QuickMealSection
            {
                Id = "sec_diet",
                Title = "@UI:EVENTS_SECTIONS_DIET",
                Icon = "🌱",
                IsExpanded = false,
                Items = dietItems
            });

            // 2. Sustituciones (Swaps)
            var swapOptions = new[]
            {
                ClientEventTypes.SwapBeefToLegumes,
                ClientEventTypes.SwapBeefToChicken,
                ClientEventTypes.SwapBeefToPork,
                ClientEventTypes.SwapPorkToLegumes,
                ClientEventTypes.SwapPorkToChicken,
                ClientEventTypes.SwapChickenToLegumes,
                ClientEventTypes.SwapProcessedMeatToLegumes,
                ClientEventTypes.SwapReadyMealToHomecooked,
                ClientEventTypes.SwapSugaryDrinkToWater,
                ClientEventTypes.SwapSnackToFruitNuts,
                ClientEventTypes.SwapSugaryCerealToOats
            };

            var swapItems = swapOptions.Select(swap => new QuickMealCheckItem
            {
                Id = $"q_{swap.ToLowerInvariant()}",
                Icon = "🔄",
                Prompt = SwapLocalization.GetSwapLocalizationTag(swap) ?? SwapLocalization.GetSwapDisplayName(swap),
                EventType = swap,
                IsChecked = false
            }).ToList();

            sections.Add(new QuickMealSection
            {
                Id = "sec_swaps",
                Title = "@UI:EVENTS_SECTIONS_SWAPS",
                Icon = "🔄",
                IsExpanded = false,
                Items = swapItems
            });

            // 4. Nutrición y Salud
            var nutritionItems = new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q_fruit_veg",
                    Icon = "🥦",
                    Prompt = "@UI:EVENTS_FRUIT_VEG_SERVING",
                    EventType = ClientEventTypes.NutritionFruitVegServingAdded
                },
                new QuickMealCheckItem
                {
                    Id = "q_wholegrain",
                    Icon = "🍞",
                    Prompt = "@UI:EVENTS_WHOLEGRAIN",
                    EventType = ClientEventTypes.NutritionWholegrainChosen
                },
                new QuickMealCheckItem
                {
                    Id = "q_high_fibre",
                    Icon = "🌾",
                    Prompt = "@UI:EVENTS_HIGH_FIBRE",
                    EventType = ClientEventTypes.NutritionHighFibreMeal
                },
                new QuickMealCheckItem
                {
                    Id = "q_salt_free",
                    Icon = "🧂",
                    Prompt = "@UI:EVENTS_SALT_FREE",
                    EventType = ClientEventTypes.NutritionSaltFreeTable
                },
                new QuickMealCheckItem
                {
                    Id = "q_healthy_fat",
                    Icon = "🥑",
                    Prompt = "@UI:EVENTS_HEALTHY_FAT",
                    EventType = ClientEventTypes.NutritionHealthyFatChosen
                },
                new QuickMealCheckItem
                {
                    Id = "q_added_sugar_avoided",
                    Icon = "🍬",
                    Prompt = "@UI:EVENTS_ADDED_SUGAR_AVOIDED",
                    EventType = ClientEventTypes.NutritionAddedSugarAvoided
                },
                new QuickMealCheckItem
                {
                    Id = "q_protein_included",
                    Icon = "🍳",
                    Prompt = "@UI:" + MealFlagLabels.KeyFor(ClientEventTypes.NutritionProteinIncluded),
                    EventType = ClientEventTypes.NutritionProteinIncluded
                },
                new QuickMealCheckItem
                {
                    Id = "q_rainbow",
                    Icon = "🌈",
                    Prompt = "@UI:" + MealFlagLabels.KeyFor(ClientEventTypes.NutritionRainbowColoursLogged),
                    EventType = ClientEventTypes.NutritionRainbowColoursLogged
                }
            };
            // 3. Origen y temporada
            sections.Add(new QuickMealSection
            {
                Id = "sec_origin",
                Title = "@UI:EVENTS_SECTIONS_ORIGIN",
                Icon = "🌍",
                IsExpanded = false,
                Items = new List<QuickMealCheckItem>
                {
                    FlagItem("q_seasonal", "🍂", ClientEventTypes.MealSeasonalProduce),
                    FlagItem("q_local", "📍", ClientEventTypes.MealLocalProduce),
                    FlagItem("q_certified", "🏷️", ClientEventTypes.MealCertifiedProduct)
                }
            });

            // 5. Desperdicio
            sections.Add(new QuickMealSection
            {
                Id = "sec_waste",
                Title = "@UI:EVENTS_SECTIONS_WASTE",
                Icon = "♻️",
                IsExpanded = false,
                Items = new List<QuickMealCheckItem>
                {
                    FlagItem("q_half_plate", "🍽️", ClientEventTypes.FoodWasteHalfPlateSaved),
                    FlagItem("q_leftovers", "♻️", ClientEventTypes.FoodWasteFullPlateSaved),
                    FlagItem("q_expired", "📅", ClientEventTypes.FoodWasteExpiredConsumed)
                }
            });

            sections.Add(new QuickMealSection
            {
                Id = "sec_nutrition",
                Title = "@UI:EVENTS_SECTIONS_NUTRITION",
                Icon = "🥗",
                IsExpanded = false,
                Items = nutritionItems
            });

            return sections;
        }

        private static QuickMealCheckItem FlagItem(string id, string icon, string eventType) => new QuickMealCheckItem
        {
            Id = id,
            Icon = icon,
            Prompt = "@UI:" + MealFlagLabels.KeyFor(eventType),
            EventType = eventType
        };

        public static HashSet<string> MissionMealEvents(IEnumerable<string> missionCodes)
        {
            var events = new HashSet<string>(StringComparer.Ordinal);
            foreach (string code in missionCodes)
            {
                // Day questions are sent as meal-log flags too, so a logged meal can answer them
                foreach (MissionReportStep step in MissionInteractionCatalog.Get(code).Steps
                    .Where(s => s.Type == MissionStepType.MealReport || s.Type == MissionStepType.DayQuestion))
                {
                    if (step.EventType != null)
                    {
                        events.Add(step.EventType);
                    }
                    events.UnionWith(step.Options.Select(o => o.EventType).Where(e => e != null));
                }
            }
            // Meat-reduction missions count meat portions, but a meat-free meal is the answer that helps them
            if (events.Contains(ClientEventTypes.MealMeatConsumed))
            {
                events.Add(ClientEventTypes.MealMeatFree);
            }
            return events;
        }

        public static IEnumerable<QuickMealCheckItem> AllItems(IEnumerable<QuickMealSection> sections)
        {
            if (sections == null)
            {
                yield break;
            }
            foreach (QuickMealSection section in sections)
            {
                if (section?.Items == null)
                {
                    continue;
                }
                foreach (QuickMealCheckItem item in section.Items)
                {
                    yield return item;
                }
            }
        }

        /// <summary>Filtered sections share the item instances, so marks survive toggling the filter.</summary>
        public static List<QuickMealSection> FilterByMissionEvents(List<QuickMealSection> sections, HashSet<string> missionEvents)
        {
            if (sections == null || missionEvents == null || missionEvents.Count == 0)
            {
                return sections;
            }
            return sections
                .Select(s => new QuickMealSection
                {
                    Id = s.Id,
                    Title = s.Title,
                    Icon = s.Icon,
                    IsExpanded = true,
                    Items = s.Items.Where(i => missionEvents.Contains(i.EventType ?? string.Empty)).ToList()
                })
                .Where(s => s.Items.Count > 0)
                .ToList();
        }

        public static void Toggle(QuickMealCheckItem target, IReadOnlyCollection<QuickMealCheckItem> allItems)
        {
            if (target == null)
            {
                return;
            }

            target.IsChecked = !target.IsChecked;
            if (target.IsChecked && target.QuestionType == DirectQuestionType.SwapSelector &&
                target.SwapOptions != null && target.SwapOptions.Length > 0 && string.IsNullOrEmpty(target.SelectedSwapOption))
            {
                target.SelectedSwapOption = target.SwapOptions[0];
                target.EventType = target.SwapOptions[0];
            }
            else if (!target.IsChecked && target.QuestionType == DirectQuestionType.SwapSelector)
            {
                target.SelectedSwapOption = null;
                target.EventType = null;
            }

            if (allItems == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(target.EventType))
            {
                foreach (QuickMealCheckItem item in allItems)
                {
                    if (item != null && item.Id != target.Id && item.EventType == target.EventType)
                    {
                        item.IsChecked = target.IsChecked;
                    }
                }
            }

            if (target.IsChecked)
            {
                ApplyMeatExclusion(target, allItems);
            }
        }

        private static void ApplyMeatExclusion(QuickMealCheckItem target, IEnumerable<QuickMealCheckItem> allItems)
        {
            if (IsMeatFreeItem(target))
            {
                foreach (QuickMealCheckItem item in allItems)
                {
                    if (IsMeatConsumedItem(item) || IsSwapEndingInMeat(item))
                    {
                        Uncheck(item);
                    }
                }
            }
            else if (IsSwapEndingInMeat(target))
            {
                // Beef → chicken is still meat: the meal had meat
                foreach (QuickMealCheckItem item in allItems)
                {
                    if (IsMeatFreeItem(item))
                    {
                        item.IsChecked = false;
                    }
                    else if (IsMeatConsumedItem(item))
                    {
                        item.IsChecked = true;
                    }
                }
            }
            else if (IsMeatConsumedItem(target))
            {
                foreach (QuickMealCheckItem item in allItems)
                {
                    if (IsMeatFreeItem(item))
                    {
                        item.IsChecked = false;
                    }
                }
            }
        }

        private static bool IsMeatFreeItem(QuickMealCheckItem item)
        {
            if (item == null)
            {
                return false;
            }
            return item.EventType == ClientEventTypes.MealMeatFree ||
                   item.EventType == ClientEventTypes.MealVegan ||
                   item.Id == "q_meat_free" ||
                   item.Id == "q_plant_based" ||
                   item.Id == "q_vegan";
        }

        private static bool IsSwapEndingInMeat(QuickMealCheckItem item) =>
            item != null && item.IsChecked && MeatSwaps.EndsInMeat(!string.IsNullOrEmpty(item.SelectedSwapOption) ? item.SelectedSwapOption : item.EventType);

        private static void Uncheck(QuickMealCheckItem item)
        {
            item.IsChecked = false;
            if (item.QuestionType == DirectQuestionType.SwapSelector)
            {
                item.SelectedSwapOption = null;
                item.EventType = null;
            }
        }

        private static bool IsMeatConsumedItem(QuickMealCheckItem item)
        {
            if (item == null)
            {
                return false;
            }
            return item.EventType == ClientEventTypes.MealMeatConsumed ||
                   item.Id == "q_meat_consumed";
        }

        public static void SelectSwap(QuickMealCheckItem target, string swapOption, IReadOnlyCollection<QuickMealCheckItem> allItems = null)
        {
            if (target == null || string.IsNullOrEmpty(swapOption))
            {
                return;
            }

            if (target.SelectedSwapOption == swapOption && target.IsChecked)
            {
                target.SelectedSwapOption = null;
                target.EventType = null;
                target.IsChecked = false;
            }
            else
            {
                target.SelectedSwapOption = swapOption;
                target.EventType = swapOption;
                target.IsChecked = true;
                if (allItems != null)
                {
                    ApplyMeatExclusion(target, allItems);
                }
            }
        }

        public static (string[] Flags, string[] Swaps) Build(IEnumerable<QuickMealCheckItem> items)
        {
            var flags = new List<string>();
            var swaps = new List<string>();

            foreach (QuickMealCheckItem item in items ?? Enumerable.Empty<QuickMealCheckItem>())
            {
                if (item == null || !item.IsChecked)
                {
                    continue;
                }

                string ev = !string.IsNullOrEmpty(item.SelectedSwapOption) ? item.SelectedSwapOption : item.EventType;
                if (string.IsNullOrEmpty(ev))
                {
                    continue;
                }

                if (ev.StartsWith("SWAP_"))
                {
                    if (!swaps.Contains(ev))
                    {
                        swaps.Add(ev);
                    }
                }
                else if (ev != ClientEventTypes.MealLogged && !flags.Contains(ev))
                {
                    flags.Add(ev);
                }
            }

            // MEAL_MEAT_CONSUMED cannot be combined with MEAL_MEAT_FREE or MEAL_VEGAN
            if (flags.Contains(ClientEventTypes.MealVegan) || flags.Contains(ClientEventTypes.MealMeatFree))
            {
                flags.Remove(ClientEventTypes.MealMeatConsumed);
            }

            return (flags.ToArray(), swaps.ToArray());
        }

        public static void ApplySelections(IReadOnlyCollection<QuickMealCheckItem> items, string[] flags, string[] swaps)
        {
            if (items == null)
            {
                return;
            }

            var flagsSet = new HashSet<string>(flags ?? Array.Empty<string>());
            var swapsSet = new HashSet<string>(swaps ?? Array.Empty<string>());

            foreach (QuickMealCheckItem item in items)
            {
                if (item == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.EventType) && flagsSet.Contains(item.EventType))
                {
                    item.IsChecked = true;
                }

                if (swapsSet.Count == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.EventType) && swapsSet.Contains(item.EventType))
                {
                    item.IsChecked = true;
                    item.SelectedSwapOption = item.EventType;
                }
                else if (item.SwapOptions != null && item.SwapOptions.Length > 0)
                {
                    foreach (string option in item.SwapOptions)
                    {
                        if (swapsSet.Contains(option))
                        {
                            item.IsChecked = true;
                            item.SelectedSwapOption = option;
                            item.EventType = option;
                            break;
                        }
                    }
                }
            }

            // Mutual exclusion sanity check
            if (flagsSet.Contains(ClientEventTypes.MealVegan) || flagsSet.Contains(ClientEventTypes.MealMeatFree))
            {
                QuickMealCheckItem meatConsumed = items.FirstOrDefault(i => i?.EventType == ClientEventTypes.MealMeatConsumed);
                if (meatConsumed != null)
                {
                    meatConsumed.IsChecked = false;
                }
            }
        }
    }
}
