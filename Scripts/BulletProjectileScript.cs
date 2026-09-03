using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class BulletProjectileScript : NetworkBehaviour
{
    #region Variables
    public NetworkObjectReference ShooterRef { get; set; }
    [HideInInspector]public ParticleSystem bulletParticleSystem;
    private float BulletDamage;
    [SerializeField] private GameObject RocketExplosion;
    [SerializeField] private GameObject BulletHoleDecalPrefab;
    [SerializeField] private AudioSource BulletHitAudio;
    [SerializeField] private AudioClip[] MachineGunBulletHitClips; // assign all 7 in Inspector
    //[SerializeField] private AudioClip MachineGunBulletHitClip;
    [SerializeField] private AudioClip RocketHitClip;
    [SerializeField] private float BulletDespawnTime = 0.1f;
    [SerializeField] private float RayCastDistance = 0.5f;
    [SerializeField] private float RocketExplosionForce =12f;
    [SerializeField] private float RocketExplosionForceUpwards = 0.2f;
    [SerializeField] private ObjectType objectType;
    public static float BulletAudioTimer;
    private Rigidbody rb;
    private bool hasHit = false;

    private GameObject localShooter;
    private struct HitInfo : INetworkSerializable
    {
        public Vector3 HitPos;
        public Quaternion HitRotation;
        public Vector3 HitNormal;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HitPos);
            serializer.SerializeValue(ref HitRotation);
            serializer.SerializeValue(ref HitNormal);
        }

    }

    private HitInfo hitInfo;


    #endregion

    #region Awake

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    #endregion

    #region OnNetworkSpawn
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();       
    }
    #endregion

    #region IntializeBullets
    public void SetBullet(GameObject LocalShooter, GameObject bulletRef)
    {
        hasHit = false;
        localShooter = LocalShooter;
        bulletParticleSystem = bulletRef.GetComponent<ParticleSystem>();
    }

    [Rpc(SendTo.Everyone)]
    public void SetBulletRpc(NetworkObjectReference networkObjectReference , float bulletDamage)
    {
        hasHit = false;
        
        networkObjectReference.TryGet(out NetworkObject bulletRef);
        bulletParticleSystem = bulletRef.gameObject.GetComponent<ParticleSystem>();
        BulletDamage = bulletDamage;
        
      
    }
    [Rpc(SendTo.Everyone)]
    public void SetTargetForHomingRocketsRpc(NetworkObjectReference networkObjectReference)
    {
        
        StartCoroutine(FollowTargetHomingRockets(networkObjectReference));
    }
    public void SetTargetForHomingRockets(GameObject Target)
    {
       
        StartCoroutine(FollowTargetHomingRockets(Target));
    }
    private IEnumerator FollowTargetHomingRockets(NetworkObjectReference networkObjectReference)
    {
      
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
           
            ConstantForce constantForce = gameObject.GetComponent<ConstantForce>();
            if (constantForce != null) constantForce.enabled = false;

            float homingStrength = 5f;
            float rocketSpeed = 70f;
            yield return new WaitForSeconds(0.3f);
            while (gameObject != null && networkObject != null)
            {
                
                Vector3 direction = (networkObject.transform.position - transform.position).normalized;


                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    homingStrength * Time.deltaTime
                );
                float Distance = Vector3.Distance(transform.position, networkObject.transform.position);

                if (Distance < 10f && networkObject.GetComponentInParent<CarUserControl>().CanDefend)
                {

                    networkObject = null;
                    yield break;
                }
                else if (Distance < 5f && !networkObject.GetComponentInParent<CarUserControl>().CanDefend)
                {
                   
                    if (IsOwner)
                    {
                        RocketAreaDamageAffectsRpc(transform.position);
                    }
                    yield break;
                }
                
                    rb.linearVelocity = transform.forward * rocketSpeed;
                yield return null;
            }
        }
    }
    private IEnumerator FollowTargetHomingRockets(GameObject Target)
    {
        
        ConstantForce constantForce = gameObject.GetComponent<ConstantForce>();
        if (constantForce != null) constantForce.enabled = false;

        float homingStrength = 5f;
        float rocketSpeed = 50f;

        if (gameObject != null && Target != null)
        {
            yield return new WaitForSeconds(0.3f);
            while (gameObject != null && Target != null)
            {
                             
                Vector3 direction = (Target.transform.position - transform.position).normalized;
               

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    homingStrength * Time.deltaTime
                );


                rb.linearVelocity = transform.forward * rocketSpeed;
                float Distance = Vector3.Distance(transform.position, Target.transform.position);
                if (Distance < 10f && Target.GetComponentInParent<CarUserControl>().CanDefend)
                {
             
                    Target = null;
                    yield break;
                }
                else if (Distance < 5f && !Target.GetComponentInParent<CarUserControl>().CanDefend)
                {
                    Destroy(gameObject);
             
                    yield break;
                }
              
                yield return null;

            }
        }
    }

    #endregion

    #region ShooterRef
    public GameObject GetShooter()
    {
        if (ShooterRef.TryGet(out NetworkObject networkObject))
        {           
                return networkObject.gameObject;
        }
        return null;
    }

    #endregion

    #region BulletTriggerMechanics
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;   if (!IsOwner) return;  
        Collider collider = other.GetComponent<Collider>();      
        MeshRenderer meshRenderer = other.gameObject.GetComponent<MeshRenderer>();
        MeshRenderer meshRendererChildren = other.gameObject.GetComponentInChildren<MeshRenderer>();

        if (gameObject.CompareTag("VisualBullet") || gameObject.CompareTag("VisualRocket"))
        {
            MainObjectPooler.instance.ReturnObjectToPoolRpc(gameObject, objectType);
            return;
            
        }
        else
        {
            //Debug.Log("<color=red> GameObjectName </color> = " + other.gameObject.name);
            if (collider != null && !collider.isTrigger)
            {
              
                if (GetShooter() != null)
                {
                 
                    if (GetShooter() != other.gameObject && other.transform.root != GetShooter().transform.root && (meshRenderer != null || meshRendererChildren != null))
                    {

                        bool foundHit = false;

                        // Try forward raycast first — bullet approaching the surface
                        Vector3 rayStart = transform.position - transform.forward * 1.2f; // start further back
                        if (Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, 5f)) // longer distance
                        {
                            // Don't check hit.collider == other — on concave meshes the sub-collider
                            // reported may differ. Instead check if it's on the same root object.
                            if (hit.collider.transform.root == other.transform.root)
                            {


                                hitInfo.HitPos = hit.point;
                                hitInfo.HitNormal = hit.normal;
                                foundHit = true;
                            }
                        }

                        // If forward raycast failed, try a reverse raycast FROM the other collider's
                        // closest surface point TOWARD the bullet — handles cases where bullet is
                        // already inside the mesh when trigger fires
                        if (!foundHit)
                        {
                            Vector3 closestPoint = other.ClosestPoint(transform.position);
                            Vector3 reverseDir = (transform.position - closestPoint).normalized;

                            if (reverseDir == Vector3.zero) reverseDir = -transform.forward; // fallback

                            if (Physics.Raycast(closestPoint + reverseDir * 0.1f, -reverseDir, out RaycastHit reverseHit, 3f))
                            {
                                if (reverseHit.collider.transform.root == other.transform.root)
                                {
                                    hitInfo.HitPos = reverseHit.point;
                                    hitInfo.HitNormal = reverseHit.normal;
                                    foundHit = true;
                                }
                            }
                        }

                        // Last resort fallback — use closest surface point directly
                        if (!foundHit)
                        {
                            hitInfo.HitPos = other.ClosestPoint(transform.position);
                            hitInfo.HitNormal = (transform.position - other.transform.position).normalized;
                        }
                        hitInfo.HitPos += hitInfo.HitNormal * 0.001f;
                        hitInfo.HitRotation = Quaternion.LookRotation(-hitInfo.HitNormal);
                        // Stop bullet
                        rb.isKinematic = false;
                        rb.linearVelocity = Vector3.zero;

                        Vector3 bulletDirection = transform.forward;
                        //float impactAngle = Vector3.Angle(-bulletDirection, surfaceNormal);
                        if (!isRocket)
                        {

                            if (other.gameObject.GetComponentInParent<PlayerData>() != null)
                            {
                                PlayerData Car = other.gameObject.GetComponentInParent<PlayerData>();
                                var Shooter = GetShooter();

                                if (Shooter != null && Car != null && Shooter.GetComponent<NetworkObject>().NetworkObjectId != Car.NetworkObjectId)
                                {


                                    if (IsOwner)
                                    {
                                        hasHit = true;
                                        this.BulletHitCarServerRpc(hitInfo, Car.GetComponent<NetworkObject>());
                                        UpdateCarServerRpc(Car.gameObject.GetComponent<NetworkObject>(), ShooterRef, false);

                                    }
                                }
                            }
                            else
                            {

                                if (IsOwner)
                                {
                                    if (other.gameObject.GetComponent<MeshRenderer>().enabled)
                                    {
                                        hasHit = true;
                                        BulletHitNoCarServerRpc(hitInfo);
                                    }
                                }
                            }
                        }
                        if (isRocket)
                        {
                            hasHit = true;
                           
                            RocketAreaDamageAffectsRpc(transform.position);

                        }



                    }
                }

            }
        }
    }

    #endregion

    #region BulletDespawnMechanics
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DespawnBulletServerRpc()
    {
      
        StartCoroutine(DespawnBullet());
     
    }
    private IEnumerator DespawnBullet()
    {
        yield return new WaitForSeconds(0.5f);

        if (IsSpawned)
        {
            gameObject.GetComponent<NetworkObject>().Despawn();

        }
    }
    #endregion

    private AudioClip GetRandomBulletHitClip()
    {
        if (MachineGunBulletHitClips == null || MachineGunBulletHitClips.Length == 0) return null;
        return MachineGunBulletHitClips[Random.Range(0, MachineGunBulletHitClips.Length)];
    }

    #region BulletHitNoCar
    [ServerRpc]
    private void BulletHitNoCarServerRpc(HitInfo info)//for rockets  on everygameobject and bullet holes on non networked objects 
    {

     
        GameObject bulletHole = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.Decal);
        GameObject bulletHitEffect = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.BulletHitEffect);
       
        if (bulletHitEffect != null)
        {
            bulletHitEffect.SetActive(true);
            bulletHitEffect.transform.SetPositionAndRotation(info.HitPos, info.HitRotation);
            bulletHitEffect.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            //DespawnBulletEffectServerRpc(bulletHitEffect.GetComponent<NetworkObject>());
        }
        if (bulletHole != null)
        {
            bulletHole.SetActive(true);
            bulletHole.transform.SetPositionAndRotation(info.HitPos, info.HitRotation);          
            bulletHole.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            StartCoroutine(SoundFX.instance.PlayClip(GetRandomBulletHitClip(), false));
        }
     
        if (IsOwner)
        {          
            DespawnBulletServerRpc();
        }
        
    }

    #endregion

    #region BulletHitCarServerRpc
    [ServerRpc]
    private void BulletHitCarServerRpc(HitInfo info, NetworkObjectReference CarRef)//for bullet holes on networked gameobjects pand parenting
    {
        if (CarRef.TryGet(out NetworkObject Car))
        {
            GameObject bulletHole = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.Decal);
            GameObject bulletHitEffect = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.BulletHitEffect);
            if (bulletHitEffect != null)
            {
                bulletHitEffect.SetActive(true);
                bulletHitEffect.transform.SetPositionAndRotation(info.HitPos, info.HitRotation);
                bulletHitEffect.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);                                             
                //DespawnBulletEffectServerRpc(bulletHitEffect.GetComponent<NetworkObject>());
            }
            if (bulletHole != null)
            {
                bulletHole.SetActive(true);
                bulletHole.transform.SetPositionAndRotation(info.HitPos, info.HitRotation);
                bulletHole.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                bulletHole.transform.SetParent(Car.transform, true);
                var Shooter = GetShooter();
                if (Shooter != null && Car != null && Shooter.GetComponent<NetworkObject>().NetworkObjectId != Car.NetworkObjectId)
                {
                    if (IsOwner)
                    {
                      
                        DespawnBulletServerRpc();
                    }
                }
                StartCoroutine(SoundFX.instance.PlayClip(GetRandomBulletHitClip(), false));
            }
           
           
        }
        else
        {
            return;
        }
    }
  


    #endregion

    #region UpdateCarStats

    [ServerRpc]
    private void UpdateCarServerRpc(NetworkObjectReference DeadCarRef, NetworkObjectReference ShooterRef,bool isRocket)
    {
       
        //if (DeadCarRef.TryGet(out NetworkObject DeadCar) && ShooterRef.TryGet(out NetworkObject Shooter))
        //{
        //    PlayerData Car = DeadCar.GetComponent<PlayerData>();

            
        //    if (Car.CarHealthNetwork.Value >= 0)
        //    {
        //        if (IsOwner)
        //        {
        //            GunAffectsServerRpc(DeadCarRef,ShooterRef,isRocket);
        //        }
        //    }
           
        //}

      
        if (DeadCarRef.TryGet(out NetworkObject networkObject))
        {
            PlayerData Car = networkObject.GetComponent<PlayerData>();
          
            if (Car.CarHealthNetwork.Value >= 0)
            {
                Car.CarHealthNetwork.Value -= BulletDamage;
            }
            if (Car.CarHealthNetwork.Value <= 0 && !Car.IsCarOnZeroHealth.Value)
            {
                //Debug.Log("Invoked");
                Car.CarDeathEvent?.Invoke(DeadCarRef, ShooterRef);

            }
        }
    }
 
    #endregion

    #region RocketAreaDamage

    [Rpc(SendTo.Everyone)]
    private void RocketAreaDamageAffectsRpc(Vector3 TriggerLoc)
    {
      
        if (IsServer)
        {
            GameObject RocketExplosionVisual = Instantiate(RocketExplosion, TriggerLoc, Quaternion.identity);
            RocketExplosionVisual.GetComponent<NetworkObject>().SpawnWithOwnership(ShooterRef.NetworkObjectId);
        }
        StartCoroutine(SoundFX.instance.PlayClip(RocketHitClip, true));

        Collider[] colliders = Physics.OverlapSphere(TriggerLoc, 5f);

        HashSet<PlayerData> damagedCars = new();
       
        bool despawnedSelf = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null) continue;

            PlayerData Car = collider.gameObject.GetComponentInParent<PlayerData>();
          
            if (Car != null && damagedCars.Add(Car))
            {
               
                if (IsOwner)
                    UpdateCarServerRpc(Car.gameObject.GetComponent<NetworkObject>(), ShooterRef, true);
                Rigidbody rb = collider.gameObject.GetComponentInParent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(100, TriggerLoc, 5, 1f, ForceMode.Impulse);
                }
                return;
            }

        }
      
        if (IsOwner && !despawnedSelf)
        {
            DespawnBulletServerRpc();
            despawnedSelf = true;
        }
       
    }
    //[ClientRpc]
    //private void RocketAreaDamageAffectsClientRpc(Vector3 TriggerLoc)
    //{

    //}


    #endregion

    #region Getters
    public bool isRocket { get; set; }
    public bool isHomingRocket { get; set; }
    #endregion

}
