using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    public class TestRelay : NetworkBehaviour
    {

        public static TestRelay instance;
        async void OnEnable()
        {
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () => { Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId); };
            //await AuthenticationService.Instance.SignInAnonymouslyAsync();
            //CreateRelay();
        }

        public async Task<string> CreateRelay()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);
                string JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log("Join Code Relay = " + JoinCode);
                RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                int selectedIndex = (int)CarSelectionManager.instance.SelectedCar;
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(selectedIndex);

                NetworkManager.Singleton.StartHost();
                return JoinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
            return null;
        }
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            int selectedCarIndex = 0;
            if (request.Payload != null && request.Payload.Length > 0)
            {
                selectedCarIndex = System.BitConverter.ToInt32(request.Payload, 0);
            }

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = CarPrefabRegistry.instance.GetPrefabForIndex(selectedCarIndex).GetComponent<NetworkObject>().PrefabIdHash;
        }
        public async void JoinRelay(string JoinCode)
        {
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(JoinCode);
                RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                int selectedIndex = (int)CarSelectionManager.instance.SelectedCar;
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(selectedIndex);

                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }
        private void Awake()
        {
            instance = this;
        }
    }
}
