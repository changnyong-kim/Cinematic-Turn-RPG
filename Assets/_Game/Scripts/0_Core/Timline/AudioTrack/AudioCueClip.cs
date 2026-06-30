using UnityEngine;
using UnityEngine.Playables;

public sealed class AudioCueClip : PlayableAsset
{
    [SerializeField]
    private AudioCueId _cueId;

    [SerializeField]
    [Range(0f, 2f)]
    private float _volumeMultiplier = 1f;

    [SerializeField]
    [Range(0.1f, 3f)]
    private float _pitchMultiplier = 1f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<AudioCueBehaviour>.Create(graph);

        AudioCueBehaviour behaviour = playable.GetBehaviour();

        behaviour.CueId = _cueId;
        behaviour.VolumeMultiplier = _volumeMultiplier;
        behaviour.PitchMultiplier = _pitchMultiplier;

        return playable;
    }
}