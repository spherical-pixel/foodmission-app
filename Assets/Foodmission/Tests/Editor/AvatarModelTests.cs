using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AvatarModelTests
    {
        [Test]
        public void AvatarPartConfig_Copy_ReturnsDeepClone()
        {
            var original = new AvatarPartConfig { idPart = 3, idColor = 5 };
            var copy = original.Copy();
            Assert.AreEqual(3, copy.idPart);
            Assert.AreEqual(5, copy.idColor);
            copy.idPart = 99;
            Assert.AreEqual(3, original.idPart);
        }

        [Test]
        public void AvatarPartConfig_Roundtrips_Via_JsonUtility()
        {
            var config = new AvatarPartConfig { idPart = 2, idColor = 4 };
            string json = JsonUtility.ToJson(config);
            var result = JsonUtility.FromJson<AvatarPartConfig>(json);
            Assert.AreEqual(2, result.idPart);
            Assert.AreEqual(4, result.idColor);
        }

        [Test]
        public void AvatarConfig_Copy_ReturnsDeepClone()
        {
            var original = new AvatarConfig
            {
                hair = new AvatarPartConfig { idPart = 1, idColor = 2 },
                eyes = new AvatarPartConfig { idPart = 3, idColor = 4 },
                skin = new AvatarPartConfig { idPart = 1, idColor = 5 }
            };
            var copy = original.Copy();
            Assert.AreEqual(1, copy.hair.idPart);
            copy.hair.idPart = 99;
            Assert.AreEqual(1, original.hair.idPart);
        }

        [Test]
        public void AvatarConfig_Copy_WithNullParts_DoesNotThrow()
        {
            var original = new AvatarConfig();
            var copy = original.Copy();
            Assert.IsNull(copy.hair);
            Assert.IsNull(copy.eyes);
        }

        [Test]
        public void AvatarConfig_Roundtrips_Via_JsonUtility()
        {
            var config = new AvatarConfig
            {
                hair = new AvatarPartConfig { idPart = 2, idColor = 3 },
                eyes = new AvatarPartConfig { idPart = 1, idColor = 1 },
                skin = new AvatarPartConfig { idPart = 1, idColor = 7 },
                tshirt = new AvatarPartConfig { idPart = 4, idColor = 2 },
                trousers = new AvatarPartConfig { idPart = 3, idColor = 1 },
                shoes = new AvatarPartConfig { idPart = 2, idColor = 5 }
            };
            string json = JsonUtility.ToJson(config);
            var result = JsonUtility.FromJson<AvatarConfig>(json);
            Assert.AreEqual(2, result.hair.idPart);
            Assert.AreEqual(4, result.tshirt.idPart);
            Assert.AreEqual(5, result.shoes.idColor);
        }
    }
}
