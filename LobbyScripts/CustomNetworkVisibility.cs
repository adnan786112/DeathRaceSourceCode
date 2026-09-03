using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class CustomNetworkVisibility : NetworkBehaviour
{
    private HashSet<ulong> observers = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UpdateObservers();
        }
    }

    public override void OnNetworkDespawn()
    {
        observers.Clear();
    }

    public void UpdateObservers()
    {
        if (!IsServer) return;

        observers.Clear();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            observers.Add(client.ClientId);
        }

        // Set observers for this network object
        //NetworkObject.ChangeOwnership(NetworkManager.Singleton.ServerClientId); // Ensure server maintains ownership
        //NetworkObject.NetworkManagerOwner = true; // Mark as owned by the server
        //NetworkObject.SetNetworkVariableObservers(observers);
    }
}
