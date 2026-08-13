using System;
using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public struct SfxEntry
    {
        public SfxType type;
        public AudioClip clip;
    }

    [Serializable]
    public struct NutriSfxEntry
    {
        public NutriSfxType type;
        public AudioClip clip;
    }

    /// <summary>
    /// ScriptableObject catalog holding all strongly-typed audio clips for UI and Nutri mascot.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Foodmission/Audio Catalog")]
    public class AudioCatalogSO : ScriptableObject
    {
        [Header("UI Sound Effects")]
        public List<SfxEntry> sfxClips = new List<SfxEntry>();

        [Header("Nutri Mascot Sounds")]
        public List<NutriSfxEntry> nutriSfxClips = new List<NutriSfxEntry>();

        [NonSerialized]
        private Dictionary<SfxType, AudioClip> _sfxDict;

        [NonSerialized]
        private Dictionary<NutriSfxType, AudioClip> _nutriDict;

        /// <summary>
        /// Initializes internal dictionaries for fast O(1) runtime lookup.
        /// </summary>
        public void Initialize()
        {
            _sfxDict = new Dictionary<SfxType, AudioClip>();
            if (sfxClips != null)
            {
                foreach (var entry in sfxClips)
                {
                    if (entry.type != SfxType.None && entry.clip != null)
                    {
                        _sfxDict[entry.type] = entry.clip;
                    }
                }
            }

            _nutriDict = new Dictionary<NutriSfxType, AudioClip>();
            if (nutriSfxClips != null)
            {
                foreach (var entry in nutriSfxClips)
                {
                    if (entry.type != NutriSfxType.None && entry.clip != null)
                    {
                        _nutriDict[entry.type] = entry.clip;
                    }
                }
            }
        }

        public AudioClip GetSfx(SfxType type)
        {
            if (_sfxDict == null) Initialize();
            _sfxDict.TryGetValue(type, out var clip);
            return clip;
        }

        public AudioClip GetNutriSfx(NutriSfxType type)
        {
            if (_nutriDict == null) Initialize();
            _nutriDict.TryGetValue(type, out var clip);
            return clip;
        }
    }
}
