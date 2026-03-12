using UnityEngine;

public class OneShotEffect : MonoBehaviour
{
    [SerializeField]
    private float lifetime = 2f;

    private void Start()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
            ps.Play();
        }

        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        Destroy(gameObject, lifetime);
    }
}
