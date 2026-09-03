using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WheelEffects : NetworkBehaviour
{
    public Transform SkidTrailPrefab;
    public static Transform skidTrailsDetachedParent;
    public ParticleSystem skidParticles;
    public bool skidding { get; private set; }
    public bool PlayingAudio { get; private set; }
    private AudioSource m_AudioSource;
    private Transform m_SkidTrail;
    private WheelCollider m_WheelCollider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Start()
    {
        //skidParticles = transform.root.GetComponentInChildren<ParticleSystem>();
        if (skidParticles == null)
        {
            Debug.LogWarning("no particle system found on car to generate smoke particles", gameObject);
        }
        else
        {
            skidParticles.Stop();
        }
        m_WheelCollider = GetComponent<WheelCollider>();
        m_AudioSource = GetComponent<AudioSource>();
        PlayingAudio = false;
        if (skidTrailsDetachedParent == null)
        {
            skidTrailsDetachedParent = new GameObject("Skid Trails - Detached").transform;
        }
    }

    public void EmitTyreSmoke()
    {
        skidParticles.transform.position = transform.position - transform.up * m_WheelCollider.radius;
        skidParticles.Emit(1);
        if (!skidding)
        {
            StartCoroutine(StartSkidTrail());
        }
    }

    // Cinematic version: emit a burst scaled by intensity (0-1).
    // intensity 1 = full handbrake/launch smoke, lower = lighter revving smoke.
    [Rpc(SendTo.Everyone)]
    public void EmitCinematicSmokeRpc(float intensity)
    {
        if (skidParticles == null) return;
        skidParticles.transform.position = transform.position - transform.up * m_WheelCollider.radius;

        // Scale emit count: 1 particle at low intensity, up to 2 at full intensity
        int emitCount = Mathf.RoundToInt(Mathf.Lerp(1f, 1.5f, intensity));
        skidParticles.Emit(emitCount);

        if (intensity > 0.4f && !skidding)
        {
            StartCoroutine(StartSkidTrail());
        }
        else if (intensity <= 0.1f)
        {
            EndSkidTrail();
        }
    }

    public void PlayAudio()
    {
        m_AudioSource.Play();
        PlayingAudio = true;
    }

    public void StopAudio()
    {
        m_AudioSource.Stop();
        PlayingAudio = false;
    }

    public IEnumerator StartSkidTrail()
    {
        skidding = true;
        m_SkidTrail = Instantiate(SkidTrailPrefab);
        while (m_SkidTrail == null)
        {
            yield return null;
        }
        m_SkidTrail.parent = transform;
        m_SkidTrail.localPosition = -Vector3.up * m_WheelCollider.radius;
    }

    public void EndSkidTrail()
    {
        if (!skidding)
        {
            return;
        }
        skidding = false;
        m_SkidTrail.parent = skidTrailsDetachedParent;
        Destroy(m_SkidTrail.gameObject, 10);
    }
}
