using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class CollectibleItem : MonoBehaviour
{
    public static event Action<int> OnCollected;

    [Header("Configuración")]
    [SerializeField]
    private int scoreValue = 1;

    [SerializeField]
    private LayerMask collectorLayer;

    [Header("Feedback (Prefab)")]
    [Tooltip("Prefab que contiene el script OneShotEffect, AudioSource y ParticleSystem")]
    [SerializeField]
    private GameObject pickupVFXPrefab;

    private bool _isCollected = false;

    [Header("Event Channel")]
    [SerializeField]
    private AudioEventChannelSO _sfxChannel;

    [Header("Audio Configs")]
    [SerializeField]
    private AudioConfigSO _coinSfx;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isCollected)
            return;

        if ((collectorLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            Collect();
        }
    }

    private void Collect()
    {
        _isCollected = true;

        OnCollected?.Invoke(scoreValue);
        _coinSfx.Play(_sfxChannel);
        if (pickupVFXPrefab != null)
        {
            Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
