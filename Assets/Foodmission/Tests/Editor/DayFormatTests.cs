using System;
using System.Globalization;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    public class DayFormatTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 11, 2);

        [TestCase("en", "Monday, November 2")]
        [TestCase("es", "lunes 2 de noviembre")]
        [TestCase("de", "Montag, 2. November")]
        [TestCase("no", "mandag 2. november")]
        [TestCase("nb", "mandag 2. november")]
        [TestCase("nl", "maandag 2 november")]
        [TestCase("it", "lunedì 2 novembre")]
        [TestCase("el", "Δευτέρα 2 Νοεμβρίου")]
        [TestCase("pl", "poniedziałek, 2 listopada")]
        [TestCase("sl", "ponedeljek, 2. novembra")]
        public void Long_IncludesWeekdayDayAndMonth(string culture, string expected)
        {
            Assert.AreEqual(expected, DayFormat.Long(Monday, new CultureInfo(culture)));
        }

        [TestCase("en", "Mon, Nov 2")]
        [TestCase("es", "lun. 2 nov.")]
        [TestCase("de", "Mo, 2. Nov")]
        [TestCase("no", "man. 2. nov")]
        [TestCase("pl", "pon. 2 lis")]
        [TestCase("sl", "pon. 2. nov.")]
        public void Short_IncludesAbbreviatedWeekdayDayAndMonth(string culture, string expected)
        {
            Assert.AreEqual(expected, DayFormat.Short(Monday, new CultureInfo(culture)));
        }

        [Test]
        public void Long_UnknownLanguage_FallsBackToEnglishPattern()
        {
            Assert.AreEqual("Monday, November 2", DayFormat.Long(Monday, CultureInfo.InvariantCulture));
        }

        [Test]
        public void Capitalize_UpperCasesOnlyTheFirstLetter()
        {
            Assert.AreEqual("Lunes 2 de noviembre", DayFormat.Capitalize("lunes 2 de noviembre", new CultureInfo("es")));
            Assert.AreEqual("", DayFormat.Capitalize("", new CultureInfo("es")));
        }
    }
}
