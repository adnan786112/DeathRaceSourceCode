using UnityEngine;

public class MinimapCameraRef : MonoBehaviour
{
    public static Camera Instance { get; private set; }

    private void Awake()
    {
        Instance = GetComponent<Camera>();
    }
}