using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodyModelsTests
    {
        [Test]
        public void FoodyItemType_Normalize_HandlesVariations()
        {
            Assert.AreEqual("ANTENNAS", FoodyItemType.Normalize("antennas"));
            Assert.AreEqual("ANTENNAS", FoodyItemType.Normalize("ANTENNAS"));
            Assert.AreEqual("EARS", FoodyItemType.Normalize("  ears  "));
            Assert.AreEqual("GLASSES", FoodyItemType.Normalize("Glasses"));
            Assert.AreEqual(string.Empty, FoodyItemType.Normalize(null));
            Assert.AreEqual(string.Empty, FoodyItemType.Normalize(""));
            Assert.AreEqual(string.Empty, FoodyItemType.Normalize("   "));
        }

        [Test]
        public void FoodyItem_Deserializes_Correctly()
        {
            string json = @"{
                ""id"": ""item-123"",
                ""code"": ""ANTENNAS_1"",
                ""type"": ""ANTENNAS"",
                ""slot"": 1,
                ""cost"": 100,
                ""locked"": false,
                ""owned"": true,
                ""equipped"": true,
                ""available"": true
            }";

            var item = JsonConvert.DeserializeObject<FoodyItem>(json);

            Assert.IsNotNull(item);
            Assert.AreEqual("item-123", item.id);
            Assert.AreEqual("ANTENNAS_1", item.code);
            Assert.AreEqual("ANTENNAS", item.type);
            Assert.AreEqual(1, item.slot);
            Assert.AreEqual(100, item.cost);
            Assert.IsFalse(item.locked);
            Assert.IsTrue(item.owned);
            Assert.IsTrue(item.equipped);
            Assert.IsTrue(item.available);
        }

        [Test]
        public void FoodyLoadout_GetAndSetItemByType_Works()
        {
            var loadout = new FoodyLoadout();

            var ant = new FoodyItem { code = "ANTENNAS_2", type = "ANTENNAS", slot = 2 };
            var ear = new FoodyItem { code = "EARS_3", type = "EARS", slot = 3 };
            var gla = new FoodyItem { code = "GLASSES_1", type = "GLASSES", slot = 1 };

            loadout.SetItemByType("antennas", ant);
            loadout.SetItemByType("EARS", ear);
            loadout.SetItemByType("glasses", gla);

            Assert.AreSame(ant, loadout.GetItemByType("ANTENNAS"));
            Assert.AreSame(ant, loadout.antennas);
            Assert.AreSame(ear, loadout.GetItemByType("ears"));
            Assert.AreSame(ear, loadout.ears);
            Assert.AreSame(gla, loadout.GetItemByType("GLASSES"));
            Assert.AreSame(gla, loadout.glasses);

            Assert.IsNull(loadout.GetItemByType("unknown"));
            Assert.IsNull(loadout.GetItemByType(null));
        }

        [Test]
        public void FoodyLoadout_Deserializes_Correctly()
        {
            string json = @"{
                ""antennas"": {
                    ""id"": ""ant-1"",
                    ""code"": ""ANTENNAS_1"",
                    ""type"": ""ANTENNAS"",
                    ""slot"": 1,
                    ""cost"": 0,
                    ""owned"": true,
                    ""equipped"": true
                },
                ""ears"": null,
                ""glasses"": {
                    ""id"": ""gla-2"",
                    ""code"": ""GLASSES_2"",
                    ""type"": ""GLASSES"",
                    ""slot"": 2,
                    ""cost"": 150,
                    ""owned"": true,
                    ""equipped"": true
                }
            }";

            var loadout = JsonConvert.DeserializeObject<FoodyLoadout>(json);

            Assert.IsNotNull(loadout);
            Assert.IsNotNull(loadout.antennas);
            Assert.AreEqual("ANTENNAS_1", loadout.antennas.code);
            Assert.AreEqual(1, loadout.antennas.slot);
            Assert.IsNull(loadout.ears);
            Assert.IsNotNull(loadout.glasses);
            Assert.AreEqual("GLASSES_2", loadout.glasses.code);
            Assert.AreEqual(2, loadout.glasses.slot);
        }

        [Test]
        public void FoodyPurchaseResponse_Deserializes_Correctly()
        {
            string json = @"{
                ""item"": {
                    ""id"": ""item-456"",
                    ""code"": ""EARS_2"",
                    ""type"": ""EARS"",
                    ""slot"": 2,
                    ""cost"": 200,
                    ""owned"": true,
                    ""equipped"": false
                },
                ""pricePaid"": 200,
                ""pointsBalance"": 350
            }";

            var response = JsonConvert.DeserializeObject<FoodyPurchaseResponse>(json);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.item);
            Assert.AreEqual("EARS_2", response.item.code);
            Assert.AreEqual(200, response.pricePaid);
            Assert.AreEqual(350, response.pointsBalance);
            Assert.IsTrue(response.item.owned);
        }
    }
}
