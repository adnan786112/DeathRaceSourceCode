using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode.Components;
using System;

public class StartingLightsScript : NetworkBehaviour
{
    public GameObject RLightOff;
    public GameObject RLightOn;
    public GameObject ALightOff;
    public GameObject ALightOn;
    public GameObject GLightOff;
    public GameObject GLightOn;
    public AudioSource Sound1;
    public AudioSource Sound2;
    public GameObject Text3;
    public GameObject Text2;
    public GameObject Text1;
    public GameObject TextGo;

    public static bool ChangedStartingLightCoroutine;

    void Start()
    {
      
        TextGo.SetActive(false);
       
    }
    
    private void Update()
    {
        if (LobbyManager.SpawnedALLCars && ChangedStartingLightCoroutine == false)
        {

            StartCoroutineFunctionClientRpc();

            ChangedStartingLightCoroutine = true;
        }
       
    }
    [ClientRpc]
    public void StartCoroutineFunctionClientRpc()
    {
        if (LobbyManager.SpawnedAllCarsChanged == false)
        {
            StartCoroutine(StartingLights());
            LobbyManager.SpawnedAllCarsChanged = true;
        } 
    }
    
    IEnumerator StartingLights()
    {
       
        yield return new WaitForSeconds(5f);
        RLightOff.SetActive(false);
        RLightOn.SetActive(true);
        Text3.SetActive(true);
        Sound1.Play();
        yield return new WaitForSeconds(3f);
        RLightOff.SetActive(true);
        RLightOn.SetActive(false);
        Sound1.Play();
        ALightOff.SetActive(false);
        ALightOn.SetActive(true);
        Text3.SetActive(false);
        Text2.SetActive(true);
        yield return new WaitForSeconds(3f);
        ALightOff.SetActive(true);
        ALightOn.SetActive(false);
        Sound2.Play();
        GLightOff.SetActive(false);
        GLightOn.SetActive(true);
        Text2.SetActive(false);
        Text1.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        SaveScript.RaceStart = true;
        SaveScript.RaceStartEvent.Invoke();
        TextGo.SetActive(true);
        GLightOff.SetActive(true);
        GLightOn.SetActive(false);
        Text1.SetActive(false);
        TextGo.SetActive(true);
        CarUserControl.FreezeCarAtStartingLight = false;
        yield return new WaitForSeconds(2f);
        TextGo.SetActive(false);
      
       
            
        
        
    }


}
