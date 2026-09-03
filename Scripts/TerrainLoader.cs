using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TerrainLoader : MonoBehaviour
{
    [SerializeField] private string terrainAddress = "Terrain_Track1";
    [SerializeField] private Vector3 terrainPosition = Vector3.zero;
    [SerializeField] private Quaternion terrainRotation = Quaternion.identity;

    private AsyncOperationHandle<GameObject> handle;

    private void Start()
    {
        StartCoroutine(LoadTerrainAsync());
    }

    private IEnumerator LoadTerrainAsync()
    {
        handle = Addressables.InstantiateAsync(terrainAddress, terrainPosition, terrainRotation);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load terrain Addressable '{terrainAddress}'.");
        }
    }

    private void OnDestroy()
    {
        if (handle.IsValid())
        {
            Addressables.ReleaseInstance(handle);
        }
    }
}