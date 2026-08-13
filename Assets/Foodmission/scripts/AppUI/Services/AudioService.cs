using System;
using System.Collections.Generic;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.Audio;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Implementation of IAudioService.
    /// Manages sound effects, music playback, and AudioMixer exposed parameters (FXVOL, MUSVOL).
    /// </summary>
    public class AudioService : IAudioService
    {
        private const string FxParamName = "FXVOL";
        private const string MusicParamName = "MUSVOL";

        private readonly IStoreService _storeService;
        private AudioMixer _mixer;
        private AudioCatalogSO _catalog;
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private GameObject _audioHostGameObject;
        private IDisposableSubscription _storeSubscription;

        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

        public int SoundVolume { get; private set; } = 100;
        public int MusicVolume { get; private set; } = 100;

        public AudioService(IStoreService storeService)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));

            // Sync initial state from store if available
            AppState initialState = _storeService.GetAppState();
            if (initialState != null)
            {
                SoundVolume = initialState.soundVolume;
                MusicVolume = initialState.musicVolume;
            }

            // Subscribe to store updates for sound, music, user session, and auth
            _storeSubscription = _storeService.store?.Subscribe(
                state => (state.soundVolume, state.musicVolume, state.userId, state.accessToken),
                OnVolumesStateChanged
            );
        }

        public void Initialize(AudioMixer mixer, AudioCatalogSO catalog = null, AudioSource sfxSource = null, AudioSource musicSource = null)
        {
            _mixer = mixer;
            if (_mixer == null)
            {
                _mixer = Resources.Load<AudioMixer>("AudioMixer");
            }

            _catalog = catalog;
            if (_catalog == null)
            {
                _catalog = Resources.Load<AudioCatalogSO>("AudioCatalog");
            }
            _catalog?.Initialize();

            _sfxSource = sfxSource;
            _musicSource = musicSource;

            EnsureAudioSources();
            RouteAudioSourcesToMixerGroups();

            // Re-sync latest state from store service
            AppState state = _storeService.GetAppState();
            if (state != null)
            {
                SoundVolume = state.soundVolume;
                MusicVolume = state.musicVolume;
            }

            // Apply initial volumes to mixer immediately
            ApplyVolumeToMixer(FxParamName, SoundVolume);
            ApplyVolumeToMixer(MusicParamName, MusicVolume);

            // Also schedule a delayed frame application for Unity AudioMixer graph activation
            if (Application.isPlaying && _audioHostGameObject != null)
            {
                var hostScript = _audioHostGameObject.GetComponent<AudioServiceHostMono>() ?? _audioHostGameObject.AddComponent<AudioServiceHostMono>();
                hostScript.StartCoroutine(ApplyVolumesDelayedRoutine());
            }
        }

        public void PlaySfx(SfxType sfxType, float volumeScale = 1.0f)
        {
            if (sfxType == SfxType.None) return;

            AudioClip clip = _catalog != null ? _catalog.GetSfx(sfxType) : null;
            if (clip != null)
            {
                PlaySfx(clip, volumeScale);
            }
            else
            {
                PlaySfx(sfxType.ToString(), volumeScale);
            }
        }

        public void PlayNutriSfx(NutriSfxType sfxType, float volumeScale = 1.0f)
        {
            if (sfxType == NutriSfxType.None) return;

            AudioClip clip = _catalog != null ? _catalog.GetNutriSfx(sfxType) : null;
            if (clip != null)
            {
                PlaySfx(clip, volumeScale);
            }
            else
            {
                PlaySfx(sfxType.ToString(), volumeScale);
            }
        }

        public float LinearToDecibels(int volume)
        {
            if (volume <= 0)
            {
                return -80.0f;
            }

            float normalized = Mathf.Clamp01(volume / 100.0f);
            float dB = Mathf.Log10(normalized) * 20.0f;
            return Mathf.Clamp(dB, -80.0f, 0.0f);
        }

        public void SetSoundVolume(int volume)
        {
            SoundVolume = Mathf.Clamp(volume, 0, 100);
            ApplyVolumeToMixer(FxParamName, SoundVolume);
        }

        public void SetMusicVolume(int volume)
        {
            MusicVolume = Mathf.Clamp(volume, 0, 100);
            ApplyVolumeToMixer(MusicParamName, MusicVolume);
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null) return;
            EnsureAudioSources();
            if (_sfxSource != null)
            {
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            }
        }

        public void PlaySfx(string sfxName, float volumeScale = 1.0f)
        {
            if (string.IsNullOrEmpty(sfxName)) return;

            AudioClip clip = LoadAudioClip(sfxName);
            if (clip != null)
            {
                PlaySfx(clip, volumeScale);
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Could not load SFX clip: '{sfxName}'");
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float volumeScale = 1.0f)
        {
            if (clip == null) return;
            EnsureAudioSources();
            if (_musicSource != null)
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.volume = Mathf.Clamp01(volumeScale);
                _musicSource.Play();
            }
        }

        public void PlayMusic(string musicName, bool loop = true, float volumeScale = 1.0f)
        {
            if (string.IsNullOrEmpty(musicName)) return;

            AudioClip clip = LoadAudioClip(musicName);
            if (clip != null)
            {
                PlayMusic(clip, loop, volumeScale);
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Could not load Music clip: '{musicName}'");
            }
        }

        public void StopMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();
            }
        }

        private void OnVolumesStateChanged((int soundVolume, int musicVolume, string userId, string accessToken) state)
        {
            SetSoundVolume(state.soundVolume);
            SetMusicVolume(state.musicVolume);
        }

        private System.Collections.IEnumerator ApplyVolumesDelayedRoutine()
        {
            yield return null; // Wait for frame 1 (Unity AudioMixer graph initialization)
            ApplyVolumeToMixer(FxParamName, SoundVolume);
            ApplyVolumeToMixer(MusicParamName, MusicVolume);
            yield return new WaitForSeconds(0.1f);
            ApplyVolumeToMixer(FxParamName, SoundVolume);
            ApplyVolumeToMixer(MusicParamName, MusicVolume);
        }

        private void ApplyVolumeToMixer(string paramName, int volume)
        {
            if (_mixer == null) return;

            float dB = LinearToDecibels(volume);
            _mixer.SetFloat(paramName, dB);
        }

        private void EnsureAudioSources()
        {
            if (!Application.isPlaying && _sfxSource == null && _musicSource == null)
            {
                // Avoid creating GameObject hierarchy during non-playmode tests
                return;
            }

            if (_audioHostGameObject == null)
            {
                _audioHostGameObject = new GameObject("[AudioServiceHost]");
                UnityEngine.Object.DontDestroyOnLoad(_audioHostGameObject);
            }

            if (_sfxSource == null)
            {
                _sfxSource = _audioHostGameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }

            if (_musicSource == null)
            {
                _musicSource = _audioHostGameObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
            }

            // Ensure there is always an active AudioListener in the scene
            if (UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
            {
                _audioHostGameObject.AddComponent<AudioListener>();
                Debug.Log($"[{GetType().Name}] Added AudioListener to [AudioServiceHost]");
            }
        }

        private void RouteAudioSourcesToMixerGroups()
        {
            if (_mixer == null) return;

            if (_sfxSource != null && _sfxSource.outputAudioMixerGroup == null)
            {
                AudioMixerGroup[] fxGroups = _mixer.FindMatchingGroups("FX");
                if (fxGroups != null && fxGroups.Length > 0)
                {
                    _sfxSource.outputAudioMixerGroup = fxGroups[0];
                }
            }

            if (_musicSource != null && _musicSource.outputAudioMixerGroup == null)
            {
                AudioMixerGroup[] musicGroups = _mixer.FindMatchingGroups("MUSIC");
                if (musicGroups != null && musicGroups.Length > 0)
                {
                    _musicSource.outputAudioMixerGroup = musicGroups[0];
                }
            }
        }

        private AudioClip LoadAudioClip(string pathOrName)
        {
            if (_clipCache.TryGetValue(pathOrName, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            AudioClip clip = Resources.Load<AudioClip>(pathOrName);
            if (clip == null && !pathOrName.StartsWith("sounds/"))
            {
                clip = Resources.Load<AudioClip>($"sounds/ui/{pathOrName}");
                if (clip == null)
                {
                    clip = Resources.Load<AudioClip>($"sounds/nutri/{pathOrName}");
                }
            }

            if (clip != null)
            {
                _clipCache[pathOrName] = clip;
            }

            return clip;
        }

        public void Dispose()
        {
            _storeSubscription?.Dispose();
            _storeSubscription = null;

            _clipCache.Clear();

            if (_audioHostGameObject != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_audioHostGameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_audioHostGameObject);
                }
                _audioHostGameObject = null;
            }

            _sfxSource = null;
            _musicSource = null;
            _mixer = null;
        }
    }

    internal class AudioServiceHostMono : MonoBehaviour { }
}
