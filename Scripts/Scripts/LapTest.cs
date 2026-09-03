//using System.Collections;

//using System.Collections.Generic;

//using UnityEngine;



//public class LapTest : MonoBehaviour

//{

//    private void OnTriggerEnter(Collider other)

//    {

//        //SaveScript.WwTextReset = true;

//        StartCoroutine(WrongWayReset());

//        if (SaveScript.RaceOver == false)

//        {

//            if (other.gameObject.CompareTag("Player"))

//            {

//                if (SaveScript.HalfWayActivated == true)

//                {

//                    SaveScript.HalfWayActivated = false;



//                    SaveScript.LastLapM = SaveScript.LapTimeMinutes;

//                    SaveScript.LastLapS = SaveScript.LapTimeSeconds;

//                    AssignCarNames.instance.LapNumber.Value++;

//                    SaveScript.LapChange = true;



//                    if (AssignCarNames.instance.LapNumber.Value == 2)

//                    {

//                        SaveScript.BestLapTimeM = SaveScript.LastLapM;

//                        SaveScript.BestLapTimeS = SaveScript.LastLapS;

//                        SaveScript.NewRecord = true;

//                    }

//                    SaveScript.CheckPointPass1 = false;

//                    SaveScript.CheckPointPass2 = false;

//                    SaveScript.LastCheckPoint1 = SaveScript.ThisCheckPoint1;

//                    SaveScript.LastCheckPoint2 = SaveScript.ThisCheckPoint2;



//                }

//            }

//        }

//    }

//    IEnumerator WrongWayReset()

//    {

//        yield return new WaitForSeconds(1.5f);

//       // SaveScript.WwTextReset = false;

//    }

//}
