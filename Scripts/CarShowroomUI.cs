using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CarShowroomUI : MonoBehaviour
{
    [System.Serializable]
    public struct CarStats
    {
        [Range(0, 10)] public float acceleration;
        [Range(0, 10)] public float handling;
        [Range(0, 10)] public float topSpeed;
        [Range(0, 10)] public float weight;
    }

    [System.Serializable]
    public struct CarAbilities
    {
        [Range(0, 10)] public float durability;
        [Range(0, 10)] public float gunPower;
        [Range(0, 10)] public float minigunAmmo;
        [Range(0, 10)] public float rocketAmmo;
    }

    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button getInButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject[] carShowroomModels; // order matches CarType enum
    [SerializeField] private TextMeshProUGUI carNameText;

    [Header("Stats")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Button statsButton;
    [SerializeField] private TextMeshProUGUI statsNameText;
    [SerializeField] private CarStats[] carStats; // order matches CarType enum
    [SerializeField] private Image accelerationSlider;
    [SerializeField] private Image handlingSlider;
    [SerializeField] private Image topSpeedSlider;
    [SerializeField] private Image weightSlider;
   
    [Header("Abilities")]
    [SerializeField] private GameObject AbilitiesPanel;
    [SerializeField] private Button abilitiesButton;
    [SerializeField] private TextMeshProUGUI abilitiesNameText;
    [SerializeField] private CarAbilities[] carAbilities;// order matches CarType enum
    [SerializeField] private Image durabilitySlider;
    [SerializeField] private Image gunPowerSlider;
    [SerializeField] private Image minigunAmmoSlider;
    [SerializeField] private Image rocketAmmoSlider;

    [Header("ButtonSprites")]
    [SerializeField] private Sprite buttonSelectedSprite;
    [SerializeField] private Sprite buttonDeSelectedSprite;

    [Header("SlidePanel")]
    [SerializeField] private Button panelToggleButton;
    [SerializeField] private Animator slidePanelAniamtor;
    private bool isSlidePanelVisible  =false;


    private readonly float MaxStatValue = 10f;


    private int currentIndex;
    private int carCount;

    private void Start()
    {
        carCount = System.Enum.GetValues(typeof(CarType)).Length;
        currentIndex = (int)CarSelectionManager.instance.SelectedCar;
        RefreshDisplay();

        prevButton.onClick.AddListener(() =>
        {
            currentIndex = (currentIndex - 1 + carCount) % carCount;
            RefreshDisplay();
        });
        nextButton.onClick.AddListener(() =>
        {
            currentIndex = (currentIndex + 1) % carCount;
            RefreshDisplay();
        });
        getInButton.onClick.AddListener(() =>
        {
            CarSelectionManager.instance.SelectedCar = (CarType)currentIndex;
            SceneManager.LoadScene(1);
        });
        backButton.onClick.AddListener(() => SceneManager.LoadScene(1));

        statsButton.onClick.AddListener(() =>
        {
            ButtonState(statsButton, statsPanel,abilitiesButton,AbilitiesPanel, abilitiesNameText, statsNameText);
           
        });

        abilitiesButton.onClick.AddListener(() =>
        {
            ButtonState(abilitiesButton, AbilitiesPanel,statsButton,statsPanel,statsNameText,abilitiesNameText);
        });

        panelToggleButton.onClick.AddListener(() =>
        {
            isSlidePanelVisible = !isSlidePanelVisible;
            slidePanelAniamtor.SetBool("State", isSlidePanelVisible);
            
            
        });
    }
    public void ButtonState(Button activeBtn,GameObject activePanel, Button deActiveBtn, GameObject deActivePanel,TextMeshProUGUI whiteText, TextMeshProUGUI blackText)
    {
        activeBtn.image.sprite = buttonSelectedSprite;
        activePanel.SetActive(true);
        whiteText.color = Color.white;
        deActiveBtn.image.sprite = buttonDeSelectedSprite;
        deActivePanel.SetActive(false);
        blackText.color = Color.black;

    }
    
    private void RefreshDisplay()
    {
        for (int i = 0; i < carShowroomModels.Length; i++)
        {
            if (carShowroomModels[i] != null)
                carShowroomModels[i].SetActive(i == currentIndex);

            if (carNameText != null)
                carNameText.text = ((CarType)currentIndex).ToString();
        }
            if (carStats != null && currentIndex < carStats.Length)
            {
                CarStats stats = carStats[currentIndex];
                if (accelerationSlider != null) accelerationSlider.fillAmount = stats.acceleration/MaxStatValue;
                if (handlingSlider != null) handlingSlider.fillAmount = stats.handling/MaxStatValue;
                if (topSpeedSlider != null) topSpeedSlider.fillAmount = stats.topSpeed / MaxStatValue;
                if (weightSlider != null) weightSlider.fillAmount = stats.weight / MaxStatValue;
            }
            if (carAbilities != null && currentIndex < carAbilities.Length)
            {
                CarAbilities abilities = carAbilities[currentIndex];
                if (durabilitySlider != null) durabilitySlider.fillAmount = abilities.durability/MaxStatValue;
                if (gunPowerSlider != null) gunPowerSlider.fillAmount = abilities.gunPower / MaxStatValue;
                if (minigunAmmoSlider != null) minigunAmmoSlider.fillAmount = abilities.minigunAmmo / MaxStatValue;
                if (rocketAmmoSlider != null) rocketAmmoSlider.fillAmount = abilities.rocketAmmo / MaxStatValue;
            }
        
    }
}