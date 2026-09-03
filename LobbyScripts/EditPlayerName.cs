using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EditPlayerName : MonoBehaviour
{

    public static EditPlayerName Instance { get; private set; }
    public event EventHandler OnNameChanged;

    private enum ConfirmButtonState { Confirm, Login }

    [Header("Edit Mode")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;

    [Header("Display Mode")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Button editButton;

    private const string ValidCharacters = "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ ._,-";
    private const int CharacterLimit = 20;

    private string playerName = "Enter Your Name";
    private ConfirmButtonState confirmButtonState = ConfirmButtonState.Confirm;
    public static bool PlayerEditedName = false;

    private void Awake()
    {
        PlayerEditedName = false;
        Instance = this;

        inputField.characterLimit = CharacterLimit;
        inputField.onValidateInput = (string text, int charIndex, char addedChar) => {
            return ValidateChar(addedChar);
        };

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        GetComponent<Button>().onClick.AddListener(StartEditing);
        editButton.onClick.AddListener(StartEditing);

        ShowInitialState();
    }

    private void ShowInitialState()
    {
        backgroundImage.gameObject.SetActive(true);
        inputField.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        playerNameText.gameObject.SetActive(true);

        editButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        OnNameChanged += EditPlayerName_OnNameChanged;
    }

    private char ValidateChar(char addedChar)
    {
        return ValidCharacters.IndexOf(addedChar) != -1 ? addedChar : '\0';
    }

    private void StartEditing()
    {
        if (inputField.gameObject.activeSelf) return; // already editing, ignore repeat clicks

        inputField.text = playerName == "Enter Your Name" ? "" : playerName;
        confirmButtonState = ConfirmButtonState.Confirm;
        confirmButtonText.text = "OK";
        ShowEditMode();
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void OnConfirmButtonClicked()
    {
        if (confirmButtonState == ConfirmButtonState.Confirm)
        {
            string newName = inputField.text;
            if (string.IsNullOrWhiteSpace(newName)) newName = playerName;
            playerName = newName;

            ShowConfirmedState();

            confirmButtonState = ConfirmButtonState.Login;
            confirmButtonText.text = "Login";

            OnNameChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            LoginAndLoadNextScene();
        }
    }

    private async void LoginAndLoadNextScene()
    {
        LobbyManager.Instance.Authenticate(GetPlayerName());
        await SceneManager.LoadSceneAsync(1); // Main Menu scene
    }

    private void ShowEditMode()
    {
        backgroundImage.gameObject.SetActive(true);
        inputField.gameObject.SetActive(true);
        confirmButton.gameObject.SetActive(true);

        playerNameText.gameObject.SetActive(false);
        editButton.gameObject.SetActive(false);
    }

    private void ShowConfirmedState()
    {
        backgroundImage.gameObject.SetActive(false);
        inputField.gameObject.SetActive(false);

        playerNameText.text = "Welcome " + playerName;
        playerNameText.gameObject.SetActive(true);
        editButton.gameObject.SetActive(true);
    }

    private void EditPlayerName_OnNameChanged(object sender, EventArgs e)
    {
        LobbyManager.Instance.UpdatePlayerName(GetPlayerName());
        PlayerEditedName = true;
    }

    public string GetPlayerName()
    {
        return playerName;
    }
}