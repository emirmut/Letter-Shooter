using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public Sound[] AllSounds;

    private Dictionary<SoundType, Sound> _soundDictionary = new Dictionary<SoundType, Sound>();
    [HideInInspector] public AudioSource musicSource;

    private void Awake()
    {
        Instance = this;

        foreach (var s in AllSounds)
        {
            _soundDictionary[s.Type] = s;
        }
    }

    public void Play(SoundType type)
    {
        if (_soundDictionary.TryGetValue(type, out Sound s) == false)
        {
            Debug.LogWarning($"Sound type {type} not found!");
            return;
        }

        var soundObj = new GameObject($"Sound_{type}");
        var audioSrc = soundObj.AddComponent<AudioSource>();

        audioSrc.clip = s.Clip;
        audioSrc.volume = s.Volume;

        audioSrc.Play();

        if (s.Type != SoundType.Music_Menu)
        {
            Destroy(soundObj, s.Clip.length);
        }
    }

    public void ChangeMusic(SoundType type)
    {
        if (_soundDictionary.TryGetValue(type, out Sound track) == false)
        {
            Debug.LogWarning($"Music track {type} not found!");
            return;
        }

        if (musicSource == null)
        {
            var container = new GameObject("SoundTrackObj");
            musicSource = container.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        musicSource.clip = track.Clip;
        musicSource.volume = track.Volume;
        musicSource.Play();
    }

    [System.Serializable]
    public class Sound
    {
        public SoundType Type;
        public AudioClip Clip;

        [Range(0f, 1f)]
        public float Volume = 1f;
    }
    
    public enum SoundType
    {
        GunshotHit,
        GunshotMiss,
        Music_Menu,
        Music_Gameplay
    }
}
