using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstructionsPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject instructionsPanelRoot;
    [SerializeField] private GameObject[] subPanels; // 0 = Controls, 1 = General Instructions

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

 

    private int currentPanelIndex = 0;

    private void Awake()
    {
    
        openButton.onClick.AddListener(OpenPanel);
        closeButton.onClick.AddListener(ClosePanel);
        nextButton.onClick.AddListener(ShowNextPanel);
        previousButton.onClick.AddListener(ShowPreviousPanel);

        instructionsPanelRoot.SetActive(false);
    }

    private void OpenPanel()
    {
        instructionsPanelRoot.SetActive(true);
        currentPanelIndex = 0;
        RefreshSubPanels();
    }

    private void ClosePanel()
    {
        instructionsPanelRoot.SetActive(false);
    }

    private void ShowNextPanel()
    {
        currentPanelIndex = (currentPanelIndex + 1) % subPanels.Length;
        RefreshSubPanels();
    }

    private void ShowPreviousPanel()
    {
        currentPanelIndex = (currentPanelIndex - 1 + subPanels.Length) % subPanels.Length;
        RefreshSubPanels();
    }

    private void RefreshSubPanels()
    {
        for (int i = 0; i < subPanels.Length; i++)
        {
            subPanels[i].SetActive(i == currentPanelIndex);
        }
    }
}
