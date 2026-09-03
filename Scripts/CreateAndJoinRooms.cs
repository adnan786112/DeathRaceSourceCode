using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.SceneManagement;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public TMP_InputField CreateInput;
    public TMP_InputField JoinInput;

    public static bool LimitInstantiate = false;
    //public TMP_Dropdown DropDownValue;
    //public static int TeamNo;
    void Start()
    {
        
    }
    private void Update()
    {
        //TeamNo = DropDownValue.value;
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(CreateInput.text);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(JoinInput.text);
    }
    public override void OnJoinedRoom()
    {

        PhotonNetwork.LoadLevel(3);
    }

   

}
