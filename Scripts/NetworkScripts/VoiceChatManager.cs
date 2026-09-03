using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class VoiceChatManager : MonoBehaviour
{
    public static VoiceChatManager instance;

    [Header("UI")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;
    [SerializeField] private Image muteButtonImage;

    private bool _isMuted = false;
    private bool _isLoggedIn = false;
    private string _currentChannelName;

    // Tracks the in-progress login so JoinLobbyChannel can await it
    // instead of hitting _isLoggedIn == false while the task is still running
    private Task _loginTask;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (muteButton != null)
            muteButton.onClick.AddListener(ToggleMute);
    }

    // Now returns Task so callers can await it
    public async Task LoginToVivox()
    {
        if (_isLoggedIn) return;

        // If login is already in progress, just wait for it to finish
        // instead of starting a second concurrent login
        if (_loginTask != null)
        {
            await _loginTask;
            return;
        }

        _loginTask = DoLogin();
        await _loginTask;
        _loginTask = null;
    }

    private async Task DoLogin()
    {
        try
        {
            await VivoxService.Instance.InitializeAsync();
            LoginOptions options = new LoginOptions();
            options.DisplayName = LobbyManager.PendingPlayerName;
            options.EnableTTS = false;
            await VivoxService.Instance.LoginAsync(options);
            _isLoggedIn = true;
            Debug.Log("Vivox logged in.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Vivox login failed: {e.Message}");
        }
    }

    // Now awaits login before joining — no more race condition
    public async void JoinLobbyChannel(string lobbyId)
    {
        // Wait for login to fully complete before attempting to join
        await LoginToVivox();

        if (!_isLoggedIn)
        {
            Debug.LogWarning("Vivox login failed, cannot join channel.");
            return;
        }

        try
        {
            _currentChannelName = $"lobby_{lobbyId}";
            await VivoxService.Instance.JoinGroupChannelAsync(
                _currentChannelName,
                ChatCapability.AudioOnly,
                null
            );
            Debug.Log($"Joined Vivox channel: {_currentChannelName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Vivox join channel failed: {e.Message}");
        }
    }

    public async void LeaveChannel()
    {
        if (!_isLoggedIn || string.IsNullOrEmpty(_currentChannelName)) return;
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(_currentChannelName);
            _currentChannelName = null;
            Debug.Log("Left Vivox channel.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Vivox leave channel failed: {e.Message}");
        }
    }

    public async void LogoutFromVivox()
    {
        if (!_isLoggedIn) return;
        try
        {
            await VivoxService.Instance.LogoutAsync();
            _isLoggedIn = false;
            Debug.Log("Vivox logged out.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Vivox logout failed: {e.Message}");
        }
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;
        VivoxService.Instance.MuteInputDevice();
        if (muteButtonImage != null)
            muteButtonImage.sprite = _isMuted ? mutedSprite : unmutedSprite;
        Debug.Log(_isMuted ? "Muted" : "Unmuted");
    }

    private void OnDestroy()
    {
        LeaveChannel();
        LogoutFromVivox();
    }
}