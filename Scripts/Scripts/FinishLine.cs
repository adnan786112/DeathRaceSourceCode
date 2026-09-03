using Unity.Netcode;
using UnityEngine;

public class FinishLine : NetworkBehaviour
{
    public static int PlayerFinishPosition;
    public static string PName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            NetworkObject netObj = other.gameObject.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsLocalPlayer) return;

            SaveScript.RaceOver = true;
            Lap.instance.RegisterFinishServerRpc(new NetworkObjectReference(netObj));
        }
    }
}