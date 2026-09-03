using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Netcode
{
    public class AssignPosScript : NetworkBehaviour
    {
        public GameObject PlayerPos;
        public GameObject[] OpponentPos;
        public static AssignPosScript instance;
        public WaypointCircuit waypointCircuit;
        public static bool[] PositionsFilled;
        public static int SpawnNumber;
        public GameObject ProgressWayPointsGameObject;
        public GameObject AssignCarNameScriptGameObject;
        public Vector3 ProgressWayPointSpawnPos;
        public GameObject LapCollider;
        public Vector3 LapColliderSpawnPos;

        public List<GameObject> spawnedObject;
        [SerializeField] private Vector3 SpawnRotation;
        public GameObject ProgressWaypointsRef;
        public static Action ProgressWaypointsSpawned;


        private void Awake()
        {
            instance = this;
            PositionsFilled = new bool[7];

            for (int i = 0; i < PositionsFilled.Length; i++)
            {
                PositionsFilled[i] = false;
            }
           
        }
        [Rpc(SendTo.Server)]
        public void AssignCarNameGameObjectSpawnRpc()
        {
            GameObject AssignCarNameScript = Instantiate(AssignCarNameScriptGameObject, Vector3.zero, Quaternion.identity);
            AssignCarNameScript.GetComponent<NetworkObject>().Spawn();
            AssignCarNameScript.GetComponent<AssignCarNames>().OpponentPos = OpponentPos;
        }
        [ServerRpc(InvokePermission =RpcInvokePermission.Everyone)]
        public void SpawnProgressWayPointsServerRpc()
        {

            GameObject SpawnedPWP = Instantiate(ProgressWayPointsGameObject, ProgressWayPointSpawnPos, Quaternion.identity);
            SpawnedPWP.GetComponent<NetworkObject>().Spawn();

            AssignProgressWaypointRefRpc(SpawnedPWP.GetComponent<NetworkObject>());
          

            GameObject LapC = Instantiate(LapCollider, LapColliderSpawnPos, Quaternion.Euler(SpawnRotation));
            LapC.GetComponent<NetworkObject>().Spawn();

        }
        [Rpc(SendTo.Everyone)]
        private void AssignProgressWaypointRefRpc(NetworkObjectReference progressWaypintRef)
        {
        
            progressWaypintRef.TryGet(out NetworkObject ProgressWayPoint);
            ProgressWaypointsRef = ProgressWayPoint.gameObject;
            ProgressWaypointsSpawned.Invoke();
        }


    }
    
}
