using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MeshCullScript : NetworkBehaviour
{
    // Server-only: tracks which clients currently report this decal as visible
    private HashSet<ulong> viewers = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        viewers.Clear();
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnect;
            StartCoroutine(MaxLifetimeFailsafe());
        }
    }

    private IEnumerator MaxLifetimeFailsafe()
    {
        yield return new WaitForSeconds(20f); // generous upper bound
        if (IsSpawned && transform.parent == null)
        {
            TryDespawn();
        }
    }
   

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
        viewers.Clear();
    }

    private void OnBecameVisible()
    {
        if (!IsSpawned) return;
        ReportVisibilityRpc(true);
    }

    private void OnBecameInvisible()
    {
        if (!IsSpawned) return;
        ReportVisibilityRpc(false);
    }

    // Any client can call this, not just the owner — matches your DespawnBulletServerRpc pattern
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportVisibilityRpc(bool isVisible, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (isVisible) viewers.Add(sender);
        else viewers.Remove(sender);

        if (viewers.Count == 0)
        {
            TryDespawn();
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        viewers.Remove(clientId);
        if (viewers.Count == 0)
        {
            TryDespawn();
        }
    }

    private void TryDespawn()
    {
        if (!IsServer || !IsSpawned) return;

        // This is the only line that actually does anything —
        // Despawn() triggers Netcode's Destroy() call on every machine,
        // which your NetworkPoolHandler already intercepts and routes
        // straight into MainObjectPooler. No extra pool call needed here.
        NetworkObject.Despawn();
    }
}