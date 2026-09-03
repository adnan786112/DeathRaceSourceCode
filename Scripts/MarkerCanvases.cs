using UnityEngine;

public class MarkerCanvases : MonoBehaviour
{
  public static MarkerCanvases instance;

    private void Awake()
    {
        
        instance = this;
    }

}
