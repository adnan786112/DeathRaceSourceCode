using UnityEngine;

public class TerrainRef : MonoBehaviour
{
    public static TerrainRef instance;
    private void Awake()
    {
        instance = this;
    }

}
