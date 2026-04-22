using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMDialogActionTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            bool called = false;
            var action = new FMDialogAction("Aceptar", () => { called = true; }, isPrimary: true);

            Assert.AreEqual("Aceptar", action.Label);
            Assert.IsTrue(action.IsPrimary);

            action.Callback();
            Assert.IsTrue(called);
        }

        [Test]
        public void Constructor_DefaultIsPrimary_IsFalse()
        {
            var action = new FMDialogAction("Cancelar", () => { });

            Assert.IsFalse(action.IsPrimary);
        }

        [Test]
        public void Constructor_NullCallback_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new FMDialogAction("OK", null));
        }
    }
}
