using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioUIMixerController : MonoBehaviour
{
    [SerializeField]
    private AudioMixer _mainMixer;

    [Header("UI References")]
    [SerializeField]
    private Slider _masterSlider;

    [SerializeField]
    private Slider _sfxSlider;

    [SerializeField]
    private Slider _bgmSlider;

    [Header("Exposed Parameters")]
    [SerializeField]
    private string _masterParam = "MasterVolume";

    [SerializeField]
    private string _sfxParam = "SFXVolume";

    [SerializeField]
    private string _bgmParam = "BGMVolume";

    [SerializeField]
    private CanvasGroup _audioPanel;

    [Header("Button Images")]
    [SerializeField]
    Image _btnToggle;

    [SerializeField]
    private Sprite _toggledImage;

    [SerializeField]
    private Sprite _untoggledImage;

    private bool _isPanelVisible = false;

    [Header("Event Channel")]
    [SerializeField]
    private AudioEventChannelSO _sfxChannel;

    [Header("Audio Configs")]
    [SerializeField]
    private AudioConfigSO _clickSfx;

    private void Start()
    {
        float masterVol = PlayerPrefs.GetFloat(_masterParam, 1f);
        float sfxVol = PlayerPrefs.GetFloat(_sfxParam, 1f);
        float bgmVol = PlayerPrefs.GetFloat(_bgmParam, 1f);

        _masterSlider.SetValueWithoutNotify(masterVol);
        _sfxSlider.SetValueWithoutNotify(sfxVol);
        _bgmSlider.SetValueWithoutNotify(bgmVol);

        SetVolume(_masterParam, masterVol);
        SetVolume(_sfxParam, sfxVol);
        SetVolume(_bgmParam, bgmVol);
    }

    private void OnEnable()
    {
        _masterSlider.onValueChanged.AddListener(SetMasterVolume);
        _sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        _bgmSlider.onValueChanged.AddListener(SetBGMVolume);
    }

    private void OnDisable()
    {
        _masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        _sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
        _bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
    }

    private void SetMasterVolume(float linearValue)
    {
        SetVolume(_masterParam, linearValue);
        PlayerPrefs.SetFloat(_masterParam, linearValue);
    }

    private void SetSFXVolume(float linearValue)
    {
        SetVolume(_sfxParam, linearValue);
        PlayerPrefs.SetFloat(_sfxParam, linearValue);
    }

    private void SetBGMVolume(float linearValue)
    {
        SetVolume(_bgmParam, linearValue);
        PlayerPrefs.SetFloat(_bgmParam, linearValue);
    }

    private void SetVolume(string exposedParam, float linearValue)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, linearValue)) * 20f;
        _mainMixer.SetFloat(exposedParam, dB);
    }

    public void ToggleAudioPanel()
    {
        _clickSfx.Play(_sfxChannel);
        _isPanelVisible = !_isPanelVisible;
        _audioPanel.interactable = _isPanelVisible;
        _audioPanel.blocksRaycasts = _isPanelVisible;
        _btnToggle.sprite = _isPanelVisible ? _toggledImage : _untoggledImage;
        StartCoroutine(FadeCanvasGroup(_audioPanel, _isPanelVisible ? 1f : 0f, 0.5f));
    }

    private System.Collections.IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float targetAlpha,
        float duration
    )
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}
