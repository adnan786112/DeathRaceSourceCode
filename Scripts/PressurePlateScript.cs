using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using System.Threading.Tasks;
using System.Collections;
using System;

public class PressurePlateScript : NetworkBehaviour
{
    public enum PressurePlatetype
    { Sword,Shield,DeathHead }
    private Material Mat;
    [SerializeField] private Canvas pressurePlateDispalyCanvas;
    [SerializeField] private Color color;
    [SerializeField] private PressurePlatetype plateType;
    private NetworkVariable<bool> IsTaken = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    private Action<bool> OnAllPressurePlatesActivated;
    private void Awake()
    {
        Mat = gameObject.GetComponent<MeshRenderer>().material;
        // Enable emission keyword (required or it won't show)
        Mat.EnableKeyword("_EMISSION");
        Mat.SetFloat("_EmissiveIntensity", 0f);
        HDMaterial.ValidateMaterial(Mat);
        pressurePlateDispalyCanvas.gameObject.SetActive(false);
        OnAllPressurePlatesActivated += ActivatePressurePlates;

    }
    private void OnEnable()
    {
        IsTaken.OnValueChanged += PressurePlateTaken;
    }

    
    private void PressurePlateTaken(bool previous, bool current)
    {
        Debug.Log(current);
        if(current == true)
        {
            Debug.Log("Taken");
            Mat.SetFloat("_EmissiveIntensity", 0f);
            HDMaterial.ValidateMaterial(Mat);
            pressurePlateDispalyCanvas.gameObject.SetActive(false);
            this.gameObject.GetComponent<BoxCollider>().enabled = false;

        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void ActivatePressurePlatesRpc(NetworkObjectReference TriggerCarRef, bool isAiCar, RpcParams rpcParams = default)
    {
        if (TriggerCarRef.TryGet(out NetworkObject TriggerCar))
        {

            //Debug.Log("yes");
            //Debug.Log($"<color=red>{TriggerCar.name}</color>");
            //Debug.Log(rpcParams.Send.Target);
            OnAllPressurePlatesActivated.Invoke(isAiCar);
        }
    }

    private void ActivatePressurePlates(bool isAicar)
    {
        if (!IsTaken.Value)
        {
            //Debug.Log("Activated");
            gameObject.GetComponent<BoxCollider>().enabled = true;
            if (!isAicar)
            {
                Mat = gameObject.GetComponent<MeshRenderer>().material;
                Mat.SetFloat("_EmissiveIntensity", 9000f);
                HDMaterial.ValidateMaterial(Mat);
                pressurePlateDispalyCanvas.gameObject.SetActive(true);

            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != null && gameObject != null && other.gameObject.GetComponentInParent<PlayerData>() != null)
        {
          
            EnableShootingOnVehiclerRpc(other.gameObject.GetComponentInParent<NetworkObject>());
                       
        }
    }

    //private void CommonResponse(NetworkObjectReference TriggerCarRef)
    //{
    //}

  

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void IstakenValueUpdateServerRpc()
    {
        IsTaken.Value = true;
        
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastPlayerWeaponStatusRpc(NetworkObjectReference PlayerRef,PressurePlatetype pressurePlatetype)
    {
       //Debug.Log("Announce");
        PlayerRef.TryGet(out NetworkObject Player);
        UIScript.instance.GetWeaponStatusText.gameObject.SetActive(true);
        if (PlayerRef.NetworkObjectId == NetworkManager.LocalClient.PlayerObject.NetworkObjectId)
        {
            UIScript.instance.GetWeaponStatusText.text = $"<color=white> You </color> Got a <color=green>{pressurePlatetype.ToString()}</color>";
        }
        else
        {
            UIScript.instance.GetWeaponStatusText.text = $"<color=red>{Player.name} </color> Got  a <color=green>{pressurePlatetype.ToString()}</color>";
        }

        StartCoroutine(DisablekillText());
    

    }
    private IEnumerator DisablekillText()
    {
        yield return new WaitForSeconds(2f);
        UIScript.instance.GetWeaponStatusText.gameObject.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void EnableShootingOnVehiclerRpc(NetworkObjectReference TriggerCarRef)
    {
        //CommonResponse(TriggerCarRef);

        //Debug.Log("inside");
        if (TriggerCarRef.TryGet(out NetworkObject TriggerCar))
        {
            CarUserControl carUserControl = TriggerCar.GetComponent<CarUserControl>();
            if (TriggerCar.GetComponent<PlayerData>().CarLapPosition.Value > 1)
            {
                //Debug.Log("LapValue2");
                switch (plateType)
                {
                    case PressurePlatetype.Sword:
                        if (!carUserControl.CanShoot)
                        {
                            //Debug.Log("Shooter" + TriggerCar.name);
                            carUserControl.CanShoot = true;
                          
                            if(TriggerCarRef.NetworkObjectId == NetworkManager.LocalClient.PlayerObject.NetworkObjectId)
                            {
                                PressurePlateMasterScript.DispalyCrosshairCamvas.Invoke();
                                UIScript.instance.GetWeaponTextCanvas().gameObject.SetActive(true);
                            }
                            BroadcastPlayerWeaponStatusRpc(TriggerCarRef, plateType);
                            if (IsOwner)
                            {
                                IstakenValueUpdateServerRpc();
                            }
                        }
                        break;
                    case PressurePlatetype.Shield:
                        if (!carUserControl.CanDefend)
                        {
                            //Debug.Log("Defender" + TriggerCar.name);
                            carUserControl.CanDefend = true;
                            BroadcastPlayerWeaponStatusRpc(TriggerCarRef, plateType);
                            if (IsOwner)
                            {
                                IstakenValueUpdateServerRpc();
                            }
                        }
                        break;
                }


              



            }

        }
    }
}
