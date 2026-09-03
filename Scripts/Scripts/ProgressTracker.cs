using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProgressTracker : NetworkBehaviour
{
 

    public NetworkVariable<int> CurrentWP = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    public int ThisWPNumber = 0;
    public int LastWPNumber = 0;
    public Transform[] waypoints; // Assigned in Inspector or dynamically
    public static ProgressTracker localInstance;
    private PlayerData playerData;
    private bool LapChange = false;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
            localInstance = this;
        playerData = gameObject.GetComponentInParent<PlayerData>();

    }
  
    public Transform GetNextWaypoint()
    {
        if(IsLocalPlayer)
        //Debug.Log(CurrentWP.Value);


        if (CurrentWP.Value < waypoints.Length - 1)
        {
       
            return waypoints[CurrentWP.Value + 1];
        }
        
      
        return waypoints[0];
        
        
         
    }

    


    void Update()
    {
        if (IsLocalPlayer)
        {
            UpdateTrackingRpc();
        }
        if (SaveScript.RaceStart)
        {
            if (IsOwner)
            {
                RequestProgressUpdateServerRpc(CurrentWP.Value);
                
                if (LapChange)
                {

                    CurrentWpResetServerRpc();

                }
            }
              
        }
       


    }
    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
   
    public void RequestProgressUpdateServerRpc(int currentWP)
    {
        if (playerData != null)
        {
            playerData.CurrentWaypoint.Value = currentWP;
            Transform  nextWaypoint = GetNextWaypoint();                     
            if (nextWaypoint != null)
            {
                float dist = Vector3.Distance(transform.position, nextWaypoint.position);
                playerData.DistanceToNextWaypoint.Value = dist;
            }
        }
 
    }

    IEnumerator CheckDirection()
    {
        yield return new WaitForSeconds(0.5f);
        ThisWPNumber = LastWPNumber;
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateTrackingRpc()
    {      
        if (CurrentWP.Value > LastWPNumber)
        {

            StartCoroutine(CheckDirection());
        }
        if (LastWPNumber > ThisWPNumber)
        {
            SaveScript.WrongWay = false;
        }
        if (LastWPNumber < ThisWPNumber)
        {
            SaveScript.WrongWay = true;
        }
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void CurrentWpResetServerRpc()
    {
        CurrentWP.Value = 0;
    }

    public void OnLapChange(int oldValue, int newValue)
    {
        LapChange = true;
        if (playerData != null)
            ProgressWaypoints.ResetLapTrackingForCarOnAllWaypoints(playerData.NetworkObjectId);
        StartCoroutine(LapReset());
    }

    IEnumerator LapReset()
    {
        yield return new WaitForSeconds(0.5f);
        LapChange = false;
    }
}
