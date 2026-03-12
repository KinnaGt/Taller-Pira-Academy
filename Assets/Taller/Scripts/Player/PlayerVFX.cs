using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private PlayerController playerController;

    [Header("Sistemas (Auto-Configurados en Awake)")]
    [SerializeField]
    private ParticleSystem jumpDust;

    [SerializeField]
    private ParticleSystem landDust;

    [SerializeField]
    private ParticleSystem hitEffect;

    [Header("Configuración de Emisión")]
    [SerializeField]
    private int jumpParticlesCount = 6;

    [SerializeField]
    private int landParticlesCount = 10;

    [SerializeField]
    private int hitParticlesCount = 15;

    [Header("Offsets")]
    [SerializeField]
    private Vector3 footOffset = new Vector3(0, -0.5f, 0);

    [SerializeField]
    private Vector3 centerOffset = Vector3.zero;

    private void Awake()
    {
        ConfigureParticleSystem(jumpDust);
        ConfigureParticleSystem(landDust);
        ConfigureParticleSystem(hitEffect);
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnJumpPerformed += PlayJumpDust;
            playerController.OnLandPerformed += PlayLandDust;
            PlayerController.OnHitPerformed += PlayHitVFX;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnJumpPerformed -= PlayJumpDust;
            playerController.OnLandPerformed -= PlayLandDust;
            PlayerController.OnHitPerformed -= PlayHitVFX;
        }
    }

    private void ConfigureParticleSystem(ParticleSystem ps)
    {
        if (ps == null)
            return;

        var main = ps.main;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 0;
    }

    private void PlayJumpDust() => EmitDust(jumpDust, jumpParticlesCount, footOffset);

    private void PlayLandDust() => EmitDust(landDust, landParticlesCount, footOffset);

    private void PlayHitVFX() => EmitDust(hitEffect, hitParticlesCount, centerOffset);

    private void EmitDust(ParticleSystem system, int count, Vector3 posOffset)
    {
        if (system == null)
            return;

        system.transform.position = transform.position + posOffset;

        if (system.isStopped)
        {
            system.Play();
        }

        system.Emit(count);
    }
}
