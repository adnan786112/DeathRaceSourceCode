using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
public class LeaderboardUIScript : MonoBehaviour
{
    [SerializeField] private Canvas leaderboardCanvas;
    [SerializeField] private float DisplayOffsetPosX = -2;
    [SerializeField] private float DisplayOffsetPosZ = -3;
    [SerializeField] private float DisplayOffsetPosY = 5;
    [SerializeField] private float DisplayOffsetRotation = 150;
    [SerializeField] private float leaderboardDisplayTiming = 5f;
    private Vector3 localPlayerPos;
    private Quaternion leaderboardRotation;
    private Camera mainCam;
    public static int a = 0;
    public static LeaderboardUIScript instance;
    private bool changed = false;
    private readonly Dictionary<int, int> killCounts = new();
    private readonly HashSet<ulong> shooterAlreadyCounted = new();
    private void Awake()
    {
        instance = this;
        mainCam = Camera.main;
        leaderboardCanvas.renderMode = RenderMode.WorldSpace;
        leaderboardCanvas.worldCamera = mainCam;
        SaveScript.RaceStartEvent += UpdateUIPosition;
        leaderboardCanvas.enabled = false;

        for (int i = 0; i < MembersUI.pLRemainingTrnasform.Count; i++)
        {
            if (MembersUI.pLRemainingTrnasform[i] != null)
                MembersUI.pLRemainingTrnasform[i].gameObject.SetActive(false);
        }
    }
    [Serializable]
    public struct LeaderboardUIMembers
    {
        public List<RectTransform> pLPostionTrnasform;
        public List<RectTransform> pLRemainingTrnasform;
        public List<TextMeshProUGUI> Position;
        public List<TextMeshProUGUI> Name;
        public List<TextMeshProUGUI> Kills;
        public List<TextMeshProUGUI> Status;
        public List<ulong> NetworkIDs;

    };
    public LeaderboardUIMembers MembersUI;

   
    private List<PlayerData> playerData = new();

  
    public void SetUI(string name, PlayerData playerDataRef, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MembersUI.Name.Count)
        {
            Debug.LogWarning($"LeaderboardUIScript.SetUI: slotIndex {slotIndex} out of range, ignoring.");
            return;
        }

        MembersUI.Name[slotIndex].text = name;

        while (playerData.Count <= slotIndex)
            playerData.Add(null);

        playerData[slotIndex] = playerDataRef;
        playerDataRef.RecieveTransforms(MembersUI.pLRemainingTrnasform[slotIndex]);

        if (slotIndex < MembersUI.pLRemainingTrnasform.Count
            && MembersUI.pLRemainingTrnasform[slotIndex] != null)
        {
            MembersUI.pLRemainingTrnasform[slotIndex].gameObject.SetActive(true);
        }

     

       
        if (AssignCarNames.instance != null
            && AssignCarNames.instance.spawnedObject.Count > 0)
        {
            int filledCount = 0;
            foreach (var p in playerData)
                if (p != null) filledCount++;

            int totalExpected = AssignPosScript.SpawnNumber
                              + AssignCarNames.instance.spawnedObject.Count;

            if (filledCount >= totalExpected)
            {
                Lap.instance.RecieveLeaderboardTransformList(MembersUI.pLPostionTrnasform);
            }
        }
    }

    public void DisplayUI(int oldval, int newval)
    {
        if (SaveScript.RaceStart && !SaveScript.RaceOver && gameObject.activeInHierarchy)
        {
            StartCoroutine(DisplayUICoroutine());
        }
    }
    private IEnumerator DisplayUICoroutine()
    {

        leaderboardCanvas.enabled = true;
        yield return new WaitForSeconds(leaderboardDisplayTiming);
        leaderboardCanvas.enabled = false;
    }

    public void UpdateUIPosition()
    {
        if (!changed)
        {
            localPlayerPos = NetworkManager.Singleton.LocalClient.PlayerObject.transform.localPosition;
            leaderboardRotation = Quaternion.Euler(leaderboardCanvas.transform.rotation.eulerAngles.x,
            DisplayOffsetRotation, leaderboardCanvas.transform.rotation.z);
            leaderboardCanvas.transform.SetParent(Camera.main.transform);
            leaderboardCanvas.transform.SetPositionAndRotation(new Vector3(localPlayerPos.x + DisplayOffsetPosX, localPlayerPos.y + DisplayOffsetPosY, localPlayerPos.z + DisplayOffsetPosZ), leaderboardRotation);
            for (int i = 0; i <= 4; i++)
            {
                if (i >= playerData.Count || playerData[i] == null)
                {
                    Debug.LogWarning($"LeaderboardUIScript.UpdateUIPosition: slot {i} has no player assigned yet, skipping.");
                    continue;
                }
                playerData[i].RecieveTransforms(MembersUI.pLRemainingTrnasform[i]);
            }
            changed = true;
        }
    }

    // LeaderboardUIScript.cs

    public void AddKill(PlayerData shooterData, ulong localClientNetworkObjectId)
    {
        if(shooterAlreadyCounted.Contains(localClientNetworkObjectId)) return;
        
        shooterAlreadyCounted.Add(localClientNetworkObjectId);  
        
        int slot = shooterData.LeaderboardSlotIndex.Value;
        if (!killCounts.ContainsKey(slot))
            killCounts[slot] = 0;
        //Debug.Log($"<color=red> local client networkobject id{localClientNetworkObjectId}</color>");
        killCounts[slot]++;


        BroadCastKillRpc(slot, killCounts[slot].ToString());

        StartCoroutine(RemoveShooter(localClientNetworkObjectId));
        
    }
    [Rpc(SendTo.Everyone)]
    private void BroadCastKillRpc(int slot, string killCountText)
    {
        MembersUI.Kills[slot].text = killCountText;
    }

    private IEnumerator RemoveShooter(ulong localClientNetworkObjectId)
    {
        yield return new WaitForSeconds(3f);
        shooterAlreadyCounted.Remove(localClientNetworkObjectId);
    }
}
