using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button showroomButton;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI selectedCarNameText;
    [SerializeField] private GameObject[] carPreviewModels; // order matches CarType enum

    private void Start()
    {
        if (playerNameText != null)
            playerNameText.text = LobbyManager.PendingPlayerName;

        startButton.onClick.AddListener(() => SceneManager.LoadScene(3));
        showroomButton.onClick.AddListener(() => SceneManager.LoadScene(2));

        RefreshCarPreview();
    }

    private void OnEnable() => RefreshCarPreview();

    private void RefreshCarPreview()
    {
        if (carPreviewModels == null || carPreviewModels.Length == 0) return;

        int selected = (int)CarSelectionManager.instance.SelectedCar;

        for (int i = 0; i < carPreviewModels.Length; i++)
        {
            if (carPreviewModels[i] != null)
                carPreviewModels[i].SetActive(i == selected);
        }

        if (selectedCarNameText != null)
            selectedCarNameText.text = CarSelectionManager.instance.SelectedCar.ToString();
    }
}