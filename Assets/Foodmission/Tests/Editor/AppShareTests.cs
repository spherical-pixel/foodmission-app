using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AppShareTests
    {
        [Test]
        public void Build_MessageFollowedByDownloadUrl()
        {
            ShareContent content = AppShare.Build("  Únete a FOODMISSION:  ", "Prueba la app");

            Assert.AreEqual("Únete a FOODMISSION:\nhttps://www.foodmission.eu/platform/", content.Text);
            Assert.AreEqual("Prueba la app", content.Subject);
            Assert.IsNull(content.Image);
        }

        [Test]
        public void Build_EmptyMessage_SharesOnlyUrl()
        {
            Assert.AreEqual(AppShare.DownloadUrl, AppShare.Build(null, null).Text);
            Assert.AreEqual(AppShare.DownloadUrl, AppShare.Build("   ", null).Text);
        }
    }
}
