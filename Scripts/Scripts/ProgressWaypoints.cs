using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor;
using UnityEngine;

public class ProgressWaypoints : NetworkBehaviour
{
    public int WPNumber = 0;
    public int CarTracking = 0;
    public bool PenaltyOption = false;
    public int PenaltyWayPoint;
    public int Position = 0;

    public static ProgressWaypoints instance;
    private HashSet<ulong> lap1AlreadyCounted = new HashSet<ulong>();

    public static readonly List<ProgressWaypoints> AllWaypoints = new();

    private void Awake()
    {
        instance = this;
        AllWaypoints.Add(this);
    }

    public override void OnDestroy()
    {
        AllWaypoints.Remove(this);
    }

    public void ResetLapTrackingFor(ulong networkObjectId)
    {
        lap1AlreadyCounted.Remove(networkObjectId);
    }

    public static void ResetLapTrackingForCarOnAllWaypoints(ulong networkObjectId)
    {
        foreach (var wp in AllWaypoints)
            wp.ResetLapTrackingFor(networkObjectId);
    }



    private void OnTriggerEnter(Collider other)
    {
        if(!IsOwner)
            return;        
    
        if (other.gameObject.CompareTag("Progress") || other.gameObject.CompareTag("ProgressAI1") || other.gameObject.CompareTag("ProgressAI2")
            || other.gameObject.CompareTag("ProgressAI3") || other.gameObject.CompareTag("ProgressAI4") || other.gameObject.CompareTag("ProgressAI5")
            || other.gameObject.CompareTag("ProgressAI6") || other.gameObject.CompareTag("ProgressAI7"))
        {
            
            NetworkObject networkObject = other.gameObject.GetComponentInParent<NetworkObject>();
            if (networkObject != null)
            {

                NetworkObjectReference playerGameObject = new(networkObject);                
                UpdatePositionOfAllCarsRpc(playerGameObject);
                
            }
        }      

    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    [Rpc(SendTo.Everyone)]
    public void UpdatePositionOfAllCarsRpc(NetworkObjectReference playerGameObject)
    {

        if (playerGameObject.TryGet(out NetworkObject networkObject))
        {
            if(lap1AlreadyCounted.Contains(networkObject.NetworkObjectId))
            {
                return;
            }
            lap1AlreadyCounted.Add(networkObject.NetworkObjectId);
            var tracker = networkObject.gameObject.transform.GetChild(0).GetComponent<ProgressTracker>();
            if (networkObject.gameObject.transform.GetChild(0).CompareTag("Progress"))
            {
             
                CarTracking = networkObject.gameObject.transform.GetChild(0).GetComponent<ProgressTracker>().CurrentWP.Value;
                                
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);

                
                if (CarTracking > WPNumber)
                {
                    networkObject.gameObject.transform.GetChild(0).GetComponent<ProgressTracker>().LastWPNumber = WPNumber;
                }
               
            }

            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI1"))
            {                              
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);

            }



            if (networkObject.gameObject.transform.GetChild(0).CompareTag("ProgressAI2"))
            {
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);               
                
            }


            if (networkObject.gameObject.transform.GetChild(0).CompareTag("ProgressAI3"))
            {                            
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);
              
            }


            if (networkObject.gameObject.transform.GetChild(0).CompareTag("ProgressAI4"))
            {               
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);                             

            }


            if (networkObject.gameObject.transform.GetChild(0).CompareTag("ProgressAI5"))
            {               
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);
              
            }


            if (networkObject.gameObject.transform.GetChild(0).CompareTag("ProgressAI6"))
            {
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);
                               
            }


            if (networkObject.gameObject.transform.GetChild(0).CompareTag("ProgressAI7"))
            {
               
                if (WPNumber > tracker.CurrentWP.Value && IsOwner)
                    RequestWaypointUpdateServerRpc(WPNumber, networkObject);
                
            }
        }
    }
    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestWaypointUpdateServerRpc(int wpNumber, NetworkObjectReference netRef)
    {
        if (netRef.TryGet(out NetworkObject obj))
        {
            var tracker = obj.transform.GetChild(0).GetComponent<ProgressTracker>();          
            tracker.CurrentWP.Value = wpNumber;
            
           
        }
    }
  

   
}
