using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class SpawnPlayers : MonoBehaviourPunCallbacks
{
    public float MinX;
    public float MaxX;

    public float MinY;
    public float MaxY;

   // public GameObject PlayerA;
    //public GameObject PlayerB;

    //private GameObject[] PlayerInstanceA;
    //private GameObject[] PlayerInstanceB;
    //private Vector2 RandomPos;

    //public TextMeshProUGUI TeamAScore;
   // public TextMeshProUGUI TeamBScore;

    //public TextMeshProUGUI WinningTeamText;

    private PhotonView PView;

    //private float Timer;
    void Start()
    {
        
        

        PView = gameObject.GetComponent<PhotonView>();       
      
    }

    // Update is called once per frame
    void Update()
    {
        


    }
   


}
