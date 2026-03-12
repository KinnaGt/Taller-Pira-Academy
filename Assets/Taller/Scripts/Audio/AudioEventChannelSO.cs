using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SFXEventChannel", menuName = "Events/Audio Event Channel")]
public class AudioEventChannelSO : ScriptableObject
{
    public event Action<AudioConfigSO> OnAudioRequested;

    public void RaiseEvent(AudioConfigSO audioConfig)
    {
        OnAudioRequested?.Invoke(audioConfig);
    }
}
