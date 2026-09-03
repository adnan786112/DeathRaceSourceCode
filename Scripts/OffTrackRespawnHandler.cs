using System.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class OffTrackRespawnHandler : NetworkBehaviour
{
    [SerializeField] private ProgressTracker progressTracker;
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private float respawnHeightOffset = 1.5f;
    [SerializeField] private float postRespawnGracePeriod = 0.3f;
    [SerializeField] private float RelocateTiming = 3;
    private Terrain terrain;
    private bool isRespawning = false;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        terrain = TerrainRef.instance.GetComponent<Terrain>();
    }

    private void OnTriggerEnter(Collider other)
    {
       
        if (!IsOwner || isRespawning || gameObject.GetComponent<CarController>().IsAICarEffective || !IsSpawned || !SaveScript.RaceStart) return;

        if (other.CompareTag("OutOfBounds"))
        {
            terrain.detailObjectDistance = 60;
            Debug.Log("OutOfBounds");

            StartCoroutine(RelocateCar());

            isRespawning = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("OutOfBounds"))
        {
            terrain.detailObjectDistance = 60;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("OutOfBounds"))
        {

            terrain.detailObjectDistance = 30;
        }
    }
 

    private void ApplyTeleport(Vector3 pos, Quaternion rot)
    {
        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        carRigidbody.position = pos;
        carRigidbody.rotation = rot;   
        var nt = GetComponent<ClientNetworkTransform>();
        nt.Teleport(pos, rot, transform.localScale);
        
        
    }

  



    private IEnumerator RelocateCar()
    {
        yield return new WaitForSeconds(RelocateTiming);

        var aiControl = GetComponent<CarAIControl>();
        aiControl?.SetDrivingEnabled(false);

        int wpIndex = progressTracker.CurrentWP.Value;
        Transform wpTransform = progressTracker.GetNextWaypoint();
      
        if (wpTransform == null) yield break;

        Vector3 respawnPos = wpTransform.position + Vector3.up * respawnHeightOffset;
        Quaternion respawnRot = wpTransform.rotation;
        ApplyTeleport(respawnPos, respawnRot);

       
        if (IsOwner)
            Invoke(nameof(ClearRespawnFlag), postRespawnGracePeriod);
    }
    private void ClearRespawnFlag() => isRespawning = false;
}