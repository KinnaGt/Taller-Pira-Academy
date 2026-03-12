using UnityEngine;

public class SceneAudioStarter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private AudioConfigSO _bgmConfig;

    [SerializeField]
    private AudioEventChannelSO _bgmChannel;

    private void Start()
    {
        if (_bgmConfig != null && _bgmChannel != null)
        {
            _bgmConfig.Play(_bgmChannel);
        }
    }
}
