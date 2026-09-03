using UnityEngine;

public class CarPrefabRegistry : MonoBehaviour
{
    public static CarPrefabRegistry instance;

    [SerializeField] private GameObject[] carPrefabs; // order matches CarType enum

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GameObject GetPrefabForIndex(int index)
    {
        if (index < 0 || index >= carPrefabs.Length)
            return carPrefabs[0];
        return carPrefabs[index];
    }
}