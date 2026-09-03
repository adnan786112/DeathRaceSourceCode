using Unity.Netcode;
using UnityEngine;

public class InstantiateLobbyUI : NetworkBehaviour
{
    [SerializeField] private GameObject LobbyUI;


    public override void OnNetworkSpawn()
    {

        InstantiateLobbyUIGameObjectServerRpc();
    }
    [ServerRpc]
    private void InstantiateLobbyUIGameObjectServerRpc()
    {
        GameObject lobbyUIGameObject =  Instantiate(LobbyUI);
        lobbyUIGameObject.GetComponent<NetworkObject>().Spawn();
        //lobbyUIGameObject.transform.SetParent(transform);
      
        
    }


}
