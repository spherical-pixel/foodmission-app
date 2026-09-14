using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public class ActivityEventMapper : IActivityEventMapper
    {
        private static readonly string[] ProteinSwapEvents = new[]
        {
            ClientEventTypes.SwapBeefToLegumes,
            ClientEventTypes.SwapBeefToChicken,
            ClientEventTypes.SwapPorkToLegumes,
            ClientEventTypes.SwapPorkToChicken,
            ClientEventTypes.SwapChickenToLegumes,
            ClientEventTypes.SwapBeefToPork
        };

        private static readonly string[] ProcessedSwapEvents = new[]
        {
            ClientEventTypes.SwapSugaryDrinkToWater,
            ClientEventTypes.SwapSnackToFruitNuts,
            ClientEventTypes.SwapSugaryCerealToOats,
            ClientEventTypes.SwapReadyMealToHomecooked,
            ClientEventTypes.SwapProcessedMeatToLegumes
        };

        public static string GetSwapDisplayName(string eventType)
        {
            return eventType switch
            {
                ClientEventTypes.SwapBeefToLegumes => "🥩 -> 🥗 Ternera por legumbres",
                ClientEventTypes.SwapBeefToChicken => "🥩 -> 🍗 Ternera por pollo",
                ClientEventTypes.SwapBeefToPork => "🥩 -> 🐷 Ternera por cerdo",
                ClientEventTypes.SwapPorkToLegumes => "🐷 -> 🥗 Cerdo por legumbres",
                ClientEventTypes.SwapPorkToChicken => "🐷 -> 🍗 Cerdo por pollo",
                ClientEventTypes.SwapChickenToLegumes => "🍗 -> 🥗 Pollo por legumbres",
                ClientEventTypes.SwapSugaryDrinkToWater => "🥤 -> 💧 Refresco por agua",
                ClientEventTypes.SwapSnackToFruitNuts => "🍪 -> 🍎 Snack por fruta / frutos secos",
                ClientEventTypes.SwapSugaryCerealToOats => "🥣 -> 🌾 Cereales azucarados por avena",
                ClientEventTypes.SwapReadyMealToHomecooked => "🍕 -> 🍲 Ultraprocesado por casero",
                ClientEventTypes.SwapProcessedMeatToLegumes => "🌭 -> 🫘 Carne procesada por legumbres",
                _ => eventType ?? ""
            };
        }

        public ActivityMapping GetMissionMapping(string missionCode)
        {
            if (string.IsNullOrEmpty(missionCode))
            {
                return CreateDefaultMapping(missionCode);
            }

            string codeUpper = missionCode.Trim().ToUpperInvariant();

            // 1. Diet & Meat / Flexitarianism
            if (codeUpper == "M.B1.1")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealLogged },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_MEALLOG_MEAT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Anota al menos una comida al día para conocer tus hábitos y empezar a mejorarlos.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_MEAT_COUNT",
                    DirectQuestionPrompt = "📝 ¿Has anotado o registrado lo que has comido?",
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            if (codeUpper == "M.B1.2")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealMeatConsumed, ClientEventTypes.MealMeatFree, ClientEventTypes.MealLogged },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_MEALLOG_MEAT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Lleva un control de todas las comidas que contengan carne para tomar conciencia.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_MEAT_COUNT",
                    DirectQuestionPrompt = "🥩 ¿Esta comida ha sido sin carne o has contabilizado la ración?",
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            if (codeUpper == "M.B1.3")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealMeatFree, ClientEventTypes.MealLogged },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_MEALLOG_MEAT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Come una comida con carne menos que la semana anterior.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_MEAT_COUNT",
                    DirectQuestionPrompt = "🌱 ¿Has evitado o reducido la carne en este plato?",
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            if (codeUpper == "M.B1.4")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = ProteinSwapEvents,
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_PROTEIN_SWAP",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Registra cambios de proteínas sostenibles (ej. ternera por legumbres o pollo).",
                    QuestionType = DirectQuestionType.SwapSelector,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_PROTEIN_SWAP",
                    DirectQuestionPrompt = "🔄 ¿Qué intercambio de proteínas has realizado?",
                    SwapOptions = ProteinSwapEvents
                };
            }

            if (codeUpper == "M.B1.5")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.SwapBeefToLegumes, ClientEventTypes.SwapBeefToChicken },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_PROTEIN_SWAP",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Anota cambios de carne de vacuno por pollo o legumbres para reducir tu huella.",
                    QuestionType = DirectQuestionType.SwapSelector,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_PROTEIN_SWAP",
                    DirectQuestionPrompt = "🥩 ¿Qué sustitución de carne de ternera has realizado?",
                    SwapOptions = new[] { ClientEventTypes.SwapBeefToLegumes, ClientEventTypes.SwapBeefToChicken }
                };
            }

            if (codeUpper == "M.A1.1")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealMeatFree, ClientEventTypes.MealLogged },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_MEALLOG_MEAT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Mantente en la zona verde con menos de 4 comidas de carne por semana.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_MEAT_COUNT",
                    DirectQuestionPrompt = "🌱 ¿Comida flexitariana baja en emisiones (sin carne)?",
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            // Other Protein Swaps
            if (codeUpper == "M.I1.1" || codeUpper == "M.I1.2")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = ProteinSwapEvents,
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_PROTEIN_SWAP",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Realiza intercambios de proteína animal por alternativas vegetales o de menor huella.",
                    QuestionType = DirectQuestionType.SwapSelector,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_PROTEIN_SWAP",
                    DirectQuestionPrompt = codeUpper == "M.I1.1"
                        ? "🔄 ¿Has sustituido carne de vacuno por una alternativa más sostenible?"
                        : "🔄 ¿Has cambiado cerdo o carne procesada por legumbres?",
                    SwapOptions = ProteinSwapEvents
                };
            }

            // Legumes & Plant Diversity
            if (codeUpper == "M.A1.2" || codeUpper == "M.A1.3" || codeUpper == "M.I1.4" || codeUpper == "M.I1.5" || codeUpper == "M.I1.3")
            {
                string prompt = "🥗 ¿Has incluido una porción de legumbres en tu plato?";
                if (codeUpper == "M.I1.3") prompt = "🌱 ¿Ha sido una comida o día 100% vegetariano?";
                else if (codeUpper == "M.A1.2") prompt = "🌱 ¿La proteína principal ha sido de origen vegetal?";
                else if (codeUpper == "M.A1.3") prompt = "🥗 ¿Has comido legumbres (lentejas, garbanzos, alubias)?";

                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealLegumeConsumed, ClientEventTypes.MealVegan, ClientEventTypes.MealVegetarianDay },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_LEGUMES",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Las legumbres son fuentes de proteína saludables y altamente eficientes.",
                    QuestionType = DirectQuestionType.CountStepper,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_LEGUMES_COUNT",
                    DirectQuestionPrompt = prompt,
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            // Ancient grains & staples
            if (codeUpper == "M.A1.4" || codeUpper == "M.A1.5")
            {
                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealAncientGrain, ClientEventTypes.MealAlternativeStaple, ClientEventTypes.MealSustainablePlate },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_ANCIENT_GRAINS",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Combina verduras, proteínas sostenibles y cereales integrales o alternativos.",
                    QuestionType = DirectQuestionType.CountStepper,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_ANCIENT_GRAINS_COUNT",
                    DirectQuestionPrompt = codeUpper == "M.A1.4"
                        ? "🌾 ¿Has incluido cereales alternativos (quinoa, mijo, avena, espelta)?"
                        : "🍽️ ¿Plato sostenible combinado (verduras + legumbres/cereales)?",
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            // 2. Product Choices & Origin
            if (codeUpper.StartsWith("M.B2.") || codeUpper.StartsWith("M.I2.") || codeUpper.StartsWith("M.A2."))
            {
                string prompt = "🛒 ¿Has elegido productos locales, de temporada o certificados?";
                if (codeUpper == "M.B2.1") prompt = "🔍 ¿Has consultado el país de origen de los ingredientes?";
                else if (codeUpper == "M.B2.2") prompt = "🍎 ¿Has incluido frutas o verduras de temporada?";
                else if (codeUpper == "M.B2.3") prompt = "🏷️ ¿Has elegido productos ecológicos o con sello sostenible?";
                else if (codeUpper == "M.B2.4") prompt = "🏡 ¿Has utilizado ingredientes locales de proximidad?";

                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[]
                    {
                        ClientEventTypes.ShoppingOriginChecked,
                        ClientEventTypes.ShoppingLocalChosen,
                        ClientEventTypes.ShoppingSeasonalChosen,
                        ClientEventTypes.ShoppingCertificationChosen,
                        ClientEventTypes.ShoppingPackagingInfoChecked,
                        ClientEventTypes.ShoppingMulticriteriaPurchase
                    },
                    NativeModuleAction = "go_to_shopping_list",
                    NativeModuleLabelKey = "@UI:GO_TO_SHOPPING_LIST",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_PRODUCT_CHOICES",
                    NativeModuleButtonTitle = "Ir a la Lista de la Compra",
                    NativeModuleHint = "Planifica tu compra eligiendo productos locales, de temporada o certificados.",
                    QuestionType = DirectQuestionType.OptionChips,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_PRODUCT_CHOICES",
                    DirectQuestionPrompt = prompt
                };
            }

            // 3. Production Methods & Processing
            if (codeUpper.StartsWith("M.B3.") || codeUpper.StartsWith("M.I3.") || codeUpper.StartsWith("M.A3."))
            {
                bool isSwap = codeUpper == "M.B3.5" || codeUpper == "M.I3.3";
                string prompt = "🔬 ¿Has priorizado alimentos frescos y mínimamente procesados?";
                if (codeUpper == "M.B3.1") prompt = "🥫 ¿Has revisado la lista de ingredientes o grado NOVA?";
                else if (codeUpper == "M.B3.2") prompt = "🥣 ¿Has preparado una comida casera evitando ultraprocesados?";
                else if (codeUpper == "M.B3.5") prompt = "💧 ¿Has cambiado bebidas azucaradas por agua o infusiones?";

                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = isSwap ? ProcessedSwapEvents : new[]
                    {
                        ClientEventTypes.ProcessingNovaChecked,
                        ClientEventTypes.ProcessingIngredientsReviewed,
                        ClientEventTypes.ProcessingGreenscoreChecked,
                        ClientEventTypes.ProcessingIndicatorsCompared,
                        ClientEventTypes.ProcessingProductionMethodChecked
                    },
                    NativeModuleAction = "go_to_quicksearch",
                    NativeModuleLabelKey = "@UI:GO_TO_SCANNER",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_PRODUCTION_METHODS",
                    NativeModuleButtonTitle = "Abrir Escáner de Alimentos",
                    NativeModuleHint = "Escanea el código de barras de tus productos para comprobar su grado de procesamiento.",
                    QuestionType = isSwap ? DirectQuestionType.SwapSelector : DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_PRODUCTION_METHODS",
                    DirectQuestionPrompt = prompt,
                    SwapOptions = isSwap ? ProcessedSwapEvents : Array.Empty<string>()
                };
            }

            // 4. Packaging & Circularity
            if (codeUpper.StartsWith("M.B4.") || codeUpper.StartsWith("M.I4.") || codeUpper.StartsWith("M.A4."))
            {
                string prompt = "♻️ ¿Has minimizado el uso de envases desechables?";
                if (codeUpper == "M.B4.1") prompt = "📦 ¿Has observado el tipo de envase o comprado a granel?";
                else if (codeUpper == "M.B4.2") prompt = "♻️ ¿Has separado y reciclado correctamente los envases?";
                else if (codeUpper == "M.B4.3") prompt = "🛍️ ¿Has utilizado bolsas o recipientes reutilizables?";

                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[]
                    {
                        ClientEventTypes.PackagingMaterialObserved,
                        ClientEventTypes.PackagingRecyclingLabelRead,
                        ClientEventTypes.PackagingReusableSpotChosen,
                        ClientEventTypes.PackagingRecyclabilityEvaluated,
                        ClientEventTypes.PackagingComparisonMade,
                        ClientEventTypes.PackagingSmartObserved
                    },
                    NativeModuleAction = "go_to_pantry",
                    NativeModuleLabelKey = "@UI:GO_TO_PANTRY",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_PACKAGING",
                    NativeModuleButtonTitle = "Ir a la Despensa",
                    NativeModuleHint = "Controla los productos de tu despensa y gestiona los envases adecuadamente.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_PACKAGING",
                    DirectQuestionPrompt = prompt
                };
            }

            // 5. Food Waste Prevention
            if (codeUpper.StartsWith("M.B5.") || codeUpper.StartsWith("M.I5.") || codeUpper.StartsWith("M.A5."))
            {
                string prompt = "✨ ¿Has evitado el desperdicio de comida en este plato?";
                if (codeUpper == "M.B5.1") prompt = "✨ ¿Ración ajustada y plato limpio sin desperdicio?";
                else if (codeUpper == "M.B5.2") prompt = "🥘 ¿Has aprovechado sobras o alimentos maduros?";
                else if (codeUpper == "M.B5.3") prompt = "📋 ¿Comida planificada según lo que tenías en nevera/despensa?";
                else if (codeUpper == "M.B5.4") prompt = "🧊 ¿Has guardado bien las sobras para otra ocasión?";

                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[]
                    {
                        ClientEventTypes.FoodWasteHalfPlateSaved,
                        ClientEventTypes.FoodWasteFullPlateSaved,
                        ClientEventTypes.FoodWasteExpiredConsumed,
                        ClientEventTypes.FoodWasteStorageInstructionsRead,
                        ClientEventTypes.FoodWasteMealPlanned,
                        ClientEventTypes.FoodWasteFridgePantryChecked,
                        ClientEventTypes.FoodWasteFifoOrganized,
                        ClientEventTypes.FoodWasteLogged
                    },
                    NativeModuleAction = "go_to_foodwaste",
                    NativeModuleLabelKey = "@UI:GO_TO_FOODWASTE",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_FOOD_WASTE",
                    NativeModuleButtonTitle = "Ir a Desperdicio de Comida",
                    NativeModuleHint = "Registra el desperdicio evitado o pesa las sobras en el módulo de desperdicio.",
                    QuestionType = DirectQuestionType.CountStepper,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_FOOD_WASTE_COUNT",
                    DirectQuestionPrompt = prompt,
                    DefaultCount = 1,
                    MaxCount = 7
                };
            }

            // 6. Nutrition & Health
            if (codeUpper.StartsWith("M.B6.") || codeUpper.StartsWith("M.I6.") || codeUpper.StartsWith("M.A6."))
            {
                string prompt = "🥗 ¿Plato equilibrado y variado con nutrientes de calidad?";
                if (codeUpper == "M.B6.1") prompt = "💪 ¿Has incluido una fuente de proteína saludable y nutritiva?";
                else if (codeUpper == "M.B6.2") prompt = "🥦 ¿Has añadido 1-2 raciones de fruta o verdura fresca?";
                else if (codeUpper == "M.B6.3") prompt = "🌾 ¿Has elegido pan o cereales integrales ricos en fibra?";
                else if (codeUpper == "M.B6.4") prompt = "🧂 ¿Has evitado añadir sal extra o azúcares añadidos?";

                return new ActivityMapping
                {
                    ActivityCode = missionCode,
                    TargetEventTypes = new[]
                    {
                        ClientEventTypes.NutritionProteinIncluded,
                        ClientEventTypes.NutritionFruitVegServingAdded,
                        ClientEventTypes.NutritionWholegrainChosen,
                        ClientEventTypes.NutritionHighFibreMeal,
                        ClientEventTypes.NutritionSaltFreeTable,
                        ClientEventTypes.NutritionHealthyFatChosen,
                        ClientEventTypes.NutritionProteinVarietyLogged,
                        ClientEventTypes.NutritionRainbowColoursLogged,
                        ClientEventTypes.NutritionAddedSugarAvoided,
                        ClientEventTypes.NutritionPlantDiversityCount
                    },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:MISSION_HINT_NUTRITION",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Registra tus comidas saludables para llevar un seguimiento de tu equilibrio nutricional.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_NUTRITION",
                    DirectQuestionPrompt = prompt
                };
            }

            return CreateDefaultMapping(missionCode);
        }

        public ActivityMapping GetChallengeMapping(string challengeCode)
        {
            if (string.IsNullOrEmpty(challengeCode))
            {
                return CreateDefaultMapping(challengeCode);
            }

            string codeUpper = challengeCode.Trim().ToUpperInvariant();

            // 1. Diet changes challenges
            if (codeUpper == "CH.B1.1" || codeUpper == "CH.A1.5")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.LearningFootprintCompared },
                    NativeModuleAction = "go_to_shopping_list",
                    NativeModuleLabelKey = "@UI:GO_TO_SHOPPING_LIST",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_COMPARE_FOOTPRINT",
                    NativeModuleButtonTitle = "Ir a la Lista de la Compra",
                    NativeModuleHint = "Compara la huella ecológica de diferentes productos en tu lista de la compra.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_COMPARE_FOOTPRINT",
                    DirectQuestionPrompt = "⚖️ ¿Has comparado la huella ambiental de dos alimentos?"
                };
            }

            if (codeUpper == "CH.B1.2")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.NutritionProteinVarietyLogged, ClientEventTypes.NutritionProteinIncluded },
                    NativeModuleAction = "go_to_pantry",
                    NativeModuleLabelKey = "@UI:GO_TO_PANTRY",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir a la Despensa",
                    NativeModuleHint = "Abre tu nevera o despensa e identifica 3 fuentes de proteína que no sean carne (legumbres, frutos secos, huevos, etc.).",
                    QuestionType = DirectQuestionType.CountStepper,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = "🥚 ¿Has identificado 3 fuentes de proteína no cárnica en tu nevera o despensa?",
                    DefaultCount = 3,
                    MaxCount = 5
                };
            }

            if (codeUpper == "CH.B1.3" || codeUpper == "CH.A1.4")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.LearningFactViewed, ClientEventTypes.LearningFactRead },
                    NativeModuleAction = "go_to_food_facts",
                    NativeModuleLabelKey = "@UI:GO_TO_FOOD_FACTS",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_FOOD_FACTS",
                    NativeModuleButtonTitle = "Ir a Datos Curiosos",
                    NativeModuleHint = "Descubre curiosidades e información científica sobre los alimentos.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_FOOD_FACTS",
                    DirectQuestionPrompt = "💡 ¿Has descubierto los beneficios nutricionales de las legumbres?"
                };
            }

            if (codeUpper == "CH.B1.4")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.ProcessingIngredientsReviewed, ClientEventTypes.MealLogged },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Revisa los alimentos que comiste e identifica si contenían carne procesada oculta.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = "🔍 ¿Has identificado si comiste carne procesada oculta?"
                };
            }

            if (codeUpper == "CH.B1.5" || codeUpper == "CH.A1.3")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.LearningRecipeExplored, ClientEventTypes.LearningRecipeShared },
                    NativeModuleAction = "go_to_recipes",
                    NativeModuleLabelKey = "@UI:GO_TO_RECIPES",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_RECIPES",
                    NativeModuleButtonTitle = "Ir al Recetario",
                    NativeModuleHint = "Explora y cocina recetas saludables y sostenibles en el recetario.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_RECIPES",
                    DirectQuestionPrompt = "👩‍🍳 ¿Has elegido o probado una receta sostenible sin carne?"
                };
            }

            if (codeUpper == "CH.A1.1")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealAncientGrain, ClientEventTypes.MealAlternativeStaple },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Elige y prueba un cereal antiguo que nunca hayas probado.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = "🌾 ¿Has probado un cereal antiguo (quinoa, mijo, espelta)?"
                };
            }

            if (codeUpper == "CH.A1.2")
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.MealSustainablePlate, ClientEventTypes.MealLogged },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Diseña un menú completo del día con legumbres, cereales sostenibles y poca carne.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = "📋 ¿Has planificado un menú completo sostenible para hoy?"
                };
            }

            // 2. Product choices challenges
            if (codeUpper.StartsWith("CH.B2.") || codeUpper.StartsWith("CH.I2.") || codeUpper.StartsWith("CH.A2."))
            {
                string prompt = "🛒 ¿Has identificado información de sostenibilidad en tus compras?";
                if (codeUpper == "CH.B2.1") prompt = "🏷️ ¿Has revisado origen e ingredientes en la etiqueta?";
                else if (codeUpper == "CH.B2.2") prompt = "🏡 ¿Has comprobado el lugar de producción de 3 alimentos?";
                else if (codeUpper == "CH.B2.3") prompt = "🍎 ¿Has elegido una fruta o verdura de temporada?";
                else if (codeUpper == "CH.B2.4") prompt = "🌿 ¿Has buscado alimentos con sello o certificación ecológica?";

                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.ShoppingOriginChecked, ClientEventTypes.ShoppingCertificationChosen },
                    NativeModuleAction = "go_to_shopping_list",
                    NativeModuleLabelKey = "@UI:GO_TO_SHOPPING_LIST",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir a la Lista de la Compra",
                    NativeModuleHint = "Identifica origen, sellos y certificaciones en tus compras habituales.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = prompt
                };
            }

            // 3. Processing challenges
            if (codeUpper.StartsWith("CH.B3.") || codeUpper.StartsWith("CH.I3.") || codeUpper.StartsWith("CH.A3."))
            {
                string prompt = "🔬 ¿Has comprobado el procesado o ingredientes de un alimento?";
                if (codeUpper == "CH.B3.1") prompt = "🥫 ¿Has leído la lista completa de ingredientes de un alimento?";
                else if (codeUpper == "CH.B3.2") prompt = "🔬 ¿Has comprobado la clasificación NOVA de un producto?";

                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.ProcessingNovaChecked, ClientEventTypes.ProcessingIngredientsReviewed },
                    NativeModuleAction = "go_to_quicksearch",
                    NativeModuleLabelKey = "@UI:GO_TO_SCANNER",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Abrir Escáner",
                    NativeModuleHint = "Escanea productos para revisar su lista de ingredientes y clasificación NOVA.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = prompt
                };
            }

            // 4. Packaging challenges
            if (codeUpper.StartsWith("CH.B4.") || codeUpper.StartsWith("CH.I4.") || codeUpper.StartsWith("CH.A4."))
            {
                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.PackagingMaterialObserved, ClientEventTypes.PackagingRecyclabilityEvaluated },
                    NativeModuleAction = "go_to_pantry",
                    NativeModuleLabelKey = "@UI:GO_TO_PANTRY",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir a la Despensa",
                    NativeModuleHint = "Observa los tipos de envases y busca alternativas reutilizables o reciclables.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = "📦 ¿Has observado y evaluado los envases de tus alimentos?"
                };
            }

            // 5. Food waste challenges
            if (codeUpper.StartsWith("CH.B5.") || codeUpper.StartsWith("CH.I5.") || codeUpper.StartsWith("CH.A5."))
            {
                string prompt = "✨ ¿Has prevenido el desperdicio aprovechando sobras o alimentos maduros?";
                if (codeUpper == "CH.A5.1") prompt = "🥘 ¿Has preparado una nueva comida utilizando sobras?";
                else if (codeUpper == "CH.A5.4") prompt = "🧊 ¿Has creado una zona 'Cómeme primero' en la nevera?";

                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.FoodWasteHalfPlateSaved, ClientEventTypes.FoodWasteExpiredConsumed },
                    NativeModuleAction = "go_to_foodwaste",
                    NativeModuleLabelKey = "@UI:GO_TO_FOODWASTE",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir a Desperdicio de Comida",
                    NativeModuleHint = "Rescata alimentos a punto de caducar y aprovecha todas las sobras.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = prompt
                };
            }

            // 6. Nutrition challenges
            if (codeUpper.StartsWith("CH.B6.") || codeUpper.StartsWith("CH.I6.") || codeUpper.StartsWith("CH.A6."))
            {
                string prompt = "🥦 ¿Has añadido diversidad y micronutrientes a tu plato?";
                if (codeUpper == "CH.A6.4") prompt = "🌈 ¿Has combinado varios colores de frutas y verduras?";
                else if (codeUpper == "CH.A6.2") prompt = "🔍 ¿Has identificado productos con bajo contenido en azúcar?";

                return new ActivityMapping
                {
                    ActivityCode = challengeCode,
                    TargetEventTypes = new[] { ClientEventTypes.NutritionRainbowColoursLogged, ClientEventTypes.NutritionProteinIncluded },
                    NativeModuleAction = "go_to_meallog",
                    NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                    NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                    NativeModuleButtonTitle = "Ir al Registro de Comidas",
                    NativeModuleHint = "Diversifica los colores y micronutrientes de tus platos.",
                    QuestionType = DirectQuestionType.SingleChoice,
                    NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                    DirectQuestionPrompt = prompt
                };
            }

            // Default Challenge Mapping - leave DirectQuestionPrompt null to allow service fallback
            return new ActivityMapping
            {
                ActivityCode = challengeCode,
                TargetEventTypes = new[] { ClientEventTypes.MealLogged },
                NativeModuleAction = "go_to_meallog",
                NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                NativeModuleExplanationKey = "@UI:CHALLENGE_HINT_DEFAULT",
                NativeModuleButtonTitle = "Ir al Módulo",
                NativeModuleHint = "Completa el reto del día registrando tus hábitos sostenibles.",
                QuestionType = DirectQuestionType.SingleChoice,
                NutriPromptKey = "@UI:NUTRI_PROMPT_CHALLENGE_CONFIRM",
                DirectQuestionPrompt = null
            };
        }

        private ActivityMapping CreateDefaultMapping(string code)
        {
            return new ActivityMapping
            {
                ActivityCode = code ?? "",
                TargetEventTypes = new[] { ClientEventTypes.MealLogged },
                NativeModuleAction = "go_to_meallog",
                NativeModuleLabelKey = "@UI:GO_TO_MEALLOG",
                NativeModuleExplanationKey = "@UI:ACTIVITY_NATIVE_MODULE_HINT",
                NativeModuleButtonTitle = "Ir al Módulo",
                NativeModuleHint = "Registra esta acción en la app para acumular progreso.",
                QuestionType = DirectQuestionType.SingleChoice,
                NutriPromptKey = "@UI:NUTRI_PROMPT_DEFAULT",
                DirectQuestionPrompt = null
            };
        }
    }
}
