using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PooledEffectAutoDespawn : NetworkBehaviour
{
    [SerializeField] private float lifetime = 0.1f;
    private Coroutine despawnRoutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            despawnRoutine = StartCoroutine(DespawnAfterDelay());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
            despawnRoutine = null;
        }
    }

    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(lifetime);
        if (IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}