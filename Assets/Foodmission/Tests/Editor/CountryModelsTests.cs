using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CountryModelsTests
    {
        [Test]
        public void CountryData_Roundtrips_Via_JsonUtility()
        {
            var country = new CountryData
            {
                country_iso = "ES",
                country_name_local = "Spain",
                flag = "🇪🇸",
                regions = new List<RegionData>
                {
                    new RegionData { region_iso = "CT", region_name_local = "Catalonia" }
                }
            };
            string json = JsonUtility.ToJson(country);
            var result = JsonUtility.FromJson<CountryData>(json);
            Assert.AreEqual("ES", result.country_iso);
            Assert.AreEqual("Spain", result.country_name_local);
            Assert.AreEqual("🇪🇸", result.flag);
            Assert.IsNotNull(result.regions);
            Assert.AreEqual(1, result.regions.Count);
            Assert.AreEqual("CT", result.regions[0].region_iso);
        }

        [Test]
        public void CountryData_WithoutRegions_SerializesCorrectly()
        {
            var country = new CountryData
            {
                country_iso = "FR",
                country_name_local = "France",
                flag = "🇫🇷"
            };
            string json = JsonUtility.ToJson(country);
            var result = JsonUtility.FromJson<CountryData>(json);
            Assert.AreEqual("FR", result.country_iso);
            Assert.That(result.regions, Is.Empty);
        }

        [Test]
        public void RegionData_Roundtrips_Via_JsonUtility()
        {
            var region = new RegionData { region_iso = "CT", region_name_local = "Catalonia" };
            string json = JsonUtility.ToJson(region);
            var result = JsonUtility.FromJson<RegionData>(json);
            Assert.AreEqual("CT", result.region_iso);
            Assert.AreEqual("Catalonia", result.region_name_local);
        }

        [Test]
        public void CountriesList_Roundtrips_Via_JsonUtility()
        {
            var list = new CountriesList
            {
                countries = new List<CountryData>
                {
                    new CountryData { country_iso = "ES", country_name_local = "Spain" },
                    new CountryData { country_iso = "FR", country_name_local = "France" }
                }
            };
            string json = JsonUtility.ToJson(list);
            var result = JsonUtility.FromJson<CountriesList>(json);
            Assert.AreEqual(2, result.countries.Count);
            Assert.AreEqual("ES", result.countries[0].country_iso);
            Assert.AreEqual("France", result.countries[1].country_name_local);
        }

        // ── CountryUtils.CountryCodeToFlag ──────────────────────────────────

        [Test]
        public void CountryCodeToFlag_WithValidCode_ReturnsEmoji()
        {
            Assert.AreEqual("\U0001F1EA\U0001F1F8", CountryUtils.CountryCodeToFlag("ES")); // 🇪🇸
            Assert.AreEqual("\U0001F1E6\U0001F1F9", CountryUtils.CountryCodeToFlag("AT")); // 🇦🇹
            Assert.AreEqual("\U0001F1FA\U0001F1F8", CountryUtils.CountryCodeToFlag("US")); // 🇺🇸
        }

        [Test]
        public void CountryCodeToFlag_WithInvalidInput_ReturnsEmpty()
        {
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag(null));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag(""));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("X"));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("ABC"));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("12"));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("es")); // lowercase not supported
        }
    }
}
