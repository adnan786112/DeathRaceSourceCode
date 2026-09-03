using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class FinishLineAI : NetworkBehaviour

{
    public static int PlayerFinishPosition;
    public static string PName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("ProgressAI1") || other.gameObject.CompareTag("ProgressAI2") || other.gameObject.CompareTag("ProgressAI3") ||
            other.gameObject.CompareTag("ProgressAI4") || other.gameObject.CompareTag("ProgressAI5") || other.gameObject.CompareTag("ProgressAI6")||
            other.gameObject.CompareTag("ProgressAI7"))
        {

            NetworkObject netObj = other.gameObject.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsLocalPlayer) return;

            SaveScript.RaceOver = true;

            Lap.instance.RegisterFinishServerRpc(new NetworkObjectReference(netObj));
        }
    }

}
