using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AudioServiceTests
    {
        private TestStoreService _storeService;
        private AudioService _audioService;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _audioService = new AudioService(_storeService);
        }

        [TearDown]
        public void TearDown()
        {
            _audioService?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithStoreVolumeDefaults()
        {
            Assert.AreEqual(100, _audioService.SoundVolume);
            Assert.AreEqual(100, _audioService.MusicVolume);
        }

        [Test]
        public void Constructor_ReadsInitialAppStateVolumes()
        {
            _storeService.Dispose();
            _storeService = new TestStoreService();
            var state = _storeService.GetAppState();
            state.soundVolume = 80;
            state.musicVolume = 40;
            _storeService.SetAppState(state);

            using var service = new AudioService(_storeService);
            Assert.AreEqual(80, service.SoundVolume);
            Assert.AreEqual(40, service.MusicVolume);
        }

        [TestCase(100, 0.0f)]
        [TestCase(50, -6.0205999f)]
        [TestCase(10, -20.0f)]
        [TestCase(1, -40.0f)]
        [TestCase(0, -80.0f)]
        [TestCase(-10, -80.0f)]
        public void LinearToDecibels_ConvertsVolumeCorrectly(int volume, float expectedDecibels)
        {
            float db = _audioService.LinearToDecibels(volume);
            Assert.AreEqual(expectedDecibels, db, 0.01f);
        }

        [Test]
        public void SetSoundVolume_ClampsAndUpdatesProperty()
        {
            _audioService.SetSoundVolume(75);
            Assert.AreEqual(75, _audioService.SoundVolume);

            _audioService.SetSoundVolume(150);
            Assert.AreEqual(100, _audioService.SoundVolume);

            _audioService.SetSoundVolume(-20);
            Assert.AreEqual(0, _audioService.SoundVolume);
        }

        [Test]
        public void SetMusicVolume_ClampsAndUpdatesProperty()
        {
            _audioService.SetMusicVolume(35);
            Assert.AreEqual(35, _audioService.MusicVolume);

            _audioService.SetMusicVolume(200);
            Assert.AreEqual(100, _audioService.MusicVolume);

            _audioService.SetMusicVolume(-50);
            Assert.AreEqual(0, _audioService.MusicVolume);
        }

        [Test]
        public void StoreDispatch_UpdatesAudioServiceVolumes()
        {
            _storeService.store.Dispatch(AppActions.setSound.Invoke(65));
            _storeService.store.Dispatch(AppActions.setMusic.Invoke(25));

            Assert.AreEqual(65, _audioService.SoundVolume);
            Assert.AreEqual(25, _audioService.MusicVolume);
        }

        [Test]
        public void PlaySfx_WithNullClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.PlaySfx((AudioClip)null));
        }

        [Test]
        public void PlaySfx_WithInvalidName_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.PlaySfx("non_existent_clip"));
        }

        [Test]
        public void PlayMusic_WithNullClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.PlayMusic((AudioClip)null));
        }

        [Test]
        public void PlayMusic_WithInvalidName_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.PlayMusic("non_existent_music"));
        }

        [Test]
        public void StopMusic_WhenNoMusicPlaying_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.StopMusic());
        }

        [Test]
        public void PlaySfx_WithSfxTypeEnum_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.PlaySfx(SfxType.PositiveButton));
            Assert.DoesNotThrow(() => _audioService.PlaySfx(SfxType.None));
        }

        [Test]
        public void PlayNutriSfx_WithNutriSfxTypeEnum_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _audioService.PlayNutriSfx(NutriSfxType.Celebration));
            Assert.DoesNotThrow(() => _audioService.PlayNutriSfx(NutriSfxType.None));
        }

        [Test]
        public void Dispose_DoesNotThrowOnMultipleCalls()
        {
            _audioService.Dispose();
            Assert.DoesNotThrow(() => _audioService.Dispose());
        }
    }
}
