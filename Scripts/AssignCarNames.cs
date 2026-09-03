using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
public class AssignCarNames : NetworkBehaviour
{
    public static AssignCarNames instance;
    private int NumberOfAICarsToSpawn;
    public GameObject OpponentPrefab;
    public GameObject[] OpponentPos;
    public List<GameObject> spawnedObject;
    private readonly List<string> aiCarNames = new() { "MachineGun Joe", "Hilly Billy", "Edwin", "Rockstar", "Raphel", "Tommmy", "Razor" };

    public static NetworkVariable<int> PlayerAICountStatic = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        instance = this;
    }
    [Rpc(SendTo.Server)]
    public void SpawnAIRpc()
    {
        NumberOfAICarsToSpawn = 8 - AssignPosScript.SpawnNumber;

      
        HashSet<int> takenSlots = new HashSet<int>();

        for (int p = 1; p < AssignPosScript.SpawnNumber; p++)
        {
            takenSlots.Add(p - 1);
        }
 

        int aiSpawned = 0;

        for (int i = 0; i < OpponentPos.Length && aiSpawned < NumberOfAICarsToSpawn; i++)
        {
            if (takenSlots.Contains(i)) continue;

            GameObject spawned = Instantiate(
                OpponentPrefab,
                OpponentPos[i].transform.position,
                OpponentPos[i].transform.rotation
            );
            spawned.GetComponent<NetworkObject>().Spawn();

            NetworkObject netObj = spawned.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                AddCarsGloabalRpc(new NetworkObjectReference(netObj));
            }

            aiSpawned++;

            //Debug.Log($"<color=green>AI spawned at OpponentPos[{i}], total AI = {aiSpawned}</color>");
        }

        //Debug.Log($"<color=cyan>Done: {AssignPosScript.SpawnNumber} human(s) + {aiSpawned} AI = {AssignPosScript.SpawnNumber + aiSpawned} total</color>");
    }

    [Rpc(SendTo.Everyone)]
    public void AddCarsGloabalRpc(NetworkObjectReference playerGameObject)
    {
        if (playerGameObject.TryGet(out NetworkObject networkObject))
        {
            spawnedObject.Add(networkObject.gameObject);
        }
    }

  
    private static bool carTagsAssigned = false;

    [Rpc(SendTo.Server)]
    public void RequestAssignCarTagsRpc()
    {
        if (carTagsAssigned) return;
        carTagsAssigned = true;

        int startingSlotOffset = PlayerData.AllocateLeaderboardSlots(spawnedObject.Count);
        AssignCarTagsToAllRpc(startingSlotOffset);
    }

    [Rpc(SendTo.Everyone)]
    private void AssignCarTagsToAllRpc(int startingSlotOffset)
    {

        for (int i = 0; i < spawnedObject.Count; i++)
        {
            spawnedObject[i].GetComponent<CarController>().ProgressTrackerObject.tag = "ProgressAI" + (i + 1).ToString();
            spawnedObject[i].name = aiCarNames[i];

            PlayerData aiPlayerData = spawnedObject[i].GetComponent<PlayerData>();
            aiPlayerData.SetCarNameTextLocal(aiCarNames[i]);
            spawnedObject[i].GetComponent<CarController>().IsAICarEffective = true;

            int slotIndex = startingSlotOffset + i;
            LeaderboardUIScript.instance.SetUI(aiCarNames[i], aiPlayerData, slotIndex);

            if (IsServer)
            {
                aiPlayerData.RecieveAICarNamesServerRpc(aiCarNames[i]);
            }

            aiPlayerData.MakrkerSpriteGetter.sprite = LobbyAssets.markerSprites[i];

            if (i == spawnedObject.Count - 1)
            {
                if (IsHost)
                {
                    PlayerAICountStatic.Value = i + 2;
                }
            }
        }

        // Re-register human players AFTER AI loop is done
        // Clients may have missed the early DisplayPlayerNameOnCanvasRpc
        // since it fired at OnNetworkSpawn before the leaderboard was ready.
        // By this point (~46s in) everything is settled on all machines.
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (!netObj.TryGetComponent<PlayerData>(out var pd)) continue;

            int slot = pd.LeaderboardSlotIndex.Value;
            if (slot < 0) continue; // not a human player slot

            string playerName = pd.CarName.Value.ToString();
            if (string.IsNullOrWhiteSpace(playerName)) continue;
            pd.GetCarNameText.text = playerName;
            LeaderboardUIScript.instance.SetUI(playerName, pd, slot);
        }
    }
}