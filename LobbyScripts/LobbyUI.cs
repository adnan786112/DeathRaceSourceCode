using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace Unity.Netcode
{

    public class LobbyUI : NetworkBehaviour
    {


        public static LobbyUI Instance { get; private set; }


        [SerializeField] private Transform playerSingleTemplate;
        [SerializeField] private Transform container;
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI gameModeText;
        [SerializeField] private Button changeAssasinButton;
        [SerializeField] private Button changeNinjaButton;
        [SerializeField] private Button changeHitmanButton;
        [SerializeField] private Button changeDodgeButton;
        [SerializeField] private Button changeJamesBondButton;
        [SerializeField] private Button changeShooterButton;
        [SerializeField] private Button changeSkullfaceButton;
        [SerializeField] private Button changeFrankensteinButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private Button changeGameModeButton;
        [SerializeField] private Button StartButton;

        [SerializeField] private List<Button> buttonArrayList;

        private Button HolderBtn;
        private bool buttonState;

        private List<Button> buttonList = new();
        private Dictionary<LobbyManager.PlayerCharacter, Button> characterButtonMap;
    

        private void Awake()
        {
            Instance = this;

            playerSingleTemplate.gameObject.SetActive(false);
            characterButtonMap = new Dictionary<LobbyManager.PlayerCharacter, Button>
            {
            { LobbyManager.PlayerCharacter.Assasin,      changeAssasinButton },
            { LobbyManager.PlayerCharacter.Ninja,        changeNinjaButton },
            { LobbyManager.PlayerCharacter.Hitman,       changeHitmanButton },
            { LobbyManager.PlayerCharacter.Dodge,        changeDodgeButton },
            { LobbyManager.PlayerCharacter.JamesBond,    changeJamesBondButton },
            { LobbyManager.PlayerCharacter.Shooter,      changeShooterButton },
            { LobbyManager.PlayerCharacter.Skullface,    changeSkullfaceButton },
            { LobbyManager.PlayerCharacter.Frankenstein, changeFrankensteinButton },
            };
         

          
            changeAssasinButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Assasin);
             
                
            });
            changeNinjaButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Ninja);
                
              
                
            });
            changeHitmanButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Hitman);
               
               
                
            });
            changeDodgeButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Dodge);
               
              
               
            });
            changeJamesBondButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.JamesBond);
               
              
            });
            changeShooterButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Shooter);
           
               
          
            });
           changeSkullfaceButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Skullface);
               
             
            });
            changeFrankensteinButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Frankenstein);
               
              
             
            });

            leaveLobbyButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.LeaveLobby();
            });

            changeGameModeButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.ChangeGameMode();
            });

            StartButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.StartGame();
            });

        }

        //public void ToggleButtonState(Button button , bool state, List<Button> buttonArray)
        //{
        //    //foreach (Button b in buttonArray)
        //    //{
        //    //    b.interactable = !state;
        //    //}
        //    //button.GetComponent<Button>().interactable = state;                 


        //    HolderBtn = button;
        //    buttonState = state;
        //}

        //public void UpdateButtonState()
        //{
        //    if (IsLocalPlayer)
        //    {
        //        HolderBtn.interactable = buttonState;
        //        buttonList.Remove(HolderBtn);
        //    }
        //    else
        //    {

        //        foreach (Button b in buttonList)
        //        {
        //            b.interactable = !buttonState;
        //        }
        //        //buttonList.Add(HolderBtn);

        //    }


        //}
        public void RefreshCharacterButtons()
        {
            Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
            if (lobby == null) return;

            string localPlayerId = AuthenticationService.Instance.PlayerId;

            // Find what THIS local player has currently selected
            LobbyManager.PlayerCharacter? myCharacter = null;
            foreach (Player player in lobby.Players)
            {
                if (player.Id == localPlayerId && player.Data != null
                    && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_CHARACTER))
                {
                    if (System.Enum.TryParse(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value,
                        out LobbyManager.PlayerCharacter c))
                    {
                      
                        myCharacter = c;
                       
                        
                    }
                }
            }

            // Collect characters taken by OTHER players only
            HashSet<LobbyManager.PlayerCharacter> takenByOthers = new HashSet<LobbyManager.PlayerCharacter>();
            foreach (Player player in lobby.Players)
            {
                if (player.Id == localPlayerId) continue; // skip self

                if (player.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_CHARACTER))
                {
                    if (System.Enum.TryParse(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value,
                        out LobbyManager.PlayerCharacter c))
                    {
                        takenByOthers.Add(c);
                    }
                }
            }

            // Apply interactable state to every button
            foreach (var kvp in characterButtonMap)
            {
                LobbyManager.PlayerCharacter character = kvp.Key;
                Button btn = kvp.Value;

                bool takenByOther = takenByOthers.Contains(character);

                // Disable if taken by someone else
                // Always keep your OWN current selection clickable (so you can reaffirm it)
                btn.interactable = !takenByOther;
            }
        }
        private void OnEnable()
        {
            LobbyManager.AllClientsConnected = false;
            LobbyManager.SpawnedALLCars = false;
            LobbyManager.SpawnedAllCarsChanged = false;
            LobbyManager.SpawnedAIOnceChanged = false;
            LobbyManager.RaceCountDownOnce = false;
            StartingLightsScript.ChangedStartingLightCoroutine = false;
            LobbyManager.OnceFreezeCar = false;
            CarController.PerframeNameAssignbool = false;
        }

        private void Start()
        {
            LobbyManager.Instance.OnJoinedLobby += UpdateLobby_Event;
            LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
            LobbyManager.Instance.OnJoinedLobbyUpdate += OnLobbyUpdated;
            LobbyManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event;
            LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
            LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;
           
            Hide();
        }
        private void OnLobbyUpdated(object sender, LobbyManager.LobbyEventArgs e)
        {
            RefreshCharacterButtons();
        }

        private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e)
        {
            ClearLobby();
            Hide();
        }

        private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
        {
            UpdateLobby();
        }

        private void UpdateLobby()
        {
            UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
        }

        private void UpdateLobby(Lobby lobby)
        {
            if (container != null)
            {
                ClearLobby();

                foreach (Player player in lobby.Players)
                {
                    Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
         
                    playerSingleTransform.gameObject.SetActive(true);
                    LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

                    lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                        LobbyManager.Instance.IsLobbyHost() &&
                        player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                    );

                    lobbyPlayerSingleUI.UpdatePlayer(player);
                }

                changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

                lobbyNameText.text = lobby.Name;
                playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
                gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;

                Show();
            }
        }

        private void ClearLobby()
        {
            if (container != null)
            {
                foreach (Transform child in container)
                {
                    if (child == playerSingleTemplate) continue;
                    Destroy(child.gameObject);
                }
            }
        }
        private void Update()
        {
            if (LobbyManager.Instance.IsLobbyHost())
            {
                StartButton.gameObject.SetActive(true);
            }
            else
            {
                StartButton.gameObject.SetActive(false);
            }
            
        }
       
        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Show()
        {
            gameObject.SetActive(true);
        }

    }
}