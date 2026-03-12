using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewAudioConfig", menuName = "Audio/Audio Config")]
public class AudioConfigSO : ScriptableObject
{
    public AudioClip Clip;
    public AudioMixerGroup MixerGroup;

    [Range(0f, 1f)]
    public float Volume = 1f;

    [Range(0.1f, 3f)]
    public float MinPitch = 0.95f;

    [Range(0.1f, 3f)]
    public float MaxPitch = 1.05f;
    public bool Loop = false;

    public void Play(AudioEventChannelSO channel)
    {
        if (Clip != null && channel != null)
        {
            channel.RaiseEvent(this);
        }
    }
}
