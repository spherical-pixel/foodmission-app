using NUnit.Framework;

using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMStarRatingTests
    {
        [TestCase(4.2f, 0, 1f)]
        [TestCase(4.2f, 3, 1f)]
        [TestCase(4.2f, 4, 0.2f)]
        [TestCase(2.5f, 2, 0.5f)]
        [TestCase(2.5f, 3, 0f)]
        [TestCase(0f, 0, 0f)]
        [TestCase(7f, 4, 1f)]
        [TestCase(-1f, 0, 0f)]
        public void FillFraction_PerStar(float value, int index, float expected)
        {
            Assert.AreEqual(expected, FMStarRating.FillFraction(value, index), 0.0001f);
        }

        [Test]
        public void Value_SetsFillWidthsInPercent()
        {
            var rating = new FMStarRating { value = 3.5f };

            Assert.AreEqual(5, rating.Stars.Count);
            Assert.AreEqual(100f, FillWidth(rating, 0), 0.01f);
            Assert.AreEqual(50f, FillWidth(rating, 3), 0.01f);
            Assert.AreEqual(0f, FillWidth(rating, 4), 0.01f);
        }

        [Test]
        public void Select_RaisesStarClicked_OnlyWhenInteractive()
        {
            var rating = new FMStarRating();
            int clicked = 0;
            rating.StarClicked += stars => clicked = stars;

            rating.Select(3);
            Assert.AreEqual(0, clicked);

            rating.interactive = true;
            rating.Select(3);
            Assert.AreEqual(3, clicked);
            Assert.IsTrue(rating.ClassListContains(FMStarRating.InteractiveUssClassName));
        }

        private static float FillWidth(FMStarRating rating, int index)
        {
            VisualElement fill = rating.Stars[index].Q(className: FMStarRating.FillUssClassName);
            Assert.AreEqual(LengthUnit.Percent, fill.style.width.value.unit);
            return fill.style.width.value.value;
        }
    }
}
