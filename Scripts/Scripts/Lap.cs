using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Lap : NetworkBehaviour
{
    public Text LapNumberText;
    public Text PlayersPosition;

    public List<int> PlayerPositionList = new();
  


    public static Lap instance;
    private readonly float positionUpdateRate = 0.5f;
    private readonly HashSet<ulong> lap1AlreadyCounted = new HashSet<ulong>();

    [SerializeField] private float stepDuration = 0.25f;

    private readonly Dictionary<ulong, Transform> cardRT = new();
    private readonly Dictionary<ulong, float> cardFixedX = new();
    private readonly Dictionary<ulong, float> cardFixedZ = new();
    private readonly Dictionary<ulong, int> cardTargetSlot = new();
    private readonly Dictionary<ulong, Coroutine> cardAnimCoroutines = new();

    private List<RectTransform> leaderboardTrnasforms = new();
    private readonly Dictionary<ulong, int> cardCurrentSlot = new();

    // Guard so only ONE SortPositionsRoutine ever runs server-side,
    // even though every player's OnNetworkSpawn calls the ServerRpc below.
    private static bool sortRoutineStarted = false;
    private PlayerData playerData;
    private void Awake() { instance = this; }
    public static int nextFinishPlacement = 0;
    private static readonly HashSet<ulong> finishedPlayers = new();

    private void OnEnable()
    {
        if (SaveScript.RaceStart) { LapNumberText.text = "0"; PlayersPosition.text = "1"; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            SortPositionsFunctionServerRpc();
        }
        LapNumberText = UIScript.instance.LapNumberText;
        PlayersPosition = UIScript.instance.PlayersPosition;
        playerData = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerData>();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RegisterFinishServerRpc(NetworkObjectReference playerRef)
    {
        if (!playerRef.TryGet(out NetworkObject netObj)) return;

        ulong id = netObj.NetworkObjectId;
        if (finishedPlayers.Contains(id)) return; // prevent double counting
        finishedPlayers.Add(id);

        int placement = nextFinishPlacement + 1;
        nextFinishPlacement = placement;

        // Broadcast the result once, directly — no NetworkVariable polling delay
        AnnounceFinishRpc(id, placement);
    }
    [Rpc(SendTo.Everyone)]
    private void AnnounceFinishRpc(ulong netId, int placement)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[netId];
        if (obj == null) return;
        if (!obj.TryGetComponent<PlayerData>(out var pd)) return;

        pd.FinishPlacement = placement; // plain int field, not a NetworkVariable — see below

        if (obj.IsLocalPlayer && !obj.GetComponent<CarController>().IsAICarEffective)
            FinishPointManager.instance.OnLocalPlayerFinished(obj.transform, placement,obj.GetComponent<FinishCinematic>());
    }

    [ServerRpc]
    private void SortPositionsFunctionServerRpc()
    {
        if (sortRoutineStarted) return;
        sortRoutineStarted = true;
        StartCoroutine(SortPositionsRoutine());
    }

    private IEnumerator SortPositionsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(positionUpdateRate);

            List<(PlayerData player, int rankIndex)> assignments = null;

            try
            {
                var playerObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList;
                var progressList = new List<(PlayerData player, int lap, int wp, float dist)>();

                foreach (var obj in playerObjects)
                    if (obj.TryGetComponent<PlayerData>(out var pd))
                        progressList.Add((pd, pd.CarLapPosition.Value, pd.CurrentWaypoint.Value, pd.DistanceToNextWaypoint.Value));

                var sorted = progressList
                    .OrderByDescending(p => p.lap)
                    .ThenByDescending(p => p.wp)
                    .ThenBy(p => p.dist)
                    .ToList();
              
                assignments = new List<(PlayerData player, int rankIndex)>();
                for (int i = 0; i < sorted.Count; i++)
                {
                    var player = sorted[i].player;

                    player.CarPosition.Value = i + 1;
                    PlayerPositionList[i] = player.CarPosition.Value;                  
                    assignments.Add((player, i));
                }
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SortPositionsRoutine Error: {e}");
            }

            if (assignments != null)
            {
                foreach (var (player, rankIndex) in assignments)
                {
                    var netObj = player.GetComponent<NetworkObject>();
                  

                    if (SaveScript.RaceStart)
                        UpdateLeaderboardTargetRpc(netObj.NetworkObjectId, rankIndex);

                    //UpdatePlayerPositionClientRpc(netObj.NetworkObjectId, rankIndex + 1);
                }
            }
        }
    }

    /// <summary>
    /// Runs on every machine (server + every client, including host) because it's SendTo.Everyone.
    /// Each machine looks up its OWN local reference to that player's leaderboard card and
    /// animates it locally. This is what makes the movement visible to remote clients.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void UpdateLeaderboardTargetRpc(ulong netId, int targetSlotIndex)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[netId];
        if (obj == null) return;
        if (!obj.TryGetComponent<PlayerData>(out var player)) return;

        SetLeaderboardTarget(netId, player, targetSlotIndex);
    }

    private void SetLeaderboardTarget(ulong netId, PlayerData player, int targetSlotIndex)
    {
        if (leaderboardTrnasforms == null || leaderboardTrnasforms.Count == 0) return;
        if (player.LeaderboardTransform == null) return;
        if (targetSlotIndex < 0 || targetSlotIndex >= leaderboardTrnasforms.Count) return;

        Transform rt = player.LeaderboardTransform;
        if (rt == null) return;

        if (!cardRT.ContainsKey(netId))
        {
            cardRT[netId] = rt;
            cardFixedX[netId] = rt.localPosition.x;
            cardFixedZ[netId] = rt.localPosition.z;
            cardCurrentSlot[netId] = targetSlotIndex;
            cardTargetSlot[netId] = targetSlotIndex;
            rt.localPosition = new Vector3(cardFixedX[netId], leaderboardTrnasforms[targetSlotIndex].localPosition.y, cardFixedZ[netId]);
            return;
        }

        if (cardTargetSlot[netId] == targetSlotIndex) return;

        cardTargetSlot[netId] = targetSlotIndex;

        if (cardAnimCoroutines.ContainsKey(netId) && cardAnimCoroutines[netId] != null)
            StopCoroutine(cardAnimCoroutines[netId]);

        cardAnimCoroutines[netId] = StartCoroutine(AnimateCard(netId));
    }

    private IEnumerator AnimateCard(ulong netId)
    {
        if (!cardRT.ContainsKey(netId) || cardRT[netId] == null) yield break;

        cardRT[netId].SetAsLastSibling();

        while (true)
        {
            int target = cardTargetSlot[netId];
            int current = cardCurrentSlot[netId];

            if (current == target) yield break;

            int nextSlot = current + (target > current ? 1 : -1);
            nextSlot = Mathf.Clamp(nextSlot, 0, leaderboardTrnasforms.Count - 1);

            float fromY = leaderboardTrnasforms[current].localPosition.y;
            float toY = leaderboardTrnasforms[nextSlot].localPosition.y;

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                if (cardRT[netId] == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepDuration);
                float ease = t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
                cardRT[netId].localPosition = new Vector3(cardFixedX[netId], Mathf.Lerp(fromY, toY, ease), cardFixedZ[netId]);
                yield return null;
            }

            cardRT[netId].localPosition = new Vector3(cardFixedX[netId], toY, cardFixedZ[netId]);
            cardCurrentSlot[netId] = nextSlot;

            yield return new WaitForSeconds(stepDuration * 0.3f);
        }
    }


    public void RecieveLeaderboardTransformList(List<RectTransform> rectTransforms)
    {
        leaderboardTrnasforms = rectTransforms;
    }

    private void Update()
    {
        UpdatePlayerStatsRpc();
      
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (other.gameObject.CompareTag("Progress") || other.gameObject.CompareTag("ProgressAI1") || other.gameObject.CompareTag("ProgressAI2")
            || other.gameObject.CompareTag("ProgressAI3") || other.gameObject.CompareTag("ProgressAI4") || other.gameObject.CompareTag("ProgressAI5")
            || other.gameObject.CompareTag("ProgressAI6") || other.gameObject.CompareTag("ProgressAI7"))
        {
            NetworkObject networkObject = other.gameObject.GetComponentInParent<NetworkObject>();
            if (networkObject != null)
                UpdateLapCountAllCarsRpc(new NetworkObjectReference(networkObject));
        }
    }

    IEnumerator WrongWayReset() { yield return new WaitForSeconds(1.5f); SaveScript.WWTextReset = false; }

    [Rpc(SendTo.Everyone)]
    public void UpdateLapCountAllCarsRpc(NetworkObjectReference playerGameObject)
    {
        if (playerGameObject.TryGet(out NetworkObject networkObject))
        {
            //Debug.Log(networkObject.name);
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("Progress"))
            {
                Debug.Log(networkObject.name);
                SaveScript.WWTextReset = true;
                StartCoroutine(WrongWayReset());

                if (SaveScript.RaceOver == false)
                {

                    if (networkObject.GetComponent<PlayerData>().HalfWayActivated.Value)
                    {
                        //Debug.Log(networkObject.name + "lap1");
                        if (IsOwner)
                        {
                            OnPlayer1LapChangeServerRpc(playerGameObject);
                        }

                    }
                }
            }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI1"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI2"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI3"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI4"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI5"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI6"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
            if (networkObject.transform.GetChild(0).gameObject.CompareTag("ProgressAI7"))
            { if (IsOwner) { OnAILapChangeServerRpc(playerGameObject); } }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UpdatePlayerStatsRpc()
    {
        if (SaveScript.RaceStart)
        {
                     
            PlayersPosition.text = playerData.CarPosition.Value.ToString();
            LapNumberText.text = playerData.CarLapPosition.Value.ToString();
            
        }
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void OnPlayer1LapChangeServerRpc(NetworkObjectReference playerGameObject)
    {
       
        if (playerGameObject.TryGet(out NetworkObject networkObject))
        {
         
            var playerData = networkObject.GetComponent<PlayerData>();
            if (playerData.CarLapPosition.Value >= SaveScript.MaxLaps) return;
            playerData.HalfWayActivated.Value = false;
            bool IsYourCar = false;
            int lapValue = 0;
            if (!IsYourCar)
            {
               
                if (lap1AlreadyCounted.Contains(networkObject.NetworkObjectId)) return;
                lap1AlreadyCounted.Add(playerGameObject.NetworkObjectId);
            }
            if (playerData != null)
            {
                int newLap = playerData.CarLapPosition.Value + 1; 
                playerData.CarLapPosition.Value = newLap;
              
                IsYourCar = true;
            
            }
            if (playerData.CarLapPosition.Value > lapValue)
            {
               lap1AlreadyCounted.Remove(networkObject.NetworkObjectId);
                IsYourCar = false;
                lapValue++;
            }
        }
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void OnAILapChangeServerRpc(NetworkObjectReference playerGameObject)
    {
     
        if (playerGameObject.TryGet(out NetworkObject networkObject))
        {
            var playerData = networkObject.GetComponent<PlayerData>();
            if (playerData.CarLapPosition.Value >= SaveScript.MaxLaps) return;
            int lapValue = 0;
            bool IsYourCar = false;
           
            if (!IsYourCar)
            {

                if (lap1AlreadyCounted.Contains(networkObject.NetworkObjectId)) return;
                lap1AlreadyCounted.Add(playerGameObject.NetworkObjectId);
            }
            if (playerData != null)
            {
                int newLap = playerData.CarLapPosition.Value + 1;
                playerData.CarLapPosition.Value = newLap;
             
                IsYourCar = true;

            }
            if (playerData.CarLapPosition.Value > lapValue)
            {
                lap1AlreadyCounted.Remove(networkObject.NetworkObjectId);
                IsYourCar = false;
                lapValue++;
            }


        }
    }
}