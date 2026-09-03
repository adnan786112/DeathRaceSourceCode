using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityEngine.Windows;


public class LobbyManager : NetworkBehaviour {


    public static LobbyManager Instance { get; private set; }
    public static string PendingPlayerName;

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_START_GAME = "0";


    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public event EventHandler<EventArgs> OnGameStarted;

    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }
 

    public enum GameMode {
        CaptureTheFlag,
        Conquest
    }

    public enum PlayerCharacter {
        Dodge,
        Ninja,
        Assasin,
        Frankenstein,
        JamesBond,
        Skullface,
        Shooter,
        Hitman

    }



    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName;
    public static bool AllClientsConnected = false;
    public static bool SpawnedALLCars = false;
    public static bool SpawnedAIOnceChanged = false;
    public static bool SpawnedAllCarsChanged = false;
    public static bool RaceCountDownOnce = false;
    public static bool OnceFreezeCar = false;
    public static int CarPosID;
    private ulong cachedLastPlaceClientId;
    public static bool MigrationInProgress = false;
    public static LobbyManager.PlayerCharacter LocalPlayerCharacter = PlayerCharacter.Assasin;
    private string cachedOldRelayCode;


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        //StartCoroutine(SubscribeToNetworkManagerEvents());
    }

    //private IEnumerator SubscribeToNetworkManagerEvents()
    //{
    //    yield return new WaitUntil(() => NetworkManager.Singleton != null);

    //    NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
    //    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

    //    Debug.Log("LobbyManager successfully subscribed to NetworkManager events.");
    //}

    //private void OnClientDisconnectCallback(ulong clientId)
    //{
    //    Debug.Log("Disconencted client id" + clientId.ToString());
      
    //    if (IsClient)
    //    {
    //        Debug.Log("Migrating");
    //        UpdateCachedLastPlaceClientId(clientId);
    //        BeginHostMigration();
    //    }
    //    else
    //    {
    //        Debug.Log("Migrating1");
    //    }
    //}

 
    public void UpdateCachedLastPlaceClientId(ulong newValue)
    {
        cachedLastPlaceClientId = newValue;
    }

    private async void BeginHostMigration()
    {
        Debug.Log("migrationbool");
        if (MigrationInProgress) return;
        MigrationInProgress = true;

        // Capture the current relay code BEFORE shutdown, so the polling loop
        // below can detect when it actually changes.
        cachedOldRelayCode = joinedLobby != null && joinedLobby.Data.ContainsKey(KEY_START_GAME)
            ? joinedLobby.Data[KEY_START_GAME].Value
            : null;

        bool iAmNewHost = (cachedLastPlaceClientId == NetworkManager.Singleton.LocalClientId);
        Debug.Log(iAmNewHost);
        Debug.Log("migrationhost");
        if (NetworkManager.Singleton.IsClient) NetworkManager.Singleton.Shutdown();
        await System.Threading.Tasks.Task.Delay(500); // let shutdown settle

        if (iAmNewHost)
        {
            Debug.Log("newhost");
            string relayCode = await TestRelay.instance.CreateRelay();

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                HostId = AuthenticationService.Instance.PlayerId,
                Data = new Dictionary<string, DataObject> {
                { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            }
            });
            joinedLobby = lobby;
        }
        else
        {
            string newRelayCode = null;
            while (newRelayCode == null)
            {
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                string currentCode = joinedLobby.Data[KEY_START_GAME].Value;
                if (currentCode != cachedOldRelayCode)
                {
                    newRelayCode = currentCode;
                }
                await System.Threading.Tasks.Task.Delay(500);
            }
            TestRelay.instance.JoinRelay(newRelayCode);
        }

        MigrationInProgress = false;
    }
    private void OnTransportFailure()
    {
        Debug.LogError("Transport failed! Attempting to reconnect...");

        // Shutdown current network manager
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Wait a bit and try to recreate relay
        StartCoroutine(RetryConnection());
    }
    private IEnumerator RetryConnection()
    {
        yield return new WaitForSeconds(2f);

        // Try to restart the game/lobby
        if (Instance != null && IsLobbyHost())
        {
            // Recreate relay and restart
            StartGame();
        }
    }


    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
        DontDestroyOnLoad(this);

    }

    public async void Authenticate(string playerName) {
        this.playerName = playerName;

        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);




        AuthenticationService.Instance.SignedIn += async () => {
            // do nothing
            if (AuthenticationService.Instance != null)
            {
                Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);
                //if (!CarUserControl.PlayercarNameDictionary.ContainsKey(AuthenticationService.Instance.PlayerId))
                {
                    //NetworkObjectReference networkObjectReference = gameObject.GetComponent<NetworkObject>();
                    //StorePlayercarNameServerRpc(networkObjectReference);
                    // Set this once login or player chooses name
                    PendingPlayerName = playerName;


                    //CarUserControl.PlayercarNameDictionary.Add(AuthenticationService.Instance.PlayerId, ConvertStringToNetworkString(playerName));
                }
            }
            RefreshLobbyList();
            //await VoiceChatManager.instance.LoginToVivox();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    



    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling() {
        try
        {
            if (joinedLobby != null)
            {
                lobbyPollTimer -= Time.deltaTime;
                if (lobbyPollTimer < 0f)
                {
                    float lobbyPollTimerMax = 1.1f;
                    lobbyPollTimer = lobbyPollTimerMax;

                    joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    if (!IsPlayerInLobby())
                    {
                        // Player was kicked out of this lobby
                        Debug.Log("Kicked from Lobby!");

                        OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                        joinedLobby = null;
                    }
                    if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                    {
                        if (!IsLobbyHost())
                        {
                            await SceneManager.LoadSceneAsync(4);

                            //gameObject.transform.position = new Vector3(UnityEngine.Random.Range(-100,100),0,1);
                            //NetworkManager.Singleton.StartClient();
                            if (TestRelay.instance != null && IsPlayerInLobby())
                            {
                                TestRelay.instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                            }
                        }
                        joinedLobby = null;

                        OnGameStarted?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public Lobby GetJoinedLobby() {
        return joinedLobby;
    }

    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }

    private Player GetPlayer() {
        //Authenticate(playerName);
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Assasin.ToString()) }
        });
    }

    public void ChangeGameMode() {
        if (IsLobbyHost()) {
            GameMode gameMode =
                Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            switch (gameMode) {
                default:
                case GameMode.CaptureTheFlag:
                    gameMode = GameMode.Conquest;
                    break;
                case GameMode.Conquest:
                    gameMode = GameMode.CaptureTheFlag;
                    break;
            }

            UpdateLobbyGameMode(gameMode);
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {
        Player player = GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                {KEY_START_GAME, new  DataObject(DataObject.VisibilityOptions.Member,"0")}
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        //VoiceChatManager.instance.JoinLobbyChannel(lobby.Id); 
        Debug.Log("Created Lobby " + lobby.Name);
    }

    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode) {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions {
            Player = player,

        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void JoinLobby(Lobby lobby) {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        //VoiceChatManager.instance.JoinLobbyChannel(joinedLobby.Id);
    }

    public LobbyManager.PlayerCharacter GetCharacterForPlayer(string playerId)
    {
        if (joinedLobby == null) return PlayerCharacter.Assasin;

        foreach (Player player in joinedLobby.Players)
        {
            if (player.Id == playerId && player.Data != null
                && player.Data.ContainsKey(KEY_PLAYER_CHARACTER))
            {
                if (System.Enum.TryParse(
                    player.Data[KEY_PLAYER_CHARACTER].Value,
                    out PlayerCharacter character))
                {
                    return character;
                }
            }
        }

        return PlayerCharacter.Assasin; // fallback
    }

    public async void UpdatePlayerName(string playerName) {
        this.playerName = playerName;

        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };


                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;
              
                PendingPlayerName = playerName;


                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public NetworkVariable<string> ConvertStringToNetworkString(string PlayerCarName)
    {
        return new NetworkVariable<string>(PlayerCarName, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter)
    {
        if (joinedLobby != null)
        {
            try
            {
                // Cache it immediately when player selects
                LocalPlayerCharacter = playerCharacter;

                UpdatePlayerOptions options = new UpdatePlayerOptions();
                options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {
                    KEY_PLAYER_CHARACTER, new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: playerCharacter.ToString())
                }
            };

                string playerId = AuthenticationService.Instance.PlayerId;
                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public async void QuickJoinLobby() {
        try {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
                //VoiceChatManager.instance.LeaveChannel();
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void UpdateLobbyGameMode(GameMode gameMode) {
        try {
            Debug.Log("UpdateLobbyGameMode " + gameMode);
            
            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void StartGame()
    {
        if (IsLobbyHost()) 
        {
            try
            {
                await SceneManager.LoadSceneAsync(4);
                string RelayCode = await TestRelay.instance.CreateRelay();
                //TestRelay.instance.JoinRelay(RelayCode);
                Debug.Log("Allclients connected = " + AllClientsConnected);
                
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            KEY_START_GAME , new DataObject(DataObject.VisibilityOptions.Member,RelayCode)
                            
                        }
                    }

                });

                //NetworkManager.Singleton.StartClient();
            }
            catch (LobbyServiceException e) 
            {
                Debug.Log(e);
            }
        }
       
    }

}