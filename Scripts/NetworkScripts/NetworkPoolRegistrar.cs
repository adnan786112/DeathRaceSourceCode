using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attach this to the same GameObject as MainObjectPooler (or any scene object).
/// It registers each of your pooled networked prefabs with Netcode's PrefabHandler
/// so that NetworkPoolHandler intercepts spawn/despawn calls for them.
/// 
/// Only register NETWORKED pooled prefabs here (MinigunBullet, Rocket).
/// Do NOT register visual-only non-networked prefabs (VisualMiniBullet, VisualRocket)
/// since those don't go through Netcode's spawn system at all.
/// </summary>
public class NetworkPoolRegistrar : MonoBehaviour
{
    [Header("Only assign NETWORKED pooled prefabs here")]
    [SerializeField] private GameObject minigunBulletPrefab;
    [SerializeField] private GameObject DecalPrefab;
    [SerializeField] private GameObject BulletHitEffectPrefab;
    [SerializeField] private GameObject rocketPrefab;

    private void Start()
    {
        // Wait until NetworkManager is ready before registering
        if (NetworkManager.Singleton == null)
        {
            //Debug.LogError("[NetworkPoolRegistrar] NetworkManager.Singleton is null — " +
                           //"make sure this script runs after NetworkManager initializes.");
            return;
        }

        RegisterHandler(minigunBulletPrefab, ObjectType.MinigunBullet);
        RegisterHandler(rocketPrefab, ObjectType.Rocket);
        RegisterHandler(DecalPrefab, ObjectType.Decal);
        RegisterHandler(BulletHitEffectPrefab, ObjectType.BulletHitEffect);
    }

    private void RegisterHandler(GameObject prefab, ObjectType type)
    {
        if (prefab == null)
        {
            //Debug.LogWarning($"[NetworkPoolRegistrar] Prefab for {type} is not assigned — skipping.");
            return;
        }

        NetworkPoolHandler handler = new NetworkPoolHandler(prefab, type);
        NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, handler);
        //Debug.Log($"[NetworkPoolRegistrar] Registered pool handler for {type}.");
    }

    private void OnDestroy()
    {
        // Unregister cleanly when scene tears down
        if (NetworkManager.Singleton == null) return;

        if (minigunBulletPrefab != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(minigunBulletPrefab);
        }
        if (rocketPrefab != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(rocketPrefab);
        }
        if(DecalPrefab != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(DecalPrefab);
        }
        if (BulletHitEffectPrefab != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(BulletHitEffectPrefab);
        }
    }
}
