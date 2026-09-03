using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class TeamSelect : MonoBehaviourPunCallbacks

{
    //public enum Team {TeamA,TeamB};

    //public static Team SelectTeam;

    public TMP_Dropdown DropDownValue;

   // public static int TeamNo;
    void Start()
    {
        //if (TeamNo == 1)
        //{

        //    SelectTeam = Team.TeamA;
        //}
        //else
        //{
        //    SelectTeam = Team.TeamB;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
       // TeamNo = DropDownValue.value;

        //Debug.Log("Team no =" + TeamNo);
        //if (TeamNo == 1)
        //{

        //    SelectTeam = Team.TeamA;
        //}
        //else
        //{
        //    SelectTeam = Team.TeamB;
        //}
    }
    //public void SelectTeamGo()
    //{
    //    PhotonNetwork.LoadLevel(3);
    //}

}

