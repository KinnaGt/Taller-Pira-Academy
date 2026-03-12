using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Event Channel")]
    [SerializeField]
    private AudioEventChannelSO _sfxChannel;

    [Header("Audio Configs")]
    [SerializeField]
    private AudioConfigSO _jumpSfx;

    [SerializeField]
    private AudioConfigSO _landSfx;

    [SerializeField]
    private AudioConfigSO _hitSfx;

    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        _playerController.OnJumpPerformed += PlayJumpAudio;
        _playerController.OnLandPerformed += PlayLandAudio;
        PlayerController.OnHitPerformed += PlayHitAudio;
    }

    private void OnDisable()
    {
        _playerController.OnJumpPerformed -= PlayJumpAudio;
        _playerController.OnLandPerformed -= PlayLandAudio;
        PlayerController.OnHitPerformed -= PlayHitAudio;
    }

    private void PlayJumpAudio() => _jumpSfx.Play(_sfxChannel);

    private void PlayLandAudio() => _landSfx.Play(_sfxChannel);

    private void PlayHitAudio() => _hitSfx.Play(_sfxChannel);
}
