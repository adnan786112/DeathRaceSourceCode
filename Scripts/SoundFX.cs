using System.Collections;
using UnityEngine;

public class SoundFX : MonoBehaviour
{
    [SerializeField] private AudioSource BulletAudioSource;
    [SerializeField] private float thresholdTime;

    public static SoundFX instance;
    private float elapsedTime = 0;
   
    private void Awake()
    {
        
        instance = this;
    }

    private void Start()
    {
       
        AudioListener.volume = 0.4f;
    }

    public IEnumerator PlayClip(AudioClip clip,bool isRocket)
    {
        
        if (!isRocket)
        {
            elapsedTime += Time.deltaTime;
            while (elapsedTime > thresholdTime)
            {
                elapsedTime = 0;
                yield return null;
                BulletAudioSource.PlayOneShot(clip);
            }
        }
        else
        {
            BulletAudioSource.clip = clip;
            BulletAudioSource.Play();
        }
    }

}
