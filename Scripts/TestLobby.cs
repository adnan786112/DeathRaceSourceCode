using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;

public class TestLobby : MonoBehaviour
{
    //[SerializeField] private GameObject CarToIntantiate;
    //public float minX = -5f; // Minimum X coordinate
    //public float maxX = 5f;  // Maximum X coordinate
    //public float minZ = -5f; // Minimum Y coordinate
    //public float maxZ = 5f;  // Maximum Y coordinate


    //[SerializeField] private float BeginPlayCountdown;
    //public static int NumberOfPlayers = 0;
    public static TestLobby instance;
    private string PlayerName;

    private Lobby HostLobby;
    private Lobby JoinedLobby;
    private float HeartBeatTimer;
    private float LobbyUpdateTimer;
    [SerializeField] private float HeartBeatMaxTimer;
    [SerializeField] private float LobbyUpdateMaxTimer;
    //[SerializeField] private GameObject StartButton;



    private void Awake()
    {
        instance = this;
    }



    async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => { Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId); };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        PlayerName = "Adnan" + Random.Range(0, 100);
        CreateLobby();
       
        Debug.Log(PlayerName);
    }

    void Update()
    {

        HandleLobbyHeartBeat();
        HandleLobbyPollForUpdates();
    }

    public void StartGame()
    {
    }

    public void SpawnCar()
    {

    }

    private async void JoinLobbyByCode(string LobbyCode)
    {
        JoinLobbyByCodeOptions JoinLobbyByCodeOptions = new JoinLobbyByCodeOptions
        { 
            Player = GetPlayer()
        };
        try
        {
         
           Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(LobbyCode,JoinLobbyByCodeOptions);
            JoinedLobby = lobby;
            Debug.Log("Joined Lobby With Code = " + LobbyCode);
            PrintPlayers(JoinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }
       
    }

    private Player GetPlayer()
    {
        return new Player
        {
            //Id = AuthenticationService.Instance.PlayerId,
            Data = new Dictionary<string, PlayerDataObject>
                    {
                    { "PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,PlayerName)}
                }
        };
    }

    private async void HandleLobbyHeartBeat()
    {
        if (HostLobby != null) 
        {
            HeartBeatTimer -= Time.deltaTime;
            if (HeartBeatTimer <= 0)
            {
                HeartBeatTimer = HeartBeatMaxTimer;
                await LobbyService.Instance.SendHeartbeatPingAsync(HostLobby.Id);
            }
        }
    }
    
    private async void CreateLobby()
    {
        try
        {

            string Lobbyname = "MyLobby";
            int MaxPlayers = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {
                    "GameMode",new DataObject(DataObject.VisibilityOptions.Public, "CaptureTheFlag")
                    },
                    {
                     "Map",new DataObject(DataObject.VisibilityOptions.Public,"OpenWorld")
                    }
                }
            };
            
            
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(Lobbyname, MaxPlayers,createLobbyOptions);

            HostLobby = lobby;
            JoinedLobby = HostLobby;
            Debug.Log("Created Lobby !" + lobby.Name + "Lobby Max Players " + lobby.MaxPlayers + " Lobby id = "+lobby.Id + " Lobby Code =" +lobby.LobbyCode);
            ListOfLobbies();
            PrintPlayers(HostLobby);
            JoinLobbyByCode(lobby.LobbyCode);
            //QuickJoinLobby();
          
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);

        }

    }

    private async void ListOfLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiyOptions = new QueryLobbiesOptions
            {
                Count = 8,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //new QueryFilter(QueryFilter.FieldOptions.S1,"CaptureTheFlag",QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder>
                {
                new QueryOrder(false,QueryOrder.FieldOptions.Created)
                }

            };

            QueryResponse querryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiyOptions);
            Debug.Log("Lobbies Found = " + querryResponse.Results.Count);

            foreach (Lobby lobby in querryResponse.Results)
            {
                Debug.Log("Lobby name = " + lobby.Name + "Lobby max players = " + lobby.MaxPlayers + "Game Mode = " + lobby.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }
        //JoinLobby();
    }

    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }

    }
    private void PrintPlayers()
    {
        PrintPlayers(JoinedLobby);
    }
    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in lobby = "+lobby.Name + lobby.Data["GameMode"].Value + lobby.Data["Map"].Value);
        foreach(Player player in lobby.Players) 
        {
            Debug.Log(player.Id + " Player Name = " + player.Data["PlayerName"].Value);
        }
    }
    private async void UpdateLobbyGameMode(string GameMode)
    {
        try
        {
        HostLobby = await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
        {
            {
            "GameMode",new DataObject(DataObject.VisibilityOptions.Public,GameMode)
        }
            }
            });
            JoinedLobby = HostLobby;
            PrintPlayers(HostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }
    }
    private async void HandleLobbyPollForUpdates()
    {
        if (JoinedLobby != null)
        {
            LobbyUpdateTimer -= Time.deltaTime;
            if (LobbyUpdateTimer <= 0)
            {
                LobbyUpdateTimer = LobbyUpdateMaxTimer;
               Lobby lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
                JoinedLobby = lobby;
            }
        }
    }
    private async void UpdatePlayerName(string NewPlayerName)
    {
        try
        {
            PlayerName = NewPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
            {
            {
            "PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,PlayerName)
            }
            }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }
    }
    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

    private async void KickPlayer()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, JoinedLobby.Players[1].Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void MigrateLobbyHost()
    {
        try
        {
            HostLobby = await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions
            {

                HostId = JoinedLobby.Players[2].Id
              
            
            });
            JoinedLobby = HostLobby;
            PrintPlayers(HostLobby);
        }

        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }
    }
    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(JoinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);

        }
    }
}
