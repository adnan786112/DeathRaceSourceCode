using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.Rendering.HighDefinition;
using System.Threading.Tasks;

public class UIScript : MonoBehaviour
{
    public Image SpeedRing;
    public Text SpeedText;
    public Text GearText;
    public Text LapNumberText;
    public Text TotalLapsText;
    public Text WrongWayT;
    public Text TotalCarsText;
    public Text PlayersPosition;
    public GameObject WrongWayText;
    public Text MinigunAmmoText;
    public Text RocketAmmoText;
    [SerializeField] private Text TotalMinigunAmmoText;
    [SerializeField] private Text TotalRocketAmmoText;
    public GameObject OutOfAmmoWarning;
    public GameObject OutOfRocketWarning;

    public int TotalLaps = 0;
    public int TotalCars = 0;
    public bool RaceTrack = true;

    public static UIScript instance;

    private float maxMinigunAmmo;
    private float maxRocketAmmo;

    private CarController localCarController;
    private PlayerData localPlayerData;


    [SerializeField] private Text KillText;
    [SerializeField] private Text WeaponStatusText;

    [SerializeField] private Image HealthBar;


    [SerializeField] private GameObject weaponTextCanvas;

    [Header("Finish Cinematic UI")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private GameObject finishBanner;
    [SerializeField] private Text finishPositionText;
    [SerializeField] private GameObject raceLeaderboard;
    [SerializeField] private float timeToWait  = 5f;
    [SerializeField] private Vector3 SpeedRingColor;
    [SerializeField] private Animator[] ArtificalVignetteArray;
    [SerializeField] private GameObject[] canvasGameobjectsToHide;
    [SerializeField] private Button backToLobbyButton;

    public GameObject GetLeaderboardCanvas => raceLeaderboard;
    public Animator[] GetArtificialVignette => ArtificalVignetteArray;
    public Text GetKillText => KillText;

    public Image GetHealthBar => HealthBar;

    public Text GetWeaponStatusText => WeaponStatusText;

    public GameObject GetWeaponTextCanvas() => weaponTextCanvas;
    private void Awake()
    {
        instance = this;
        if (finishBanner != null) finishBanner.SetActive(false);
        backToLobbyButton.gameObject.SetActive(false);
        backToLobbyButton.onClick.AddListener(OnClickBackToLobbyButton);
        weaponTextCanvas.SetActive(false);
      
    }

    private async void OnClickBackToLobbyButton()
    {
      
        await SceneManager.LoadSceneAsync(1);
    }
    private void OnEnable()
    {
        foreach (Animator o in ArtificalVignetteArray)
        {
            o.enabled = false;
        }
    }

    public void HideCanvas()
    {
        foreach (GameObject o in canvasGameobjectsToHide)
        {
            o.SetActive(false);
        }
    }
    private void Start()
    {
        SetStatsAtStart();
    }

    private void Update()
    {
        TryGetLocalPlayer();
        SetStatsAtUpdate();
    }

    // Tries to grab local player references once they are spawned
    private void TryGetLocalPlayer()
    {
        if (localCarController != null) return;
        if (NetworkManager.Singleton?.SpawnManager == null) return;

        var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if(localObj == null) return;     
        localCarController = localObj.GetComponent<CarController>();
        localPlayerData = localObj.GetComponent<PlayerData>();
    }

    public void SetStatsAtStart()
    {
        SpeedRing.fillAmount = 0;
        SpeedText.text = "0";
        GearText.text = "1";
        TotalLapsText.text = " / " + TotalLaps.ToString();
        WrongWayText.SetActive(false);
        SaveScript.MaxLaps = TotalLaps;
        LapNumberText.text = "0";
        TotalCarsText.text = " / " + TotalCars.ToString();

    }

    public void SetStatsAtUpdate()
    {
        
        if (localCarController != null)
        {
            float speed = SaveScript.Speed;
            float topSpeed = SaveScript.TopSpeed > 0 ? SaveScript.TopSpeed : 1f;

            SpeedRing.fillAmount = Mathf.Clamp01(speed / topSpeed);
            if (SpeedRing.fillAmount >= 0.75)
            {
                SpeedRing.color = Color.red;
            }
            else if (SpeedRing.fillAmount <= 0.35)
            {
                SpeedRing.color = new Color(SpeedRingColor.x, SpeedRingColor.y, SpeedRingColor.z);
            }
            else
            {
                SpeedRing.color = Color.yellow;
            }
            SpeedText.text = Mathf.Round(speed).ToString();
            GearText.text = (SaveScript.Gear + 1).ToString();
            WrongWayText.SetActive(SaveScript.WrongWay);
            WrongWayT.text = SaveScript.WWTextReset ? " " : "WRONG WAY!";

        }
        // Wrong way


    }

    // Ammo UI
    public void InitAmmo(float minigunMax, float rocketMax)
    {
        maxMinigunAmmo = minigunMax;
        maxRocketAmmo = rocketMax;
        TotalMinigunAmmoText.text = " / " + minigunMax.ToString();
        TotalRocketAmmoText.text = " / " + rocketMax.ToString();
        UpdateMinigunAmmoUI(minigunMax);
        if (rocketMax > 0)
        {
            UpdateRocketAmmoUI(rocketMax);
        }
    }

    public void UpdateMinigunAmmoUI(float current)
    {
       
        if (MinigunAmmoText != null)
            MinigunAmmoText.text = Mathf.CeilToInt(current).ToString();

        if (current <= 0)
        {
            if (!OutOfAmmoWarning.activeInHierarchy)
            {
                OutOfAmmoWarning.SetActive(true);
                StartCoroutine(DisableWarningScene(OutOfAmmoWarning));
            }
        }

    }

    public void UpdateRocketAmmoUI(float current)
    {
     
        if (RocketAmmoText != null)
            RocketAmmoText.text = Mathf.CeilToInt(current).ToString();

        if (current <= 0)
        {
            if (!OutOfAmmoWarning.activeInHierarchy)
            {
                OutOfRocketWarning.SetActive(true);
                StartCoroutine(DisableWarningScene(OutOfRocketWarning));
            }
        }


    }
    private IEnumerator DisableWarningScene(GameObject Sign)
    {
        yield return new WaitForSeconds(1f);
        if (Sign.activeInHierarchy)
        {
            Sign.SetActive(false);
        }
    }
    public void ShowFinishBanner(int position)
    {
        if (finishBanner != null)
        {
            finishBanner.SetActive(true);
            if (finishPositionText != null)
            {
                Debug.Log(finishPositionText);
                finishPositionText.text = GetPositionString(position);
                StartCoroutine(LoadLobbyScene());

                //TriggerFinishFadeAndLeaderboard();
            }
        }
    }
    private IEnumerator LoadLobbyScene()
    {
        yield return new WaitForSeconds(timeToWait);
        backToLobbyButton.gameObject.SetActive(true);   

    }

    private string GetPositionString(int position)
    {
        Debug.Log(position);
        return position switch
        {
            1 => "1ST",
            2 => "2ND",
            3 => "3RD",
            _ => position + "TH"
        };

    }
  

    #region UnusedCode

    public void TriggerFinishFadeAndLeaderboard()
    {
        StartCoroutine(FadeAndShowLeaderboard());
    }

    private IEnumerator FadeAndShowLeaderboard()
    {
        yield return new WaitForSeconds(timeToWait);
        yield return StartCoroutine(FadeOut());

        if (finishBanner != null) finishBanner.SetActive(false);
 


        yield return StartCoroutine(FadeIn());
    }

    public IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        Color c = Color.black;
        c.a = 0f;
        fadeImage.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        Color c = Color.black;
        c.a = 1f;
        fadeImage.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        fadeImage.color = c;

    }

    #endregion



}