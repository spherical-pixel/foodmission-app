using System.Collections.Generic;

using NUnit.Framework;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Filtering of the searchable country/region picker: case and accent insensitive,
    /// word-start matches first.
    /// </summary>
    [TestFixture]
    public class FMSearchablePickerTests
    {
        private static readonly List<string> Countries = new List<string>
        {
            "Austria", "Belgium", "Greece", "Ελλάδα", "Österreich", "España", "Norge", "Polska", "Slovenija"
        };

        [Test]
        public void Filter_EmptyQuery_ReturnsAllInOrder()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, FMSearchablePicker.Filter(Countries, ""));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, FMSearchablePicker.Filter(Countries, "   "));
        }

        [Test]
        public void Filter_IgnoresCaseAndAccents()
        {
            CollectionAssert.AreEqual(new[] { 4 }, FMSearchablePicker.Filter(Countries, "oster"));
            CollectionAssert.AreEqual(new[] { 5 }, FMSearchablePicker.Filter(Countries, "ESPANA"));
            CollectionAssert.AreEqual(new[] { 3 }, FMSearchablePicker.Filter(Countries, "ελλαδα"));
        }

        [Test]
        public void Filter_WordStartMatchesComeFirst()
        {
            var regions = new List<string> { "Castilla-La Mancha", "La Rioja", "Málaga" };

            // "Castilla-La" and "La Rioja" start a word, "Málaga" only contains it
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, FMSearchablePicker.Filter(regions, "la"));
            CollectionAssert.AreEqual(new[] { 1, 0 }, FMSearchablePicker.Filter(new List<string> { "Galicia", "Lisboa" }, "li"));
        }

        [Test]
        public void Filter_NoMatch_ReturnsEmpty()
        {
            Assert.IsEmpty(FMSearchablePicker.Filter(Countries, "xyz"));
        }

        [Test]
        public void Normalize_HandlesLettersWithoutDecomposition()
        {
            Assert.AreEqual("lodzkie", FMSearchablePicker.Normalize("Łódzkie"));
            Assert.AreEqual("tromso", FMSearchablePicker.Normalize("Tromsø"));
            Assert.AreEqual("strasse", FMSearchablePicker.Normalize("Straße"));
            Assert.AreEqual("", FMSearchablePicker.Normalize(null));
        }
    }
}
