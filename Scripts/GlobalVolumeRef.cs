using UnityEngine;

public class GlobalVolumeRef : MonoBehaviour
{
    public static GlobalVolumeRef instance;
    private void Awake()
    {
        instance = this;
    }
}
