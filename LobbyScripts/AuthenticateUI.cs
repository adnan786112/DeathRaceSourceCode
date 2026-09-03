using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthenticateUI : MonoBehaviour
{
    [SerializeField] private Button authenticateButton;

    private void Awake()
    {
        authenticateButton.gameObject.SetActive(false);
        authenticateButton.onClick.AddListener(async () =>
        {
            LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName());
           
            await SceneManager.LoadSceneAsync(1); // Main Menu scene
            //Hide();
        });
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        if (EditPlayerName.PlayerEditedName)
            authenticateButton.gameObject.SetActive(true);
    }
}