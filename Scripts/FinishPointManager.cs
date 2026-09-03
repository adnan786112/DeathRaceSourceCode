using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class FinishPointManager : MonoBehaviour
{
    public static FinishPointManager instance;

    // Assign these in Inspector — they are scene objects
    public GameObject FinishPoint;
    public GameObject FinishPointAI1;
    public GameObject FinishPointAI2;
    public GameObject FinishPointAI3;
    public GameObject FinishPointAI4;
    public GameObject FinishPointAI5;
    public GameObject FinishPointAI6;
    public GameObject FinishPointAI7;

    private FinishCinematic FinishCinematic;
    private bool cinematicFound = false;

    private void Awake() => instance = this;



    // Called by FinishLine when local player crosses finish
    public void OnLocalPlayerFinished(Transform playerTransform, int finishPosition , FinishCinematic finishCinematic)
    {
        if (finishCinematic != null)
            finishCinematic.TriggerFinishCinematic(playerTransform, finishPosition);
      
    }

    // Pass finish point refs to Activator1 at runtime
    public void ActivatePlayerFinishPoint()
    {
        if (FinishPoint != null) FinishPoint.SetActive(true);
    }

    public void ActivateAIFinishPoint(int aiIndex)
    {
        switch (aiIndex)
        {
            case 1: if (FinishPointAI1 != null) FinishPointAI1.SetActive(true); break;
            case 2: if (FinishPointAI2 != null) FinishPointAI2.SetActive(true); break;
            case 3: if (FinishPointAI3 != null) FinishPointAI3.SetActive(true); break;
            case 4: if (FinishPointAI4 != null) FinishPointAI4.SetActive(true); break;
            case 5: if (FinishPointAI5 != null) FinishPointAI5.SetActive(true); break;
            case 6: if (FinishPointAI6 != null) FinishPointAI6.SetActive(true); break;
            case 7: if (FinishPointAI7 != null) FinishPointAI7.SetActive(true); break;
        }
    }
}