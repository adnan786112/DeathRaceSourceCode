using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class PlayerData : NetworkBehaviour
{

    [SerializeField] private Canvas NameCanvas;
    [SerializeField] private TextMeshProUGUI CarNameText;
    [SerializeField] private TextMeshProUGUI CarPositionText;
    [SerializeField] private Canvas MarkerCanavas;
    [SerializeField] private Image MarkerSprite;
    [SerializeField] private float CarMaxHealth;
    public Image MakrkerSpriteGetter => MarkerSprite;

    public NetworkVariable<int> CarPosition = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CarLapPosition = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CurrentWaypoint = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> DistanceToNextWaypoint = new(0F, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> CarName = new(" ", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> CarHealthNetwork = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsCarDead = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsCarOnZeroHealth = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerCharacterIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> HalfWayActivated = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> LeaderboardSlotIndex = new(-1,    NetworkVariableReadPermission.Everyone,    NetworkVariableWritePermission.Server);
    public int FinishPlacement = 0;
    public Transform LeaderboardTransform;



    //public float CarHealth = 100f;
    public delegate void CarDeathDelegate(NetworkObjectReference DeathCarRef, NetworkObjectReference ShooterRef);
    public CarDeathDelegate CarDeathEvent;
    private Animator[] ArtificialVignette;
    private bool IsPressurePlatesActivated = false;
   

    private static int nextLeaderboardSlot = 0;
    private bool IsAICar = false;

    #region Leaderboard

    public static int AllocateLeaderboardSlots(int count)
    {
        int start = nextLeaderboardSlot;
        nextLeaderboardSlot += count;
        return start;
    }
    [Rpc(SendTo.Everyone)]
    public void DisplayPlayerNameOnCanvasRpc(FixedString64Bytes name, int slotIndex)
    {
      
        CarNameText.text = CarName.Value.ToString();          
        if (!gameObject.GetComponent<CarUserControl>().IsAICar)
        {        
            LeaderboardUIScript.instance.SetUI(name.ToString(), this, slotIndex);
        }
    }
    [Rpc(SendTo.Everyone)]
    private void UpdateCarPositionOnCanvasRpc()
    {
        CarPositionText.text = CarPosition.Value.ToString();
    }
    #endregion

    #region  OnNetworkSpawn

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
         
            CarHealthNetwork.Value = CarMaxHealth;
        }
        bool isAICar = gameObject.GetComponent<CarUserControl>() != null
                       && gameObject.GetComponent<CarUserControl>().IsAICar;


        IsAICar = gameObject.GetComponent<CarController>().IsAICarEffective;
        if (IsSpawned)
        {
            SaveScript.RaceStartEvent += OnRaceStartRpc;
        }
            NameCanvas.enabled = false;
        if (IsOwner && !IsAICar)
        {
           
            SetPlayerNameServerRpc(LobbyManager.PendingPlayerName);
            NameCanvas.worldCamera = Camera.main;
            MarkerCanavas.worldCamera = Camera.main;
    
           
        }

        if (IsLocalPlayer)
        {
            ArtificialVignette = UIScript.instance.GetArtificialVignette;
        }

        if (IsLocalPlayer)
        {
            CarPosition.OnValueChanged += LeaderboardUIScript.instance.DisplayUI;
        }
        PlayerCharacterIndex.OnValueChanged += OnCharacterIndexChanged;

        ApplyCharacterSprite(PlayerCharacterIndex.Value);
      

        int characterIndex = GetLocalPlayerCharacterIndex();
        if (IsOwner)
        {
            SetPlayerCharacterServerRpc(characterIndex);
            
        }


        MarkerCanavas.transform.SetParent(MarkerCanvases.instance.transform);
        CarDeathEvent += this.GetComponent<CarUserControl>().CarDeathServerRpc;
        CarHealthNetwork.OnValueChanged += this.GetComponent<CarUserControl>().UpdateCarHealthRpc;
        
        if (IsLocalPlayer && IsOwner)
        {
            CarLapPosition.OnValueChanged += this.GetComponent<CarUserControl>().GetProgressTracker().OnLapChange;
        }

    }
    [Rpc(SendTo.Everyone)]
    public void OnRaceStartRpc()
    {
        if (!IsOwner || IsAICar)
        {
            NameCanvas.enabled = true;
        }
    }


    #endregion

    #region NameAndProfile
    public void SetCarNameTextLocal(string name)
    {
        CarNameText.text = name;
    }

    private void OnCharacterIndexChanged(int previous, int current)
    {
        ApplyCharacterSprite(current);
    }

    private void ApplyCharacterSprite(int characterIndex)
    {
        LobbyManager.PlayerCharacter character = (LobbyManager.PlayerCharacter)characterIndex;
        MarkerSprite.sprite = LobbyAssets.Instance.GetSprite(character);
    }

    private int GetLocalPlayerCharacterIndex()
    {
        return (int)LobbyManager.LocalPlayerCharacter;
    }

    [ServerRpc]
    public void SetPlayerCharacterServerRpc(int characterIndex)
    {
        PlayerCharacterIndex.Value = characterIndex;
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerNameServerRpc(FixedString64Bytes name)
    {
        CarName.Value = name;

        int slotIndex = nextLeaderboardSlot;
        nextLeaderboardSlot++;

        LeaderboardSlotIndex.Value = slotIndex; // persist it

        DisplayPlayerNameOnCanvasRpc(CarName.Value.ToString(), slotIndex);
    }

    [ServerRpc]
    public void RecieveAICarNamesServerRpc(FixedString64Bytes name)
    {
        CarName.Value = name;
    }
    public void RecieveTransforms(Transform leaderboardTransform)
    {
        LeaderboardTransform = leaderboardTransform;
    }
    #endregion

    #region Despawn
    public override void OnNetworkDespawn()
    {
        PlayerCharacterIndex.OnValueChanged -= OnCharacterIndexChanged;
    }

 
    #endregion

    #region Update

    private void Update()
    {
        if (IsOwner)
        {
            ClampCarHealthServerRpc();
            if (IsLocalPlayer)
            {
                if (AssignProgressWayPoints.instance != null)
                {
                    if (CarLapPosition.Value == SaveScript.MaxLaps && CurrentWaypoint.Value == AssignProgressWayPoints.instance.GetProgressWaypoints - 1)
                    {
                      
                        if (ArtificialVignette != null)
                        {
                            Debug.Log("articifical vignette");
                            foreach (Animator o in ArtificialVignette)
                            {
                                o.enabled = true;
                            }

                        }
                        UIScript.instance.HideCanvas();
                        gameObject.GetComponent<CarUserControl>().GetCanvas().enabled = false;
                    }
                }
            }
            if (CarLapPosition.Value > 1)
            {
                   
                if (PressurePlateMasterScript.instance != null && !IsPressurePlatesActivated)
                {
                    PressurePlateMasterScript.instance.GetPressurePlateScripts.ForEach(o =>
                    {
                        var target = RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp);
                        o.ActivatePressurePlatesRpc(this.NetworkObject, IsAICar, new RpcParams { Send = new RpcSendParams { Target = target } });
                        IsPressurePlatesActivated = true;

                    });

                }
            }
                     
        NameCanvas.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
        MarkerCanavas.transform.position = this.transform.position;        
        }
 
        UpdateCarPositionOnCanvasRpc();
    }

    #endregion

    #region Health

    [ServerRpc]
    private void ClampCarHealthServerRpc()
    {
        CarHealthNetwork.Value = Math.Clamp(CarHealthNetwork.Value, 0, CarMaxHealth);
    }
    #endregion

    #region Getters

    public float GetCarMaxHealth => CarMaxHealth;
   
    public TextMeshProUGUI GetCarNameText => CarNameText;
    public bool GetIsAICar() => IsAICar;
    #endregion
}
