using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "Scriptable Objects/AudioData")]
public sealed class AudioData : ScriptableObject
{
    [Serializable]
    private sealed class AudioCue
    {
        [SerializeField]
        private AudioCueId _id;

        [SerializeField]
        private AudioClip _clip;

        [SerializeField]
        [Range(0f, 1f)]
        private float _volume = 0.8f;

        [SerializeField]
        private float _pitch = 1f;

        public AudioCueId Id => _id;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float Pitch => _pitch;
    }

    [SerializeField]
    private AudioCue[] _cues;

    public bool TryGetCue(
        AudioCueId id,
        out AudioClip clip,
        out float volume,
        out float pitch)
    {
        clip = null;
        volume = 1f;
        pitch = 1f;

        if (_cues == null)
        {
            return false;
        }

        for (int i = 0; i < _cues.Length; i++)
        {
            AudioCue cue = _cues[i];

            if (cue == null || cue.Id != id)
            {
                continue;
            }

            clip = cue.Clip;
            volume = cue.Volume;
            pitch = cue.Pitch;

            return clip != null;
        }

        return false;
    }
}