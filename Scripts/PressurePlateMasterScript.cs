using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlateMasterScript : MonoBehaviour
{
    [SerializeField] private List<PressurePlateScript> PressurePlates;
    public static Action DispalyCrosshairCamvas;
    public static PressurePlateMasterScript instance;
    private void Awake()
    {
      instance = this; 
    }
    public List<PressurePlateScript>  GetPressurePlateScripts => PressurePlates;



}
