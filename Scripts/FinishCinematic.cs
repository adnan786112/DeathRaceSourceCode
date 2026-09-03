using Cinemachine;
using System.Collections;
using UnityEngine;

public class FinishCinematic : MonoBehaviour
{


    [SerializeField] private float slowMoScale = 0.3f;
    [SerializeField] private float cinematicDuration = 3f;

    [Header("Phase 1 - Hero Rolling Shot")]
    [SerializeField] private CinemachineVirtualCamera cinematicCameraPhase1;
    [SerializeField] private float phase1Duration = 3f;


    [Header("Phase 2 - Zoom Out Reveal")]
    [SerializeField] private CinemachineVirtualCamera cinematicCameraPhase2;
    [SerializeField] private float phase2Duration = 3f;  

    [Header("Phase 3 - High Angle, Front-Favoring")]
    [SerializeField] private CinemachineVirtualCamera cinematicCameraPhase3;
    [SerializeField] private float phase3HoldDuration = 1.5f;
   

    [Header("Phase 4 - Zoom Past (Forza-style whip pan)")]
    [SerializeField] private float phase4HoldDuration = 1.5f;
    [SerializeField] private CinemachineVirtualCamera cinematicCameraPhase4;
   
    public static FinishCinematic instance;
    private bool cinematicPlaying = false;

    private void Awake()
    {
        instance = this;
    
    }

    private void OnEnable()
    {
        cinematicCameraPhase1.gameObject.SetActive(false);
    }
    public void TriggerFinishCinematic(Transform car, int finishPosition)
    {
        UIScript.instance.GetLeaderboardCanvas.SetActive(false);
        if (cinematicPlaying) return;
   
        cinematicPlaying = true;
        StartCoroutine(CinematicRoutine(car, finishPosition));
    }
    private IEnumerator CinematicRoutine(Transform car, int finishPosition)
    {
        cinematicCameraPhase1.gameObject.SetActive(true);
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;


        if (cinematicCameraPhase1 != null) cinematicCameraPhase1.Priority = 20;

        float holdDuration = phase1Duration;
        cinematicDuration -= holdDuration;
        float elapsed = 0f;


        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;


            yield return null;
        }
        ToggleCameraActiveStates(cinematicCameraPhase1.gameObject, cinematicCameraPhase2.gameObject);
        
        float holdDurationPhase2 = phase2Duration;
        cinematicDuration -= holdDurationPhase2;
        elapsed = 0f;
        while (elapsed < holdDurationPhase2)
        {
            elapsed += Time.unscaledDeltaTime;


            yield return null;
        }
        ToggleCameraActiveStates(cinematicCameraPhase2.gameObject, cinematicCameraPhase3.gameObject);
        float holdDurationPhase3 = phase3HoldDuration;
        cinematicDuration -= holdDurationPhase3;
        elapsed = 0f;
 
        while (elapsed < holdDurationPhase3)
        {
            elapsed += Time.unscaledDeltaTime;


            yield return null;
        }

        ToggleCameraActiveStates(cinematicCameraPhase3.gameObject, cinematicCameraPhase4.gameObject);
        float holdDurationPhase4 =  phase4HoldDuration;
        cinematicDuration -= holdDurationPhase4;
        elapsed = 0f;

        while (elapsed < holdDurationPhase4)
        {
            elapsed += Time.unscaledDeltaTime;


            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        UIScript.instance.ShowFinishBanner(finishPosition);
    }
    public void ToggleCameraActiveStates(GameObject CameraOff,GameObject CameraOn)
    {
        CameraOff.SetActive(false);
        CameraOn.SetActive(true);
       
    }



}