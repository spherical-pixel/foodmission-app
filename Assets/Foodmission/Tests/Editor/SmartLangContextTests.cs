using System.Threading.Tasks;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SmartLangContextTests
    {
        // Smart entries resolve {global.PET_NAME} through a nested LocalizedString whose table loads asynchronously:
        // the EditMode runner can't complete that, so the substitution itself is checked in the Editor / Play Mode.
        [Test]
        public async Task ResolveAsync_SmartEntry_NeverReturnsNull()
        {
            string text = await SmartLangContext.ResolveAsync("@UI:MISSION_BTN_TELL_NUTRI", "en");

            Assert.IsNotNull(text);
        }

        [Test]
        public async Task ResolveAsync_PlainEntry_And_NonReference()
        {
            Assert.AreEqual("Enviar", await SmartLangContext.ResolveAsync("@UI:MISSION_PRODUCT_SEND", "es"));
            Assert.AreEqual("plain text", await SmartLangContext.ResolveAsync("plain text", "es"));
            Assert.AreEqual("@UI:NOT_A_KEY_XYZ", await SmartLangContext.ResolveAsync("@UI:NOT_A_KEY_XYZ", "es"));
        }

        [Test]
        public void Create_UsesTheSmartResolver()
        {
            Assert.IsNotNull(SmartLangContext.Create("en").GetLocalizedStringAsyncFunc);
            Assert.AreEqual("en", SmartLangContext.Create("en").lang);
        }
    }
}
