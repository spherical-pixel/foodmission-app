using System;
using System.IO;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SpriteToPngExporterTests
    {
        private string _dir;
        private Texture2D _texture;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "fm-share-tests-" + Guid.NewGuid().ToString("N"));
            // 8x4 texture: left half red, right half green. The sprite is the right half.
            _texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    _texture.SetPixel(x, y, x < 4 ? Color.red : Color.green);
                }
            }
            _texture.Apply();
            _sprite = Sprite.Create(_texture, new Rect(4, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [Test]
        public void Export_ReadableSprite_WritesPngOfTheSpriteRect()
        {
            string path = SpriteToPngExporter.Export(_sprite, _dir);

            AssertIsGreenSquare(path);
        }

        [Test]
        public void Export_NonReadableTexture_WritesPngOfTheSpriteRect()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("No graphics device (-nographics): the Blit path cannot run.");
            }
            _texture.Apply(false, true); // makes it non-readable, like a compressed Addressables banner

            string path = SpriteToPngExporter.Export(_sprite, _dir);

            AssertIsGreenSquare(path);
        }

        [Test]
        public void Export_RemovesPreviousFiles()
        {
            string first = SpriteToPngExporter.Export(_sprite, _dir);
            System.Threading.Thread.Sleep(2);
            string second = SpriteToPngExporter.Export(_sprite, _dir);

            Assert.AreNotEqual(first, second);
            Assert.IsFalse(File.Exists(first));
            Assert.IsTrue(File.Exists(second));
            Assert.AreEqual(1, Directory.GetFiles(_dir).Length);
        }

        [Test]
        public void Export_NullSprite_ReturnsNull()
        {
            Assert.IsNull(SpriteToPngExporter.Export(null, _dir));
        }

        [Test]
        public void DefaultDirectory_IsShareFolderInTemporaryCache()
        {
            Assert.AreEqual(Path.Combine(Application.temporaryCachePath, "share"), SpriteToPngExporter.DefaultDirectory);
        }

        private static void AssertIsGreenSquare(string path)
        {
            Assert.IsNotNull(path);
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(".png", Path.GetExtension(path));
            var loaded = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(loaded.LoadImage(File.ReadAllBytes(path)));
                Assert.AreEqual(4, loaded.width);
                Assert.AreEqual(4, loaded.height);
                Color c = loaded.GetPixel(1, 1);
                Assert.Greater(c.g, 0.9f);
                Assert.Less(c.r, 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(loaded);
            }
        }
    }
}
