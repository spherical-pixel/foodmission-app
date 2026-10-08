using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMMealFactsPickerTests
    {
        [Test]
        public void SetContent_RendersOneBoxPerSection_WithSelectedCountBadge()
        {
            var sections = MealFacts.BuildStandardSections();
            MealFacts.AllItems(sections).First(i => i.Id == "q_legumes").IsChecked = true;
            var picker = new FMMealFactsPicker();

            picker.SetContent(sections);

            Assert.AreEqual(sections.Count, picker.Query(className: "fm-quick-meal-accordion-section").ToList().Count);
            var badges = picker.Query<Unity.AppUI.UI.Text>(className: "fm-quick-meal-accordion-badge").ToList();
            Assert.AreEqual(1, badges.Count);
            Assert.AreEqual("1", badges[0].text);
            Assert.AreEqual(1, picker.Query(className: "fm-quick-meal-question-card--checked").ToList().Count);
        }

        [Test]
        public void SetContent_CheckboxIgnoresPointer_SoTheCardIsTheOnlyTogglePath()
        {
            var picker = new FMMealFactsPicker();

            picker.SetContent(MealFacts.BuildStandardSections());

            var checkboxes = picker.Query<Unity.AppUI.UI.Checkbox>().ToList();
            Assert.IsNotEmpty(checkboxes);
            foreach (var checkbox in checkboxes)
            {
                Assert.AreEqual(PickingMode.Ignore, checkbox.pickingMode);
                Assert.IsTrue(checkbox.Query<VisualElement>().ToList().All(e => e.pickingMode == PickingMode.Ignore));
            }
        }

        [Test]
        public void SetContent_WithoutSections_RendersFallbackCards_AndReplacesPreviousContent()
        {
            var picker = new FMMealFactsPicker();
            picker.SetContent(MealFacts.BuildStandardSections());

            picker.SetContent(new List<QuickMealSection>(), new List<QuickMealCheckItem>
            {
                new QuickMealCheckItem { Id = "a", Prompt = "A" },
                new QuickMealCheckItem { Id = "b", Prompt = "B" }
            });

            Assert.AreEqual(0, picker.Query(className: "fm-quick-meal-accordion-section").ToList().Count);
            Assert.AreEqual(2, picker.Query(className: "fm-quick-meal-question-card").ToList().Count);
        }
    }
}
