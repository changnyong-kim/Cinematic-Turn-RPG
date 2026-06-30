using UnityEngine;
using UnityEngine.Playables;

public sealed class AudioCueBehaviour : PlayableBehaviour
{
    public AudioCueId CueId;
    public float VolumeMultiplier = 1f;
    public float PitchMultiplier = 1f;

    private bool _isPlayed;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_isPlayed)
        {
            return;
        }

        _isPlayed = true;

        if (CueId == AudioCueId.None)
        {
            return;
        }

        AudioManager.Instance?.PlaySfx(
            CueId,
            VolumeMultiplier,
            PitchMultiplier);
    }

    public override void OnGraphStart(Playable playable)
    {
        _isPlayed = false;
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (playable.GetTime() <= 0.0)
        {
            _isPlayed = false;
        }
    }
}