using System.Drawing;
using UnityEngine;
using UnityEngine.VFX;

public class SmoothTrailEmitter : MonoBehaviour
{
   
    public VisualEffect vfx;

  

    void LateUpdate()
    {
      
        vfx.SetVector3("CurrentPosition", transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")|| other.gameObject.CompareTag("AI"))
        {
            Debug.Log("<color=red>name = </color>" + other.gameObject.name);
            //Debug.Log("<color=red>hit</color>");
        }
    }
}
