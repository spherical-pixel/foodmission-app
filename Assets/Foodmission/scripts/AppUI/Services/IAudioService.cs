using System;
using UnityEngine;
using UnityEngine.Audio;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Service for playing sound effects (SFX) and background music (BGM),
    /// as well as managing AudioMixer track volume levels (FXVOL, MUSVOL).
    /// </summary>
    public interface IAudioService : IDisposable
    {
        /// <summary>
        /// Initializes the AudioService with the AudioMixer asset, optional catalog, and optional AudioSources.
        /// </summary>
        /// <param name="mixer">The Unity AudioMixer asset containing FXVOL and MUSVOL parameters</param>
        /// <param name="catalog">Optional AudioCatalogSO ScriptableObject with pre-loaded clips</param>
        /// <param name="sfxSource">Optional AudioSource component for sound effects</param>
        /// <param name="musicSource">Optional AudioSource component for background music</param>
        void Initialize(AudioMixer mixer, AudioCatalogSO catalog = null, AudioSource sfxSource = null, AudioSource musicSource = null);

        /// <summary>
        /// Plays a sound effect by strongly-typed SfxType enum.
        /// </summary>
        void PlaySfx(SfxType sfxType, float volumeScale = 1.0f);

        /// <summary>
        /// Plays a Nutri mascot sound effect by strongly-typed NutriSfxType enum.
        /// </summary>
        void PlayNutriSfx(NutriSfxType sfxType, float volumeScale = 1.0f);

        /// <summary>
        /// Plays a one-shot sound effect.
        /// </summary>
        /// <param name="clip">The AudioClip to play</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0)</param>
        void PlaySfx(AudioClip clip, float volumeScale = 1.0f);

        /// <summary>
        /// Plays a sound effect by path or asset name.
        /// </summary>
        /// <param name="sfxName">Name or resource path of sound effect</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0)</param>
        void PlaySfx(string sfxName, float volumeScale = 1.0f);

        /// <summary>
        /// Plays background music.
        /// </summary>
        /// <param name="clip">The AudioClip to play as background music</param>
        /// <param name="loop">Whether the music track should loop</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0)</param>
        void PlayMusic(AudioClip clip, bool loop = true, float volumeScale = 1.0f);

        /// <summary>
        /// Plays background music by path or asset name.
        /// </summary>
        /// <param name="musicName">Name or resource path of music track</param>
        /// <param name="loop">Whether the music track should loop</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0)</param>
        void PlayMusic(string musicName, bool loop = true, float volumeScale = 1.0f);

        /// <summary>
        /// Stops the currently playing background music.
        /// </summary>
        void StopMusic();

        /// <summary>
        /// Sets the sound effects volume (0 to 100) and updates AudioMixer parameter FXVOL.
        /// </summary>
        /// <param name="volume">Volume scale from 0 to 100</param>
        void SetSoundVolume(int volume);

        /// <summary>
        /// Sets the background music volume (0 to 100) and updates AudioMixer parameter MUSVOL.
        /// </summary>
        /// <param name="volume">Volume scale from 0 to 100</param>
        void SetMusicVolume(int volume);

        /// <summary>
        /// Gets current sound volume (0-100).
        /// </summary>
        int SoundVolume { get; }

        /// <summary>
        /// Gets current music volume (0-100).
        /// </summary>
        int MusicVolume { get; }

        /// <summary>
        /// Converts a linear volume value (0 to 100) to decibels (-80.0 dB to 0.0 dB).
        /// </summary>
        /// <param name="volume">Linear volume (0 to 100)</param>
        /// <returns>Decibels (-80.0f to 0.0f)</returns>
        float LinearToDecibels(int volume);
    }
}
