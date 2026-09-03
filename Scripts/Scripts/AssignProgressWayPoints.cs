using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AssignProgressWayPoints : MonoBehaviour
{ 
   private int[] ProgressWayPointNumber;
   public static AssignProgressWayPoints instance;
   [SerializeField] private int numberOFWaypoints;

    private void Awake()
    {
        instance = this;    
    }
    private void OnEnable()
    {
        AssignprogressWayPoint();
    }
   

    public void AssignprogressWayPoint()
    {
        ProgressWayPointNumber = new int[numberOFWaypoints];

        for (int i = 0; i < ProgressWayPointNumber.Length; i++)
        {
            transform.GetChild(i).GetComponent<ProgressWaypoints>().WPNumber = i + 1;

        }
    }
    public int GetProgressWaypoints => numberOFWaypoints;
}
