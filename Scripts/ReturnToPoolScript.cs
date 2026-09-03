using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ReturnToPoolScript : MonoBehaviour
{
    [SerializeField] private ObjectType objectType;
    private void OnEnable()
    {
        StartCoroutine(ReturnGameobjectToPool());
    }

    private IEnumerator ReturnGameobjectToPool()
    {
        yield return new WaitForSeconds(10f);
        MainObjectPooler.instance.ReturnObjectToPoolRpc(gameObject, objectType);

    }
    
}
