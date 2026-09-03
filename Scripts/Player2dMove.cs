using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Player2dMove : MonoBehaviourPunCallbacks
{
    public float Speed;

    private float XInput;
    private float YInput;

    private float CameraHeight;
    private float CameraWidth;

    private Camera MainCam;
    private Bounds ObjectBounds;

    private PhotonView View;

    public GameObject Bullets;
    private GameObject Bullet1;
    private GameObject BulletToDisplay;
    
    public int TotalBullets;
    public int BulletSpeed;
    public float StartingHealth;
    private float CurrentHealth;
    public GameObject BulletSpawnPos;
    public List<GameObject> BulletPool;

    public Slider HealthSlider;

    public RectTransform HealthCanvasRect;
    public float RotateSpeed;
    public GameObject HealthCanvas;
    public GameObject CanvasPos;

    public static int ScoreTeamA =0;
    public static int ScoreTeamB =0;

    public static Vector3 BulletADirection;
    public static Vector3 BulletBDirection;

    private Rigidbody2D BulletRb;

    void Start()
    {
        MainCam = Camera.main;

        CameraWidth = MainCam.orthographicSize * 1.8f;
        CameraHeight = CameraWidth * MainCam.aspect / 3.2f;

        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        ObjectBounds = sp.bounds;
        View = gameObject.GetComponent<PhotonView>();
        CurrentHealth = StartingHealth;
        if(View.IsMine)
        {
            //InstantiateBulletPool();
            View.RPC("InstantiateBulletPool", RpcTarget.All);
        } 
    }

    // Update is called once per frame
    void Update()
    {
        XInput = Input.GetAxis("Horizontal") * Time.deltaTime * Speed;
        YInput = Input.GetAxis("Vertical") * Time.deltaTime * Speed;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, 100);
        //if (XInput <= CameraBoundsX && YInput <= CameraBoundsY) ;
        //{
        //    gameObject.transform.Translate(XInput, YInput, 0);

        //}
        float minX = MainCam.transform.position.x - CameraWidth + ObjectBounds.extents.x;
        float maxX = MainCam.transform.position.x + CameraWidth - ObjectBounds.extents.x;

        float minY = MainCam.transform.position.y - CameraHeight + ObjectBounds.extents.y;
        float maxY = MainCam.transform.position.y + CameraHeight - ObjectBounds.extents.y;

        //float deltaX = XInput * 
        //float ClampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        //float ClampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        float TargetX = Mathf.Clamp(transform.position.x + XInput, minX, maxX);
        float targetY = Mathf.Clamp(transform.position.y + YInput, minY, maxY);

        if (View.IsMine)
        {
            transform.position = new Vector3(TargetX, targetY, transform.position.z);

            if(Input.GetKey(KeyCode.R))
            {
                transform.Rotate(0, 0, RotateSpeed * Time.deltaTime);
            }
            //if(gameObject.CompareTag("TeamA"))
            //{
            //    BulletADirection = transform.forward;
            //}
            if(Input.GetMouseButtonDown(0))
            {
                Bullet1 = GetFromBulletPool();
               
                if (Bullet1 != null)
                {
                    Bullet1.transform.position = BulletSpawnPos.transform.position;
                    Bullet1.transform.rotation = Quaternion.identity;
                    Bullet1.SetActive(true);

                   
                   
                   
                    Debug.Log("condition Working");
                    Vector2 forwardDirection =  transform.right;
                    Bullet1.GetComponent<Rigidbody2D>().AddForce(forwardDirection *BulletSpeed, ForceMode2D.Impulse);
                    BulletRb = Bullet1.GetComponent<Rigidbody2D>();
                    BulletRb.AddForce(forwardDirection * BulletSpeed * Time.deltaTime, ForceMode2D.Impulse); ;
                }
            }
            Debug.Log("Current Health = " + CurrentHealth);

            //HealthCanvas.transform.position = CanvasPos.transform.position;
            //CurrentHealth -= 10 * Time.deltaTime;
           
        }
        

        HealthSlider.value = CurrentHealth;
        Vector2 ScreenPos = MainCam.WorldToScreenPoint(CanvasPos.transform.position);
        HealthCanvasRect.position = ScreenPos;
        HealthCanvasRect.rotation = gameObject.transform.rotation;
        if (CurrentHealth <= 0)
        {
            View.RPC("Die", RpcTarget.All);
        }
    }
    [PunRPC]
    public void InstantiateBulletPool()
    {
        for (int i = 0; i < TotalBullets; i++)
        {
            Bullet1 = Instantiate(Bullets, transform.position, Quaternion.identity);
            Bullet1.SetActive(false);
            BulletPool.Add(Bullet1);
        }
    }
    public GameObject GetFromBulletPool()
    {
        for(int j=0;j<BulletPool.Count;j++)
        {
            if(!BulletPool[j].activeInHierarchy)
            {
                return BulletPool[j];
            }
        }
        return null;
    }
    [PunRPC]
    public void DamageHealth(float Damage)
    {
        
            
        
            View.RPC("DamagingEffect", RpcTarget.All);
        

    }
    [PunRPC]
    public void DamagingEffect()
    {
        CurrentHealth -= 10;
    }
   
    [PunRPC]
    public void Die()
    {
        Debug.Log("Die");
        if (gameObject.CompareTag("TeamA"))
        {
            ScoreTeamB++;
        }
        if (gameObject.CompareTag("TeamB"))
        {
            ScoreTeamA++;
        }
        gameObject.SetActive(false);
    }
}
