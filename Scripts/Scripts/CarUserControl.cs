using Cinemachine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;

namespace Unity.Netcode
{
    [RequireComponent(typeof(CarController))]
    public class CarUserControl : NetworkBehaviour
    {
        #region Variables
        [SerializeField] private ProgressTracker progressTracker;

        [Header("AmmoPropety")]
        [SerializeField] private CarAmmoScriptableObject CarAmmoData;        
        
        [Header("RocketLauncher")]
       
        [SerializeField] private GameObject RocketProjectile;
        [SerializeField] private Transform[] RocketLauncherFirePoses;
        [SerializeField] private AudioClip RocketLauncherSound;
        [SerializeField] private float LaucnhAngle = -10f;
        private float currentRocketAmmo;

        [Header("MachinGuns")]
        [SerializeField] private Transform[] MiniGunFirePoses;
        //[SerializeField] private Animator[] MiniGunAnimators;
        [SerializeField] private AudioClip MiniGunFireStartSound;
        [SerializeField] private AudioClip MiniGunFireStopSound;
        [SerializeField] private GameObject BulletProjectile;
        [SerializeField] private float FireRate;
        [SerializeField] private ParticleSystem[] MiniGunSmokeEffects; // one per fire pose, same order/length as MiniGunFirePoses
        [SerializeField] private int SmokeEmitCountPerShot = 2;
        [SerializeField] private ParticleSystem[] MiniGunMuzzleFlash; // one per fire pose, same order/length as MiniGunFirePoses
        private float currentMinigunAmmo;

        [Header("Effects")]
       
        [SerializeField] private GameObject CarExplosionCube;
        [SerializeField] private GameObject CarExplosionEffect;
        [SerializeField] private ParticleSystem CarFireEffect;
        [SerializeField] private ParticleSystem CarBlackSmokeEffect;

        [Header("CommonGuns")]
        [SerializeField] private AudioSource GunAudioSource;
        [SerializeField] private float Speed = 100f;
        [SerializeField] private float MaxLatencyValue;
        [SerializeField] private float GunForwardAimVector;
        [SerializeField] private CinemachineImpulseSource GunImpulseSource;


        [Header("Crosshair")]
        [SerializeField] private Canvas crosshairCanvas;
        [SerializeField] private RectTransform crosshairGameobject;
        [SerializeField] private Image[] Crosshairs;
        [SerializeField] private Image CrosshairCentreDot;
        [SerializeField] private Animator CrosshairAnimator;
        [SerializeField] private float CrossHairYPos;
        [SerializeField] private float crosshairMaxDistance = 300f;
        [SerializeField] private float crosshairDefaultDistance = 100f;
        [SerializeField] private float crosshairLookSensitivity = 3f;
        [SerializeField] private float minCrosshairYawAngle = -2f;
        [SerializeField] private float maxCrosshairYawAngle = 3f;   // how far left/right from the car's forward
        [SerializeField] private float minCrosshairPitchAngle = -5f; // looking down
        [SerializeField] private float maxCrosshairPitchAngle = 5f;  // looking up
        [SerializeField] private float crosshairCameraLerpValue = 2f;

        [Header("RocketDeflect")]
        [SerializeField] private float DeflectingForce;
        [SerializeField] private float DeflectingTime;

        [Header("General")]       
        [SerializeField] private CinemachineVirtualCamera LocalCamera;
        [SerializeField] private LayerMask VehicleLayerMask;
        [SerializeField] private VehicleAntiStuck VehicleAntiStuckScriptRef;

        [Header("Tire Smoke")]
        [SerializeField] private WheelEffects[] m_WheelEffects; // assign all 4 wheels in inspector
        [SerializeField] private float handbrakeSmokeSmokeThreshold = 0.1f; // minimum speed before handbrake smoke starts
        [SerializeField] private float neutralRevSmokeThrottle = 0.3f;      // throttle threshold before neutral smoke kicks in

        [Header("Mouse Look Around")]
         private bool enableMouseLookAround = false;
        [SerializeField] private float lookSensitivity = 3f;
        [SerializeField] private float maxYawAngle = 110f;   // how far left/right from the car's forward
        [SerializeField] private float minPitchAngle = -15f; // looking down
        [SerializeField] private float maxPitchAngle = 35f;  // looking up
        [SerializeField] private float cameraLerpValue = 1f;
        [SerializeField] private Transform cameraLookPivot;
        [SerializeField] private float minPosZ = -1f;
        [SerializeField] private float maxPosZ = 5F;
 

        private float currentYaw;
        private float currentPitch;
        private float currentPosZ;

        private float currentCrosshairYaw;
        private float currentCrosshairPitch;

        private CarController carController;
        private AssignPosScript AssignPos;
        private PlayerData playerData;
        private Collider[] CarColliders;
        private Collider[] RocketsToDeflect = new Collider[20];
        private Ray ray;
        private delegate void CrosshairStatus(bool status);
        private CrosshairStatus crosshairStatus;
        private float GunTimer;
        private bool isFiringSound = false;
        public Rigidbody MyBody;
        public GameObject PlayerCam;
        public bool IsAICar = false;
        public bool IsCarUserControlScriptEnabled = false;
        public static CarUserControl instance;
        public static bool FreezeCarAtStartingLight = false;
        public static bool AssignCarNamePerFrame;
        public static Dictionary<string, NetworkVariable<string>> PlayercarNameDictionary = new Dictionary<string, NetworkVariable<string>>();
        private readonly RaycastHit[] crosshairRayCastHit = new RaycastHit[10];
        private Vector3 HalfExtents = new(1f, 0.1f, 0.5f);
    
        #endregion

        #region Awake
        private void Awake()
        {
            carController = gameObject.GetComponent<CarController>();
            AssignPos = AssignPosScript.instance;
            instance = this;

            CarColliders = GetComponentsInChildren<Collider>().ToArray();

            playerData = GetComponent<PlayerData>();
            MyBody.linearDamping = 0.1f;
            MyBody.angularDamping = 0.1f;
            MyBody.maxDepenetrationVelocity = 2f;
            //crosshairStatus += UpdateCrosshairStatus;

        }
        #endregion

        #region OnNetworkSpawn
        public override void OnNetworkSpawn()
        {
            if (!IsAICar)
            {
                gameObject.transform.position = PlayerSpawner.instance.SpawnPlayerAtRandomPoint();


                if (playerData != null)
                {
                    playerData.CarName.OnValueChanged += (oldVal, newVal) =>
                    {
                        gameObject.name = newVal.ToString();
                    };

                    if (!string.IsNullOrEmpty(playerData.CarName.Value.ToString().Trim()))
                    {
                        gameObject.name = playerData.CarName.Value.ToString();
                    }
                }
            }
            if (IsOwner && !IsAICar)
            {
                cameraLookPivot.transform.position = gameObject.transform.position;
                cameraLookPivot.transform.rotation = gameObject.transform.rotation;
                LocalCamera.m_Follow = cameraLookPivot;
                LocalCamera.m_LookAt = cameraLookPivot;
                currentMinigunAmmo = CarAmmoData.GetMiniBulletMaxAmmo;
                currentRocketAmmo = CarAmmoData.GetRocketMaxAmmo;
                UIScript.instance.InitAmmo(CarAmmoData.GetMiniBulletMaxAmmo, CarAmmoData.GetRocketMaxAmmo);
                UIScript.instance.InitAmmo(CarAmmoData.GetMiniBulletMaxAmmo, CarAmmoData.GetRocketMaxAmmo);
            }
            if (IsSpawned)
            {

                SaveScript.RaceStartEvent += OnRaceStart;
                AssignPosScript.ProgressWaypointsSpawned += AssignProgreesWaypointTransforms;
            }


            CarFireEffect.gameObject.SetActive(false);
            CarBlackSmokeEffect.gameObject.SetActive(false);
            StartCoroutine(SpawnPlayerActualPosCoroutine());
            StartCoroutine(AssignCarTagCoroutine());
            if (!IsAICar && IsLocalPlayer && IsOwner)
            {
                PressurePlateMasterScript.DispalyCrosshairCamvas += DisplayCrossHair;

            }
        }
        #endregion

        #region Start
        private void Start()
        {
            MyBody = GetComponent<Rigidbody>();
            if (crosshairCanvas != null)
            {
                crosshairCanvas.enabled = false;
            }
        }
        #endregion

        #region OnRaceStartAction
        public void OnRaceStart()
        {
            MyBody.interpolation = RigidbodyInterpolation.Interpolate;
            MyBody.constraints = RigidbodyConstraints.None;
            
        }
        #endregion

        #region Update
        private void Update()
        {

            if (LobbyManager.Instance.IsLobbyHost())
            {
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.IsSpawned)
                    {
                        if (i == NetworkManager.Singleton.ConnectedClientsList.Count - 1 && LobbyManager.RaceCountDownOnce == false)
                        {
                            StartCoroutine(RaceCountDown());
                            LobbyManager.RaceCountDownOnce = true;
                            break;
                        }
                    }
                }
            }

            if (LobbyManager.SpawnedALLCars)
            {
                if (!IsOwner)
                {
                    IsAICar = true;
                }
                //AssignProgreesWaypointTransforms();
               
            }
           

            if (!playerData.IsCarDead.Value)
            {
                if (!carController.IsAICarEffective)
                {
                    if (IsOwner)
                    {
                        if (CanShoot)
                        {
                            UpdateCrosshairLookAround();
                            UpdateCrosshairColorOnFindingPotentialTarget();

                        }
                       

                        if (Input.GetKeyDown(KeyCode.LeftAlt) && CanShoot)
                        {


                            if (currentRocketAmmo > 0)
                            {
                                GameObject Target = UpdateCrosshairColorOnFindingPotentialTarget();
                                currentRocketAmmo = Mathf.Max(0, currentRocketAmmo - 1f);
                                SpawnHomingRockets(Target);
                            }
                            if (CarAmmoData.GetRocketMaxAmmo > 0)
                            {
                                UIScript.instance.UpdateRocketAmmoUI(currentRocketAmmo);
                            }
                          

                        }

                        if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.RightAlt)) && CanShoot)
                        {
                            if (IsOwner)
                            {
                                if (currentRocketAmmo > 0)
                                {
                                    currentRocketAmmo = Mathf.Max(0, currentRocketAmmo - 1f);
                                    FireRocketsFullFucntion();
                                }
                                if (CarAmmoData.GetRocketMaxAmmo > 0)
                                {
                                    UIScript.instance.UpdateRocketAmmoUI(currentRocketAmmo);
                                }
                            }
                        }


                        else
                        {
                            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.LeftControl)) && CanShoot)
                            {

                                if (currentMinigunAmmo > 0)
                                {
                                    if (!isFiringSound)
                                    {
                                        StartMiniGunFireSoundRpc();
                                        isFiringSound = true;
                                    }
                                }
                            }
                            else if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.LeftControl)) && CanShoot)
                            {

                                GunAudioSource.Stop();
                                CrossHairState(false);
                                isFiringSound = false;
                                StopMinigunsRpc();


                            }

                            if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.LeftControl))  && CanShoot)
                            {
                                if (currentMinigunAmmo <= 0)
                                {
                                    if (isFiringSound)
                                    {
                                        GunAudioSource.Stop();
                                        CrossHairState(false);
                                        isFiringSound = false;
                                        StopMinigunsRpc();
                                    }
                                }
                                else
                                {
                                    GunTimer += Time.deltaTime;
                                    if (GunTimer >= FireRate)
                                    {
                                        GunTimer = 0f;
                                        for (int i = 0; i < MiniGunFirePoses.Length; i++)
                                        {
                                            Transform o = MiniGunFirePoses[i];
                                            Vector3 spawnPos = o.transform.position;
                                            Vector3 direction = CalculateDirection(spawnPos);
                                            Quaternion spawnRot = Quaternion.LookRotation(direction);
                                            Vector3 finalVelocity = direction * Speed + MyBody.linearVelocity;
                                            SpawnLocalBullet(BulletProjectile, spawnPos, spawnRot, finalVelocity, true, 0.8f);
                                            ShootBulletServerRpc(spawnPos, spawnRot, finalVelocity, OwnerClientId, true);
                                            CrossHairState(true);
                                      
                                            GunImpulseSource.GenerateImpulse();
                                            EmitMinigunSmokeRpc(i);
                                        }
                                        currentMinigunAmmo = Mathf.Max(0, currentMinigunAmmo - 1f);
                                    }
                                }
                                UIScript.instance.UpdateMinigunAmmoUI(currentMinigunAmmo);
                            }



                        }
                    }
                }
                //make this attachbale with sheild plate aftewrwards and a cooldown after some time to disable the bool(shield)
                //for time being this code is here in this if loop

                if (CanDefend)
                {
                    if (IsOwner)
                    {
                        RocketDeflectServerRpc();
                    }
                }

            }
            else if(playerData.IsCarDead.Value || !CanShoot)
            {
                GunAudioSource.Stop();
                GunAudioSource.loop = false;
            }

           
            
            if (Input.GetKeyDown(KeyCode.T))
            {
               
                enableMouseLookAround = !enableMouseLookAround;
            }
          

        }

        #endregion

        #region FixedUpdate
        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                LocalCamera.enabled = false;
                LocalCamera.gameObject.SetActive(false);
                return;
            }
            if (carController.IsAICarEffective)
            {
                LocalCamera.enabled = false;
                LocalCamera.gameObject.SetActive(false);
            }


            if (!FreezeCarAtStartingLight && (SaveScript.RaceStart == true || LobbyManager.SpawnedALLCars == false)
                && !carController.IsAICarEffective && !playerData.IsCarDead.Value && !SaveScript.RaceOver)

            {
                LocalCamera.gameObject.SetActive(true);


                //LocalCamera.m_Follow = transform;
                //LocalCamera.m_LookAt = transform;

                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");

                if (SaveScript.Joypad == true)
                {
                    if (CrossPlatformInputManager.GetButton("Fire1"))
                    {
                        v = 2.0f;
                    }
                    if (CrossPlatformInputManager.GetButton("Fire2"))
                    {
                        v = -0.5f;
                    }
                    if (!CrossPlatformInputManager.GetButton("Fire2") && !CrossPlatformInputManager.GetButton("Fire1"))
                    {
                        v = 0;
                    }
                }

                if (v < 0 && h != 0)
                {
                    SaveScript.BrakeSlide = true;
                }
                if (v >= 0)
                {
                    SaveScript.BrakeSlide = false;
                    SaveScript.IsReversing = false;
                }

                if (v < 0 && SaveScript.Speed > 0 && SaveScript.Speed < 1)
                {
                    SaveScript.IsReversing = true;
                }

                float handbrake = CrossPlatformInputManager.GetAxis("Jump");
                carController.Move(h, v, v, handbrake);

                float handbrakeVal = CrossPlatformInputManager.GetAxis("Jump");
                float currentSpeed = carController.GetComponent<Rigidbody>().linearVelocity.magnitude;

                if (handbrakeVal > 0.1f && currentSpeed > handbrakeSmokeSmokeThreshold && m_WheelEffects != null)
                {
                    // All four wheels smoke under hard handbrake, rear heavier
                    float smokeIntensity = Mathf.Clamp01(handbrakeVal);
                    m_WheelEffects[0].EmitCinematicSmokeRpc(smokeIntensity * 0.4f); // front: lighter
                    m_WheelEffects[1].EmitCinematicSmokeRpc(smokeIntensity * 0.4f);
                    m_WheelEffects[2].EmitCinematicSmokeRpc(smokeIntensity);         // rear: full burst
                    m_WheelEffects[3].EmitCinematicSmokeRpc(smokeIntensity);
                }



            }
            else if (FreezeCarAtStartingLight && !carController.IsAICarEffective && IsOwner && !playerData.IsCarDead.Value)
            {
                float v = CrossPlatformInputManager.GetAxis("Vertical");

                if (SaveScript.Joypad)
                {
                    if (CrossPlatformInputManager.GetButton("Fire1")) v = 2.0f;
                    else if (CrossPlatformInputManager.GetButton("Fire2")) v = -0.5f;
                    else v = 0f;
                }

                // Feed throttle to controller so engine/audio simulates revving
                carController.Move(0f, v, v, 1f);

                float throttleAbs = Mathf.Abs(CrossPlatformInputManager.GetAxis("Vertical"));
                if (SaveScript.Joypad && CrossPlatformInputManager.GetButton("Fire1"))
                    throttleAbs = 1f;

                if (throttleAbs > neutralRevSmokeThrottle && m_WheelEffects != null)
                {
                    // Rear wheels only (indices 2 and 3) — front wheels don't smoke on a stationary rev
                    float smokeIntensity = Mathf.InverseLerp(neutralRevSmokeThrottle, 1f, throttleAbs);
                    m_WheelEffects[2].EmitCinematicSmokeRpc(smokeIntensity * 0.7f);
                    m_WheelEffects[3].EmitCinematicSmokeRpc(smokeIntensity * 0.7f);
                }
                else if (m_WheelEffects != null)
                {
                    // Throttle released — end any active skid trails on rear wheels
                    m_WheelEffects[2].EndSkidTrail();
                    m_WheelEffects[3].EndSkidTrail();
                }
                // Hard-clamp position and velocity so car goes nowhere

                MyBody.linearVelocity = Vector3.zero;
                MyBody.angularVelocity = Vector3.zero;

            }

            if (playerData.IsCarDead.Value)
            {
                MyBody.linearDamping = 1f;
                MyBody.angularDamping = 1f;
            }
            //make anti stuck systems for ai car as well

            if (MyBody.linearVelocity.magnitude <= 1 && SaveScript.RaceStart)
            {
                VehicleAntiStuckScriptRef.DetectCollisions();

            }


        }
        #endregion

        #region LateUpdate
        private void LateUpdate()
        {

            if (!IsOwner || IsAICar || !SaveScript.RaceStart) return;
            
            if(enableMouseLookAround)
            {
               
                UpdateMouseLookAround();
                UpdateMouseZoom();

            }
            else
            {

                cameraLookPivot.localPosition = UpdateMouseZoom();
                cameraLookPivot.localRotation = UpdateMouseLookAround();
            }
        }
        #endregion

        #region MouseLookAroundCamera

        private Quaternion UpdateMouseLookAround()
        {
            if (!enableMouseLookAround || cameraLookPivot == null|| !SaveScript.RaceStart) return cameraLookPivot.localRotation;
                     
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
            {
                currentYaw += mouseX * lookSensitivity;
                currentPitch -= mouseY * lookSensitivity;

                currentYaw = Mathf.Clamp(currentYaw, -maxYawAngle, maxYawAngle);
                currentPitch = Mathf.Clamp(currentPitch, minPitchAngle, maxPitchAngle);

                Quaternion targetLocalRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                cameraLookPivot.localRotation = Quaternion.Slerp(cameraLookPivot.localRotation, targetLocalRotation, cameraLerpValue*Time.deltaTime);
                return targetLocalRotation;
            }
           
            return cameraLookPivot.localRotation;
        }
        private Vector3 UpdateMouseZoom()
        {
            if (!enableMouseLookAround || cameraLookPivot == null || !SaveScript.RaceStart) return cameraLookPivot.localPosition;
            float mouseZ = Input.GetAxis("Mouse ScrollWheel");
           
            currentPosZ += mouseZ * lookSensitivity;
            currentPosZ = Mathf.Clamp(currentPosZ, minPosZ, maxPosZ);

            Vector3 targetPosZ = new Vector3(cameraLookPivot.localPosition.x, cameraLookPivot.localPosition.y, currentPosZ);
            cameraLookPivot.localPosition = Vector3.Lerp(cameraLookPivot.localPosition, targetPosZ, cameraLerpValue * Time.deltaTime);

            
            return cameraLookPivot.localPosition;
        }

        #endregion

        #region AssgnProgressWaypointTransforms
       
        private void AssignProgreesWaypointTransforms()
        {
            progressTracker.waypoints = AssignPos.ProgressWaypointsRef.GetComponentsInChildren<Transform>()
             .Where(t => t != AssignPos.ProgressWaypointsRef.transform)
             .ToArray();
        }




        #endregion

        #region CrossHair
        private void DisplayCrossHair()
        {
            crosshairCanvas.enabled = true;
        }
        private void CrossHairState(bool status)
        {
            CrosshairAnimator.SetBool("Firing", status);
        }

        private GameObject UpdateCrosshairColorOnFindingPotentialTarget()
        {
            ray = Camera.main.ScreenPointToRay(CrosshairCentreDot.transform.position);
            Quaternion BoxRotation = Quaternion.LookRotation(ray.direction);
            int hits = Physics.BoxCastNonAlloc(ray.origin, HalfExtents, ray.direction, crosshairRayCastHit, BoxRotation, 300, VehicleLayerMask, QueryTriggerInteraction.Ignore);
            if (hits > 0)
            {
                for (int i = 0; i < hits; i++)
                {
                 
                    if (crosshairRayCastHit[i].collider.transform.root.GetComponent<NetworkObject>().NetworkObjectId != NetworkManager.LocalClient.PlayerObject.NetworkObjectId)
                    {                      
                        UpdateCrosshairColor(true);                    
                        return crosshairRayCastHit[i].collider.transform.root.GetComponent<NetworkObject>().gameObject;
                    }
                }

            }
            else
            {
                //CrosshairAnimator.SetBool("TargetFound", false);
                UpdateCrosshairColor(false);
                //CrosshairCentreDot.sprite = CrosshairGreenSprite;

            }
         

            return null;
        }
        private void UpdateCrosshairColor(bool foundtarget)
        {
            if(foundtarget)
            {
                foreach (Image o in Crosshairs)
                {
                    o.color = Color.darkRed;
                }
            }
            else
            {
                foreach (Image o in Crosshairs)
                {
                    o.color = Color.white;
                }
            }
        }
        private Vector3 CalculateDirection(Vector3 spawnPos)
        {
            // Get the center of the crosshair in screen space
            Vector3 crosshairScreenPos = new(CrosshairCentreDot.rectTransform.position.x,
                                                     CrosshairCentreDot.rectTransform.position.y / CrossHairYPos,
                                                     100f); // Distance in front of camera

            // Convert to world space
            Vector3 targetWorldPos = Camera.main.ScreenToWorldPoint(crosshairScreenPos);

            // Calculate direction from gun to target
            Vector3 direction = (targetWorldPos - spawnPos).normalized;
            return direction;

        }

        private void UpdateCrosshairLookAround()
        {
            float mouseY = Input.GetAxis("Mouse X");
            float mouseX = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
            {
                currentCrosshairYaw += mouseX * crosshairLookSensitivity;
                currentCrosshairPitch += mouseY * crosshairLookSensitivity;

                currentCrosshairYaw = Mathf.Clamp(currentCrosshairYaw, minCrosshairYawAngle, maxCrosshairYawAngle);
                currentCrosshairPitch = Mathf.Clamp(currentCrosshairPitch, minCrosshairPitchAngle, maxCrosshairPitchAngle);

                Vector3 TargetPos = new Vector3(currentCrosshairPitch, currentCrosshairYaw,0);
                crosshairGameobject.localPosition = Vector3.Lerp(crosshairGameobject.localPosition, TargetPos, crosshairCameraLerpValue*Time.deltaTime);
               
            }
           
        }

   
        #endregion

        #region CommonFunctionsOnBothGuns

        private void SpawnLocalBullet(GameObject bulletPrefab, Vector3 spawnPos, Quaternion spawnRot, Vector3 velocity, bool isMachineGunBullet, float time, bool isHomingRocket = false, GameObject TargetForHomingRockets = null)
        {
            GameObject bullet = null;
            if (isMachineGunBullet)
            {
               bullet = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.VisualMinigunBullet);
            }
            else
            {
                bullet = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.VisualRocket);
            }
            if (bullet != null)
            {

                bullet.SetActive(true);
                bullet.transform.SetPositionAndRotation(spawnPos, spawnRot);
                bullet.GetComponent<BulletProjectileScript>().SetBullet(this.gameObject, bullet);

                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = velocity;
                }

                BulletProjectileScript bulletScript = bullet.GetComponent<BulletProjectileScript>();
                if (bulletScript != null)
                {

                    if (!isMachineGunBullet && !isHomingRocket)
                    {
                        bulletScript.isRocket = true;
                    }
                    if (isHomingRocket)
                    {
                        bulletScript.isHomingRocket = true;
                        if (TargetForHomingRockets)
                        {
                            bulletScript.SetTargetForHomingRockets(TargetForHomingRockets);
                        }
                    }
                }

                bullet.transform.GetChild(0).gameObject.GetComponent<ParticleSystemDestroyer>().RecieveShooter(this.gameObject);
                bullet.transform.GetChild(0).gameObject.SetActive(true);


            }
        }
        
        private void InitializeBullet(GameObject bullet, Quaternion spawnRot, Vector3 velocity, ulong shooterId, bool isMachineGunBullet, float time, bool isHomingRocket = false, NetworkObjectReference TargetForHomingRocketsRef = default)
        {
          
            SetShooterRpc(bullet.GetComponent<NetworkObject>(),velocity,shooterId,isMachineGunBullet,time,isHomingRocket);
           

            if (isHomingRocket)
            {
                if (TargetForHomingRocketsRef.TryGet(out NetworkObject target))
                {
                    //Debug.Log("Target Aquired");
                    if (target != null)
                    {
                        SetTargetForHomingRocketsRpc(bullet.GetComponent<NetworkObject>(), TargetForHomingRocketsRef);
                    }
                }
            }
          
            if (bullet != null && !isHomingRocket)
            {
                if (bullet != null)
                {
                    DespawnBulletServerRpc(bullet.GetComponent<NetworkObject>());
                    //StartCoroutine(RetrunBulletToPoolCoroutine(bullet,ObjectType.MinigunBullet));
                                      
                }
               
            }
        }
        [Rpc(SendTo.Everyone)]
        private void SetShooterRpc(NetworkObjectReference bulletRef, Vector3 velocity, ulong shooterId, bool isMachineGunBullet, float time, bool isHomingRocket = false ,bool isRocket = false)
        {
            bulletRef.TryGet(out NetworkObject bullet);        
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            BulletProjectileScript bulletScript = bullet.gameObject.GetComponent<BulletProjectileScript>();
          
            bulletScript.ShooterRef = gameObject.GetComponent<NetworkObject>();
            

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = velocity;
            }

            //DisableParticlesOnNetworkedBulletRpc(shooterId, bullet.GetComponent<NetworkObject>(), isMachineGunBullet);
            
            //if (!isMachineGunBullet && !isHomingRocket)
            //{
            //    bulletScript.isRocket = true;
            //}          

        }

        [Rpc(SendTo.Everyone)]
        private void SetTargetForHomingRocketsRpc(NetworkObjectReference bulletRef , NetworkObjectReference TargetForHomingRocketsRef )
        {
            
            bulletRef.TryGet(out NetworkObject bullet);         
            BulletProjectileScript bulletScript = bullet.GetComponent<BulletProjectileScript>();
            bulletScript.isHomingRocket = true;
            TargetForHomingRocketsRef.TryGet(out NetworkObject TargetForHomingRockets);
            bulletScript.SetTargetForHomingRocketsRpc(TargetForHomingRockets);
               
            
        }
        #endregion

        #region BulletDespawnMechanics
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void DespawnBulletServerRpc(NetworkObjectReference networkObjectReference)
        {
            networkObjectReference.TryGet(out NetworkObject networkObject);
           
            StartCoroutine(DespawnBullet(networkObject));
            
        }
        private IEnumerator DespawnBullet(NetworkObject networkObject)
        {
        
            yield return new WaitForSeconds(1.2f);
            if (networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
        }
      
        #endregion

        #region MachineGunShootingSystem
        //[ServerRpc]
        //private void StartMiniGunAnimationsServerRpc()
        //{
        //    foreach (Animator anim in MiniGunAnimators)
        //    {
        //        anim.Play("MiniGunFire");
        //    }

        //}
        [Rpc(SendTo.Everyone)]
        private void StartMiniGunFireSoundRpc()
        {
            if (!playerData.IsCarDead.Value)
            {
                GunAudioSource.clip = MiniGunFireStartSound;
                GunAudioSource.Play();
                GunAudioSource.loop = true;
            }

        }


        [Rpc(SendTo.Everyone)]
        private void StopMinigunsRpc()
        {
            GunAudioSource.Stop();
            GunAudioSource.clip = MiniGunFireStopSound;
            GunAudioSource.Play();
            GunAudioSource.loop = false;

            //foreach (Animator anim in MiniGunAnimators)
            //{
            //    anim.StopPlayback();
            //}
        }

        [Rpc(SendTo.Everyone)]
        private void EmitMinigunSmokeRpc(int fireIndex)
        {
            if (MiniGunSmokeEffects == null) return;
            if (fireIndex < 0 || fireIndex >= MiniGunSmokeEffects.Length) return;

            ParticleSystem smoke = MiniGunSmokeEffects[fireIndex];
            ParticleSystem muzzleFlash = MiniGunMuzzleFlash[fireIndex];
            if (smoke != null)
            {
                smoke.Emit(SmokeEmitCountPerShot);
            }
            if (muzzleFlash!= null)
            {
                muzzleFlash.Emit(SmokeEmitCountPerShot);
            }
        }


        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        private void ShootBulletServerRpc(Vector3 spawnPos, Quaternion spawnRot, Vector3 velocity, ulong shooterId, bool isMachineGunBullet)
        {          
        
            GameObject bullet = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.MinigunBullet);
            if (bullet != null)
            {
                bullet.SetActive(true);
                bullet.transform.SetPositionAndRotation(spawnPos, spawnRot);
              



                NetworkObject netObj = bullet.GetComponent<NetworkObject>();
            
                if (netObj != null)
                {
                    
                    netObj.SpawnWithOwnership(shooterId);
                    var nt = bullet.GetComponent<NetworkTransform>();
                    nt.Teleport(spawnPos, spawnRot, bullet.transform.localScale);
                    bullet.GetComponent<BulletProjectileScript>().SetBulletRpc(netObj,CarAmmoData.GetMiniBulletdDamage);
                    

                    //SetActiveBulletRpc(netObj, spawnPos, spawnRot);
                    DisableParticlesOnNetworkedBulletRpc(bullet.GetComponent<NetworkObject>());
                }
                // Initialize bullet on server
                InitializeBullet(bullet, spawnRot, velocity, shooterId, true, 0.8f);
            }
            
        }
        
    
        [Rpc(SendTo.Everyone)]
        public void DisableParticlesOnNetworkedBulletRpc(NetworkObjectReference networkObjectReference)
        {
            networkObjectReference.TryGet(out NetworkObject networkObject);
            ParticleSystem bulletParticleSystemNetwork = networkObject.gameObject.GetComponent<ParticleSystem>();
            
            
            if (IsOwner)
            {

              
                if (bulletParticleSystemNetwork != null)
                {
                    Destroy(bulletParticleSystemNetwork);

                    networkObject.gameObject.transform.GetChild(0).gameObject.SetActive(false);

                }
             
                //else
                //{
                //    MeshRenderer meshRenderer = Particle.GetComponent<MeshRenderer>();
                //    meshRenderer.enabled = false;
                //    Particle.transform.GetChild(0).gameObject.SetActive(false);

                //}
            }
            else
            {
                networkObject.transform.GetChild(0).gameObject.GetComponent<ParticleSystemDestroyer>().RecieveShooter(this.gameObject);
                networkObject.transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        #endregion

        #region RocketDeflectSystem
        [ServerRpc]
        //this system doesnt deflect rockets which are fired at very close from the target car and does deflects its own rockets
        public void RocketDeflectServerRpc()
        {
            
            int overlaps = Physics.OverlapSphereNonAlloc(transform.position, 15, RocketsToDeflect,LayerMask.GetMask("MinigunBulletLayer"),QueryTriggerInteraction.Collide);
            if (overlaps > 0)
            {
                
                for (int i = 0;i < overlaps; i++)
                {
                  
                    if (RocketsToDeflect[i].gameObject.CompareTag("Rockets"))
                    {
                       
                        Rigidbody rb = RocketsToDeflect[i].gameObject.GetComponent<Rigidbody>();
                        Destroy(RocketsToDeflect[i].gameObject.GetComponent<ConstantForce>());
                        if(rb != null)
                        {
                           
                            StartCoroutine(RocketsDeflectingCoroutine(rb));
                            break;
                        }
                    }
                }
            }
        }
     

        private IEnumerator RocketsDeflectingCoroutine(Rigidbody RocketToDeflect)
        {
            float deflectDuration = DeflectingTime;   
            float elapsed = 0;

            while (elapsed < deflectDuration)
            {
                
                RocketToDeflect.AddForce(Vector3.up * DeflectingForce*Time.deltaTime, ForceMode.Force);
                elapsed += Time.deltaTime;
                yield return null;
            }

            
        }

        #endregion

        #region RocketLaucncherShootingSystem

        // In FireRocketsFullFucntion, capture target on client BEFORE calling ServerRpc
        private void FireRocketsFullFucntion(bool isHomingRocket = false, GameObject TargetForHomingRockets = null)
        {
            foreach (Transform o in RocketLauncherFirePoses)
            {
                Vector3 spawnPos = o.transform.position;
                Vector3 direction = CalculateDirection(spawnPos);
                Quaternion spawnRot;

                Vector3 launchDirection = Quaternion.AngleAxis(LaucnhAngle, transform.right) * direction;
                Vector3 finalVelocity = Vector3.zero;

                if (isHomingRocket)
                {
                    spawnRot = Quaternion.LookRotation(launchDirection);
                    finalVelocity = launchDirection.normalized * Speed + MyBody.linearVelocity;
                }
                else
                {
                    spawnRot = Quaternion.LookRotation(direction);
                    finalVelocity = direction.normalized * Speed + MyBody.linearVelocity;
                }

                double clientTime = NetworkManager.Singleton.ServerTime.Time;
                SpawnLocalBullet(RocketProjectile, spawnPos, spawnRot, finalVelocity, false, 2f, isHomingRocket, TargetForHomingRockets);

                if (IsOwner)
                {
                    // Resolve NetworkObject reference on the CLIENT here, not on server
                    NetworkObject targetNetObj = null;
                    if (isHomingRocket && TargetForHomingRockets != null)
                    {
                        targetNetObj = TargetForHomingRockets.GetComponent<NetworkObject>();
                        // Also check parent in case collider hit is a child object
                        if (targetNetObj == null)
                            targetNetObj = TargetForHomingRockets.GetComponentInParent<NetworkObject>();
                    }

                    FireRocketsServerRpc(
                        spawnPos, spawnRot, finalVelocity, clientTime, isHomingRocket,
                        targetNetObj != null ? (NetworkObjectReference)targetNetObj : default
                    );
                }

                RocketFireAudiosRpc();
            }
        }

        private void SpawnHomingRockets(NetworkObjectReference Target)
        {
            FireRocketsFullFucntion(true, Target);
        }

        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        private void FireRocketsServerRpc(Vector3 spawnPos, Quaternion spawnRot, Vector3 velocity, double clientTimestamp, bool isHomingRocket = false, NetworkObjectReference targetRef = default)  
        {
           
            GameObject bullet = MainObjectPooler.instance.GetObjectPoolByEnumServerRpc(ObjectType.Rocket);
            if (bullet != null)
            {

                bullet.SetActive(true);
                bullet.transform.SetPositionAndRotation(spawnPos, spawnRot);


                NetworkObject networkObject = bullet.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(OwnerClientId);

                var nt = bullet.GetComponent<NetworkTransform>();
                nt.Teleport(spawnPos, spawnRot, bullet.transform.localScale);


                bullet.GetComponent<BulletProjectileScript>().SetBulletRpc(networkObject, CarAmmoData.GetRocketDamage);

                GameObject target = null;
                if (isHomingRocket && targetRef.TryGet(out NetworkObject targetNetObj))
                {
                    target = targetNetObj.gameObject;

                }
                if(!isHomingRocket)
                {
                    bullet.GetComponent<BulletProjectileScript>().isRocket = true;
                }

                InitializeBullet(bullet, spawnRot, velocity, OwnerClientId, false, 2f, isHomingRocket, target);
                RocketVisualsRpc(networkObject);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void RocketVisualsRpc(NetworkObjectReference networkObjectReference)
        {
            networkObjectReference.TryGet(out NetworkObject networkObject);
            networkObject.gameObject.transform.GetChild(0).gameObject.GetComponent<ParticleSystemDestroyer>().RecieveShooter(this.gameObject);
            networkObject.gameObject.transform.GetChild(0).gameObject.SetActive(true);
        }
        [Rpc(SendTo.Everyone)]
        private void RocketFireAudiosRpc()
        {
            GunAudioSource.PlayOneShot(RocketLauncherSound);
        }
       


        #endregion

        #region RaceCountDown
        IEnumerator RaceCountDown()
        {
            if (LobbyManager.Instance.IsLobbyHost())
            {
                yield return new WaitForSeconds(38f);
                AssignPos.AssignCarNameGameObjectSpawnRpc();
                yield return new WaitForSeconds(7f); // was 2f — now waits long enough for all real cars to teleport first
                
                AssignCarNames.instance.SpawnAIRpc();
               
                AssignPos.SpawnProgressWayPointsServerRpc();
                
            }
        }
        #endregion

        #region AssignCarTags
        IEnumerator AssignCarTagCoroutine()
        {
            yield return new WaitForSeconds(46f);
            AssignCarNames.instance.RequestAssignCarTagsRpc();

            LobbyManager.SpawnedALLCars = true;
        }
        #endregion

        #region AssignSpawnCarsAtPosition
        IEnumerator SpawnPlayerActualPosCoroutine()
        {
            yield return new WaitForSeconds(30f);

            if (!LobbyManager.OnceFreezeCar)
            {
                FreezeCarAtStartingLight = true;
                LobbyManager.OnceFreezeCar = true;
            }

            if (!IsAICar)
            {
                gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
                //carController.Move(0, 0, 0, 0);
            }

            yield return new WaitForSeconds(5f);

            if (!IsAICar && IsServer)
            {
                AssignPosScript.SpawnNumber++;

                if (!IsOwner)
                {
                    Vector3 targetPos; Quaternion targetRot; bool assigned = true;

                    switch (AssignPosScript.SpawnNumber)
                    {
                        case 2: targetPos = AssignPos.OpponentPos[0].transform.position; targetRot = AssignPos.OpponentPos[0].transform.rotation; break;
                        case 3: targetPos = AssignPos.OpponentPos[1].transform.position; targetRot = AssignPos.OpponentPos[1].transform.rotation; break;
                        case 4: targetPos = AssignPos.OpponentPos[2].transform.position; targetRot = AssignPos.OpponentPos[2].transform.rotation; break;
                        case 5: targetPos = AssignPos.OpponentPos[3].transform.position; targetRot = AssignPos.OpponentPos[3].transform.rotation; break;
                        case 6: targetPos = AssignPos.OpponentPos[4].transform.position; targetRot = AssignPos.OpponentPos[4].transform.rotation; break;
                        case 7: targetPos = AssignPos.OpponentPos[5].transform.position; targetRot = AssignPos.OpponentPos[5].transform.rotation; break;
                        case 8: targetPos = AssignPos.OpponentPos[6].transform.position; targetRot = AssignPos.OpponentPos[6].transform.rotation; break;
                        default: targetPos = default; targetRot = default; assigned = false; break;
                    }

                    if (assigned)
                    {
                        ApplyAssignedGridPositionRpc(targetPos, targetRot, AssignPosScript.SpawnNumber - 1);
                    }
                }
                else // host
                {
                    ApplyAssignedGridPositionRpc(
                        AssignPos.PlayerPos.transform.position,
                        AssignPos.PlayerPos.transform.rotation,
                        0
                    );
                }
            }
        }

        [Rpc(SendTo.Owner)]
        public void ApplyAssignedGridPositionRpc(Vector3 pos, Quaternion rot, int carPosId)
        {
            gameObject.transform.position = pos;
            gameObject.transform.rotation = rot;
            LobbyManager.CarPosID = carPosId;
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }
        #endregion

        #region CarDie
     
        public void UpdateCarHealthRpc(float oldval, float CarHealth)
        {
          
            ShowSmokeRpc(CarHealth);
            
            if (IsLocalPlayer)
            {
                
                UIScript.instance.GetHealthBar.fillAmount =  Mathf.Clamp01(CarHealth / playerData.GetCarMaxHealth);
            }
        }
        [Rpc(SendTo.Everyone)]
        private void ShowSmokeRpc(float CarHealth)
        {
           
            if (!playerData.IsCarDead.Value)
            {
                if (CarHealth > 0 && CarHealth <= playerData.GetCarMaxHealth / 2)
                {
                    CarBlackSmokeEffect.gameObject.SetActive(true);
                    CarBlackSmokeEffect.Play();

                }
                else if (CarHealth <= 0)
                {
                    CarBlackSmokeEffect.Stop();                  
                    CarFireEffect.gameObject.SetActive(true);
                    CarFireEffect.Play();
                    
                }
            }
            
        }
       
        [Rpc(SendTo.Everyone)]
        public void CarDeathServerRpc(NetworkObjectReference DeadCarRef, NetworkObjectReference ShooterRef)
        {
           
            if (DeadCarRef.TryGet(out NetworkObject DeadCar))
            {
              
                if (DeadCar.GetComponent<PlayerData>().IsCarDead.Value) return;
                if (IsOwner)                  
                RequestCarOnZeroHealthServerRpc(DeadCarRef, ShooterRef);
               
                StartCoroutine(ExplodeCar(DeadCarRef, ShooterRef));

            }

        }
        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestCarOnZeroHealthServerRpc(NetworkObjectReference DeadCarRef, NetworkObjectReference ShooterRef)
        {
            if (DeadCarRef.TryGet(out NetworkObject DeadCar))
            {
               
                DeadCar.GetComponent<PlayerData>().IsCarOnZeroHealth.Value = true;
            }
          
        }

        [Rpc(SendTo.Everyone)]
        private void StopFireEffectRpc()
        {
            if (CarBlackSmokeEffect.isPlaying)
            {
                CarBlackSmokeEffect.Stop();
            }
            if (CarFireEffect.isPlaying)
            {
                CarFireEffect.Stop();
            }
            if (CarFireEffect.gameObject.activeInHierarchy)
            {
                CarFireEffect.gameObject.SetActive(false);
            }
            if (CarBlackSmokeEffect.gameObject.activeInHierarchy)
            {
                CarBlackSmokeEffect.gameObject.SetActive(false);
            }
        }

        private IEnumerator ExplodeCar(NetworkObjectReference DeadCarRef, NetworkObjectReference ShooterRef)
        {
            DeadCarRef.TryGet(out NetworkObject DeadCar);
            ShooterRef.TryGet(out NetworkObject Shooter);
            if (DeadCar.GetComponent<PlayerData>().IsCarDead.Value) yield break;
           
            yield return new WaitForSeconds(5f);
            
          
            StopFireEffectRpc();

            //Debug.Log("CarDead");
            GameObject Explosion = Instantiate(CarExplosionEffect, CarExplosionCube.transform.transform.position, CarExplosionCube.transform.rotation);
            if (Explosion != null)
            {
                if(IsServer)
                Explosion.GetComponent<NetworkObject>().Spawn();
            }

            CarDeadNameUpdateUIRpc(DeadCarRef, ShooterRef);
            if (IsOwner)
            {
                CarDeadBoolValueStatusServerRpc(DeadCarRef);
            }
         
           
            
          
        }
        [Rpc(SendTo.Everyone)]
        private void CarDeadNameUpdateUIRpc(NetworkObjectReference DeadCarRef, NetworkObjectReference ShooterRef)
        {
            DeadCarRef.TryGet(out NetworkObject DeadCar);
            ShooterRef.TryGet(out NetworkObject Shooter);

            UIScript.instance.GetKillText.gameObject.SetActive(true);
            if (ShooterRef.NetworkObjectId == NetworkManager.LocalClient.PlayerObject.NetworkObjectId)
            {
                UIScript.instance.GetKillText.text = $"<color=white> You </color> killed <color=green>{DeadCar.name}</color>";
            }
            else if (DeadCarRef.NetworkObjectId == NetworkManager.LocalClient.PlayerObject.NetworkObjectId)
            {
                UIScript.instance.GetKillText.text = $"<color=white> You were killed by {Shooter.name}</color>";
            }
            else if (ShooterRef.NetworkObjectId == DeadCarRef.NetworkObjectId)
            {
                UIScript.instance.GetKillText.text = $"<color=white> You Committed Suicide </color>";
            }
                        
            else
            {
                UIScript.instance.GetKillText.text = $"<color=green>{Shooter.name}</color> killed <color=red>{DeadCar.name}</color>";
            }

            StartCoroutine(DisablekillText());
           // Debug.Log($"<color=red>{ShooterRef.NetworkObjectId}</color> <color=green> {NetworkManager.LocalClient.PlayerObject.NetworkObjectId}</color>");
          
          
            //Debug.Log("Kill");
            LeaderboardUIScript.instance.AddKill(Shooter.GetComponent<PlayerData>(),NetworkObjectId);
            
            
        }
        private IEnumerator DisablekillText()
        {
            yield return new  WaitForSeconds(2f);
            UIScript.instance.GetKillText.gameObject.SetActive(false);
        }
        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        private void CarDeadBoolValueStatusServerRpc(NetworkObjectReference DeadCarRef)
        {
            if (DeadCarRef.TryGet(out NetworkObject DeadCar))
            {
                DeadCar.GetComponent<PlayerData>().IsCarDead.Value = true;
                
            }
        }


        #endregion

        #region AICarShootingMechanism

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject != null && gameObject != null)
            {
                if (IsOwner)
                {

                    if (carController.IsAICarEffective && CanShoot)
                    {

                        StartFiring(other);

                    }

                }
            }
        }

        private void StartFiring(Collider other)
        {
            PlayerData player = other.gameObject.GetComponentInParent<PlayerData>();
            if (player != null && other.gameObject != gameObject && other.transform.root != transform.root && !playerData.IsCarDead.Value)
            {
                if (!player.IsCarDead.Value)
                {
                    GunTimer += Time.deltaTime;
                    if (GunTimer >= FireRate)
                    {
                        GunTimer = 0f;

                        for (int i = 0; i < MiniGunFirePoses.Length; i++)
                        {
                            Transform o = MiniGunFirePoses[i];
                            Vector3 spawnPos = o.transform.position;
                            Quaternion spawnRot = o.transform.rotation;
                            Vector3 direction = spawnRot * Vector3.forward;
                            Vector3 finalVelocity = direction.normalized * Speed + MyBody.linearVelocity;

                            SpawnLocalBullet(BulletProjectile, spawnPos, spawnRot, finalVelocity, true, 0.8f);
                            if (IsOwner)
                            {
                                ShootBulletServerRpc(spawnPos, spawnRot, finalVelocity, OwnerClientId, true);
                                EmitMinigunSmokeRpc(i);
                            }
                        }
                    }
                }
            }

        }

        #endregion

        #region Getters
        public bool CanShoot { get; set; }

       public bool CanDefend { get; set; }

       public Canvas GetCanvas() => crosshairCanvas;
       public AudioSource MinigunAudio ()=> GunAudioSource;

        public ProgressTracker GetProgressTracker() => progressTracker; 
        
       #endregion
    }
}



    