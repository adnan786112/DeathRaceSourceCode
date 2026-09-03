using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
namespace Unity.Netcode
{
    public class PlayerSpawner : NetworkBehaviour
    {
        public float minX = -10f;
        public float maxX = 10f;
        public float minY = -10f;
        public float maxY = 10f;
        public float minZ = -10f;
        public float maxZ = 10f;
        //[SerializeField] private GameObject LobbyStartCam;
        public static PlayerSpawner instance;
        private void Awake()
        {
            instance = this;
           
        }
        public Vector3 SpawnPlayerAtRandomPoint()
        {
            return new Vector3(
                UnityEngine.Random.Range(minX, maxX),
                UnityEngine.Random.Range(minY, maxY),
                UnityEngine.Random.Range(minZ, maxZ)
            );

        }

        
    }
}
