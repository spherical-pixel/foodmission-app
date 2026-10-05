using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ShareTextBuilderTests
    {
        private const string Footer = "Descubierto en FOODMISSION 🌱 https://www.foodmission.eu";

        [Test]
        public void Build_BodySourceAndFooter_SeparatedByBlankLines()
        {
            string text = ShareTextBuilder.Build("Las legumbres son ricas en fibra.", "OMS (2023). https://www.who.int/x", Footer);

            Assert.AreEqual("Las legumbres son ricas en fibra.\n\nOMS (2023). https://www.who.int/x\n\n" + Footer, text);
        }

        [Test]
        public void Build_WithoutSource_SkipsSourceBlock()
        {
            Assert.AreEqual("Body\n\n" + Footer, ShareTextBuilder.Build("Body", null, Footer));
            Assert.AreEqual("Body\n\n" + Footer, ShareTextBuilder.Build("Body", "   ", Footer));
        }

        [Test]
        public void Build_WithoutFooter_EndsWithLastPart()
        {
            Assert.AreEqual("Body\n\nSrc", ShareTextBuilder.Build("Body", "Src", ""));
            Assert.AreEqual("Body", ShareTextBuilder.Build("Body", null, null));
        }

        [Test]
        public void Build_EmptyBody_ReturnsEmpty()
        {
            Assert.AreEqual("", ShareTextBuilder.Build(null, "Src", Footer));
            Assert.AreEqual("", ShareTextBuilder.Build("  \n ", "Src", Footer));
        }

        [Test]
        public void Build_TrimsParts()
        {
            Assert.AreEqual("Body\n\nSrc\n\n" + Footer, ShareTextBuilder.Build("  Body \n", "\nSrc  ", "  " + Footer + " "));
        }

        [Test]
        public void FormatSource_MarkdownLink_BecomesCitationAndUrl()
        {
            string raw = "Estudio publicado en [Nature Food](https://doi.org/10.1038/s43016-022-00489-9) sobre dietas sostenibles.";

            Assert.AreEqual("Estudio publicado en Nature Food sobre dietas sostenibles. https://doi.org/10.1038/s43016-022-00489-9",
                ShareTextBuilder.FormatSource(raw));
        }

        [Test]
        public void FormatSource_PlainCitationWithUrl_KeepsCitationThenUrl()
        {
            string raw = "Organización Mundial de la Salud (2023). https://www.who.int/news-room/fact-sheets/detail/healthy-diet";

            Assert.AreEqual(raw, ShareTextBuilder.FormatSource(raw));
        }

        [Test]
        public void FormatSource_Empty_ReturnsEmpty()
        {
            Assert.AreEqual("", ShareTextBuilder.FormatSource(null));
            Assert.AreEqual("", ShareTextBuilder.FormatSource("  "));
        }

        [Test]
        public void ShareContent_IsEmpty_WhenNoTextAndNoImage()
        {
            Assert.IsTrue(new ShareContent().IsEmpty);
            Assert.IsTrue(new ShareContent { Text = "  " }.IsEmpty);
            Assert.IsFalse(new ShareContent { Text = "x" }.IsEmpty);
        }
    }
}
