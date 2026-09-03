using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

public class Activator1 : NetworkBehaviour
{
    public static bool RoadblockTimeOver = false;

    private void OnTriggerEnter(Collider other)
    {
       
        if (other.gameObject.CompareTag("Progress"))
        {
            if (IsOwner)
            {
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            }
            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()
                ?.GetComponent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
            {
                FinishPointManager.instance.ActivatePlayerFinishPoint();
            }
        }
        if (other.gameObject.CompareTag("ProgressAI1"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(1);

        }
        if (other.gameObject.CompareTag("ProgressAI2"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(2);
        }
        if (other.gameObject.CompareTag("ProgressAI3"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(3);
        }
        if (other.gameObject.CompareTag("ProgressAI4"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(4);
        }
        if (other.gameObject.CompareTag("ProgressAI5"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(5);
        }
        if (other.gameObject.CompareTag("ProgressAI6"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(6);
        }
        if (other.gameObject.CompareTag("ProgressAI7"))
        {
            if (IsOwner)
                HalfwayActivatedServerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
            if (other.gameObject.GetComponentInParent<PlayerData>().CarLapPosition.Value == SaveScript.MaxLaps)
                FinishPointManager.instance.ActivateAIFinishPoint(7);
        }
    }
    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void HalfwayActivatedServerRpc(NetworkObjectReference PlayerCarRef)
    {

        PlayerCarRef.TryGet(out NetworkObject PlayerCar);
        PlayerCar.GetComponent<PlayerData>().HalfWayActivated.Value = true;
       

    }
}