using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BulletScript : MonoBehaviourPunCallbacks
{
    public float BulletSpeed;
    private Rigidbody2D rb;
    private GameObject[] PlayerInstanceA;
    private GameObject[] PlayerInstanceB;

    private PhotonView Pview;
    //private float Timer
    void Start()
    {
        Pview = gameObject.GetComponent<PhotonView>();
    }
    private void OnEnable()
    {
       // PlayerInstanceA = GameObject.FindGameObjectsWithTag("TeamA");

        //PlayerInstanceB = GameObject.FindGameObjectsWithTag("TeamB");
        //if (gameObject.CompareTag("BulletA"))
        //{
           //GetComponent<Rigidbody2D>().AddForce(this.transform.forward *BulletSpeed * Time.deltaTime, ForceMode2D.Impulse);
        //}
       
    }

    // Update is called once per frame
    void Update()
    {
        if(!gameObject.GetComponent<SpriteRenderer>().isVisible)
        {
            gameObject.SetActive(false);
        }
        //PlayerInstanceA = GameObject.FindGameObjectsWithTag("TeamA");

        //PlayerInstanceB = GameObject.FindGameObjectsWithTag("TeamB");

    }
    //private void OnTriggerEnter2D()
    //{
    //    //if(collision.gameObject.CompareTag("TeamA"))
    //    Debug.Log(collision.gameObject.name);
    //    if (collision.gameObject.GetComponent<Player2dMove>() != null)
    //    {
    //        collision.gameObject.GetComponent<Player2dMove>().DamageHealth(10);

    //    }
    //    gameObject.SetActive(false);
    //}
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<Player2dMove>() != null)
        {
           
            
            collision.gameObject.GetComponent<Player2dMove>().DamageHealth(10);
            gameObject.SetActive(false);

        }
       
    }

}
