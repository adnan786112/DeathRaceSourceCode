using Cinemachine;
using UnityEngine;

public class CollisionScript : MonoBehaviour
{
    [Header("Car Dash Audio")]
    [SerializeField] private AudioSource carDashSoundAudioSource;
    [SerializeField] private AudioClip[] carDashClips;
    [SerializeField] private float minimumCarSpeed = 2f;
    [SerializeField] private float shakeSensitivity = 40f;  
    [SerializeField] private CinemachineImpulseSource CameraShake;
    [SerializeField] private float CameraShakeIntensity = 0.5f;

    private void OnCollisionEnter(Collision collision)
    {
     
        if (collision.gameObject.layer != 7) return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minimumCarSpeed) return;

        float normalizedIntensity = 1f - Mathf.Exp(-speed / shakeSensitivity);
        //Debug.Log($"<color=red>{normalizedIntensity}</color>");

        CameraShake.GenerateImpulse(normalizedIntensity * CameraShakeIntensity);
        carDashSoundAudioSource.PlayOneShot(GenrateRandomDashSound());
    }

    private AudioClip GenrateRandomDashSound()
    {
        int r = UnityEngine.Random.Range(0, 3);
        switch (r)
        {
            case 0:
                return carDashClips[0];

            case 1:
                return carDashClips[1];

            case 2:
                return carDashClips[2];

        }
        return null;
    }
}