using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class SaveScript : MonoBehaviour
{
    public static bool BrakeSlide;
    public static bool WrongWay = false;
    public static bool WWTextReset = false;
    public static bool RaceStart = false;
    public static int MaxLaps;
    public static bool RaceOver = false;
    public static bool Joypad = false;
    public static GameObject PenaltyText;
    public static SaveScript instance;
    public static Action RaceStartEvent;
    public static bool IsReversing = false;
    public static float Speed;
    public static float TopSpeed;
    public static float Gear;
    private void Awake()
    {
        instance = this;
    }
  

}