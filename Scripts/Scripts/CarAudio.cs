using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static Unity.VisualScripting.Member;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAudio : NetworkBehaviour
    {
        // This script reads some of the car's current properties and plays sounds accordingly.
        // The engine sound can be a simple single clip which is looped and pitched, or it
        // can be a crossfaded blend of four clips which represent the timbre of the engine
        // at different RPM and Throttle state.

        public enum EngineAudioOptions
        {
            Simple,
            FourChannel
        }

        public EngineAudioOptions engineSoundStyle = EngineAudioOptions.FourChannel;
        public AudioClip lowAccelClip;
        public AudioClip lowDecelClip;
        public AudioClip highAccelClip;
        public AudioClip highDecelClip;
        public float pitchMultiplier = 1f;
        public float lowPitchMin = 1f;
        public float lowPitchMax = 6f;
        public float highPitchMultiplier = 0.25f;
        public float maxRolloffDistance = 500;
        public float dopplerLevel = 1;
        public bool useDoppler = true;

        private AudioSource m_LowAccel;
        private AudioSource m_LowDecel;
        private AudioSource m_HighAccel;
        private AudioSource m_HighDecel;
        private bool m_StartedSound;
        private CarController m_CarController;
        [SerializeField] private GameObject CarAudioGameObject;
        private List<AudioSource> CarAudios = new();

        private float _simulatedRevs = 0f;

        // --- Neutral-gear rev blip tuning ---
        [Header("Neutral Rev Blip (Death Race style)")]
        [SerializeField] private float revUpSpeed = 4f;       // moderate pitch attack on throttle blip
        [SerializeField] private float revDownSpeed = 1.8f;   // fall-off after release
        [SerializeField] private float idleRevLevel = 0.1f;   // deep idle floor
        [SerializeField, UnityEngine.Range(0.3f, 1f)] private float snapCurvePower = 0.55f; // pitch curve, kept moderate

        [Header("Volume Punch (the actual 'grunt')")]
        [SerializeField] private float punchAttackTime = 0.04f;   // near-instant volume slam on blip
        [SerializeField] private float punchHoldTime = 0.06f;     // stays at peak briefly
        [SerializeField] private float punchDecayTime = 0.4f;     // falls back to sustain level
        [SerializeField, UnityEngine.Range(0f, 1f)] private float punchSustainLevel = 0.55f; // volume level after the hit
        [SerializeField, UnityEngine.Range(0f, 1f)] private float punchStrength = 0.6f;      // how much extra loudness the hit adds

        private float _snapRevTarget = 0f;
        private bool _wasThrottling = false;
        private float _punchTimer = 0f; // time since the current blip started
        private bool StopAddingSounds = false;  
        

        private void StartSound()
        {
            m_CarController = GetComponent<CarController>();

            m_HighAccel = SetUpEngineAudioSource(highAccelClip);

            if (engineSoundStyle == EngineAudioOptions.FourChannel && !StopAddingSounds)
            {

                Task task = AddSounds();
                if(task.IsCompletedSuccessfully)
                {
                    StopAddingSounds = true;
                }

            }

            m_StartedSound = true;
        }

        private async Task AddSounds()
        {
            m_LowAccel = SetUpEngineAudioSource(lowAccelClip);
            m_LowDecel = SetUpEngineAudioSource(lowDecelClip);
            m_HighDecel = SetUpEngineAudioSource(highDecelClip);

            await Task.CompletedTask;
        }

        private void StopSound()
        {
            foreach (var source in GetComponents<AudioSource>())
            {
                if (source != CarAudioGameObject.GetComponent<CarUserControl>().MinigunAudio())
                {
                    Destroy(source);
                }
            }

            m_StartedSound = false;
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (CarAudios != null)
                {
                    foreach (AudioSource source in CarAudios)
                    {
                        if (source != null)
                        {
                            source.volume = Math.Clamp(source.volume, 0, 0.4f);
                        }
                    }
                }

                float camDist = (Camera.main.transform.position - transform.position).sqrMagnitude;

                if (m_StartedSound && camDist > maxRolloffDistance * maxRolloffDistance)
                {
                    StopSound();
                    if (camDist > maxRolloffDistance * maxRolloffDistance)
                    {
                        m_HighAccel.volume = m_HighAccel.volume -= 0.3f * Time.deltaTime;
                        if (m_HighAccel.volume <= 0)
                        {
                            m_HighAccel.volume = 0;
                        }
                    }
                }

                if (!m_StartedSound && camDist < maxRolloffDistance * maxRolloffDistance)
                {
                    StartSound();
                    if (camDist < maxRolloffDistance * maxRolloffDistance)
                    {
                        m_HighAccel.volume = m_HighAccel.volume += 0.3f * Time.deltaTime;
                        if (m_HighAccel.volume >= 0.5f)
                        {
                            m_HighAccel.volume = 0.5f;
                        }
                    }
                }

                if (m_StartedSound)
                {
                    float throttle = Mathf.Abs(m_CarController.AccelInput);
                    float actualSpeed = m_CarController.GetComponent<Rigidbody>().linearVelocity.magnitude;
                    bool isStationary = actualSpeed < 2f;
                    float pitch;

                    if (isStationary)
                    {
                        bool isThrottling = throttle > 0.1f;

                        // Track blip timing for the punch envelope
                        if (isThrottling && !_wasThrottling)
                        {
                            _punchTimer = 0f; // new blip started, reset the hit
                        }
                        if (isThrottling)
                        {
                            _punchTimer += Time.deltaTime;
                        }
                        _wasThrottling = isThrottling;

                        // Moderate pitch envelope — no overshoot, no screaming, just a controlled rise
                        _snapRevTarget = isThrottling ? 1f : idleRevLevel;
                        _simulatedRevs = Mathf.MoveTowards(
                            _simulatedRevs,
                            _snapRevTarget,
                            (isThrottling ? revUpSpeed : revDownSpeed) * Time.deltaTime
                        );

                        float snappedRevs = Mathf.Pow(Mathf.Clamp01(_simulatedRevs), snapCurvePower);
                        pitch = ULerp(lowPitchMin, lowPitchMax, snappedRevs);

                        // --- Volume punch envelope (this is the "grunt") ---
                        // Attack -> brief hold at peak -> decay down to a sustain level, purely on volume
                        float punch;
                        if (!isThrottling)
                        {
                            punch = 0f; // no hit while off-throttle, decel channels take over below
                        }
                        else if (_punchTimer < punchAttackTime)
                        {
                            punch = Mathf.InverseLerp(0f, punchAttackTime, _punchTimer); // 0 -> 1 fast
                        }
                        else if (_punchTimer < punchAttackTime + punchHoldTime)
                        {
                            punch = 1f; // brief peak hold, this is the "hit"
                        }
                        else
                        {
                            float t = Mathf.InverseLerp(punchAttackTime + punchHoldTime,
                                punchAttackTime + punchHoldTime + punchDecayTime,
                                _punchTimer);
                            punch = Mathf.Lerp(1f, punchSustainLevel, t); // settle down to sustain
                        }

                        if (engineSoundStyle == EngineAudioOptions.FourChannel)
                        {
                            m_LowAccel.pitch = pitch * pitchMultiplier;
                            m_LowDecel.pitch = pitch * pitchMultiplier;
                            m_HighAccel.pitch = pitch * highPitchMultiplier * pitchMultiplier;
                            m_HighDecel.pitch = pitch * highPitchMultiplier * pitchMultiplier;

                            // Low channel stays dominant throughout — this is where the "grunt" tone lives
                            float accelWeight = Mathf.Pow(throttle, 0.7f);

                            float lowBase = Mathf.Lerp(0f, 0.75f, accelWeight);
                            float highBase = Mathf.Lerp(0f, 0.5f, accelWeight * snappedRevs);

                            // Apply the punch as an extra multiplier on top of the base volumes
                            float punchMult = 1f + punch * punchStrength;

                            m_LowAccel.volume = Mathf.Clamp01(lowBase * punchMult);
                            m_LowDecel.volume = Mathf.Lerp(0.6f, 0f, accelWeight);
                            m_HighAccel.volume = Mathf.Clamp01(highBase * punchMult);
                            m_HighDecel.volume = Mathf.Lerp(0.3f, 0f, accelWeight);
                        }
                    }
                    else
                    {
                        float speedBlend = Mathf.Clamp01(actualSpeed / 5f);
                        float curvedRevs = Mathf.Pow(m_CarController.Revs, 0.8f);
                        float blendedRevs = Mathf.Lerp(_simulatedRevs, curvedRevs, speedBlend);

                        pitch = ULerp(lowPitchMin, lowPitchMax, blendedRevs);
                        pitch = Mathf.Min(lowPitchMax, pitch);

                        if (engineSoundStyle == EngineAudioOptions.FourChannel)
                        {
                            m_LowAccel.pitch = pitch * pitchMultiplier;
                            m_LowDecel.pitch = pitch * pitchMultiplier;
                            m_HighAccel.pitch = pitch * highPitchMultiplier * pitchMultiplier;
                            m_HighDecel.pitch = pitch * highPitchMultiplier * pitchMultiplier;

                            float accFade = Mathf.Abs(m_CarController.AccelInput);
                            float decFade = 1f - accFade;
                            float highFade = Mathf.InverseLerp(0.2f, 0.8f, m_CarController.Revs);
                            float lowFade = 1f - highFade;

                            highFade = 1f - ((1f - highFade) * (1f - highFade));
                            lowFade = 1f - ((1f - lowFade) * (1f - lowFade));
                            accFade = 1f - ((1f - accFade) * (1f - accFade));
                            decFade = 1f - ((1f - decFade) * (1f - decFade));

                            m_LowAccel.volume = lowFade * accFade;
                            m_LowDecel.volume = lowFade * decFade;
                            m_HighAccel.volume = highFade * accFade;
                            m_HighDecel.volume = highFade * decFade;
                        }
                    }

                    if (engineSoundStyle == EngineAudioOptions.Simple)
                    {
                        m_HighAccel.pitch = pitch * pitchMultiplier * highPitchMultiplier;
                        m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                        if (camDist < maxRolloffDistance * maxRolloffDistance)
                        {
                            m_HighAccel.volume = m_HighAccel.volume += 0.3f * Time.deltaTime;
                            if (m_HighAccel.volume >= 0.7f)
                            {
                                m_HighAccel.volume = 0.7f;
                            }
                        }
                        m_HighAccel.volume = 1;
                    }
                    else
                    {
                        m_LowAccel.pitch = pitch * pitchMultiplier;
                        m_LowDecel.pitch = pitch * pitchMultiplier;
                        m_HighAccel.pitch = pitch * highPitchMultiplier * pitchMultiplier;
                        m_HighDecel.pitch = pitch * highPitchMultiplier * pitchMultiplier;

                        float accFade = Mathf.Abs(m_CarController.AccelInput);
                        float decFade = 1 - accFade;

                        float highFade = Mathf.InverseLerp(0.2f, 0.8f, m_CarController.Revs);
                        float lowFade = 1 - highFade;

                        highFade = 1 - ((1 - highFade) * (1 - highFade));
                        lowFade = 1 - ((1 - lowFade) * (1 - lowFade));
                        accFade = 1 - ((1 - accFade) * (1 - accFade));
                        decFade = 1 - ((1 - decFade) * (1 - decFade));

                        m_LowAccel.volume = lowFade * accFade;
                        m_LowDecel.volume = lowFade * decFade;
                        m_HighAccel.volume = highFade * accFade;
                        m_HighDecel.volume = highFade * decFade;

                        m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                        m_LowAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                        m_HighDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                        m_LowDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    }

                    if (SaveScript.RaceOver == true)
                    {
                        m_HighAccel.volume = 0;
                    }
                }
            }
        }

        private AudioSource SetUpEngineAudioSource(AudioClip clip)
        {
            if (IsOwner)
            {
                //Debug.Log("times");
                AudioSource source = CarAudioGameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = 0;
                source.loop = true;

                source.time = Random.Range(0f, clip.length);
                source.Play();
                source.minDistance = 5;
                source.maxDistance = maxRolloffDistance;
                source.dopplerLevel = 1;
               

                CarAudios.Add(source);
                return source;
            }
            return null;
        }

        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }
    }
}