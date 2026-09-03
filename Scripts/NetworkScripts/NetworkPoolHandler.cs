using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Bridges Unity Netcode's spawn/despawn system into your existing MainObjectPooler.
/// Attach one of these per pooled networked prefab type (MinigunBullet, Rocket, etc).
/// 
/// HOW IT WORKS:
/// Normally Netcode calls Instantiate() when spawning and Destroy() when despawning.
/// By registering INetworkPrefabInstanceHandler, we intercept both calls:
///   - Instantiate → pull from your existing pool instead of creating a new object
///   - Destroy     → return to your existing pool instead of destroying the object
/// 
/// This means netObj.Despawn() on the server propagates to ALL clients via Netcode,
/// and on every machine the Destroy() intercept fires — returning to pool instead of
/// actually destroying. Non-owner clients no longer clog up with stale bullet objects.
/// </summary>
public class NetworkPoolHandler : INetworkPrefabInstanceHandler
{
    private readonly GameObject _prefab;
    private readonly ObjectType _objectType;

    public NetworkPoolHandler(GameObject prefab, ObjectType objectType)
    {
        _prefab = prefab;
        _objectType = objectType;
    }

    // Called by Netcode on ALL machines INSTEAD of Instantiate when this prefab spawns.
    // We pull from your existing pool instead.
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        GameObject obj = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(_objectType);

        if (obj == null)
        {
            // Pool exhausted — fallback to real instantiate so nothing breaks
            Debug.LogWarning($"[NetworkPoolHandler] Pool exhausted for {_objectType}, instantiating directly.");
            obj = UnityEngine.Object.Instantiate(_prefab, position, rotation);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj.GetComponent<NetworkObject>();
    }

    // Called by Netcode on ALL machines INSTEAD of Destroy when this prefab despawns.
    // We return to your existing pool instead of destroying.
    // This is what fixes the non-owner client clog — despawn propagates to every client
    // via Netcode, and this intercept fires on every machine, returning to pool everywhere.
    public void Destroy(NetworkObject networkObject)
    {
        MainObjectPooler.instance.ReturnObjectToPoolRpc(networkObject.gameObject, _objectType);
    }
}
