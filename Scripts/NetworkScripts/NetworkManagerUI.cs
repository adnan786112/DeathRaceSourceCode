using UnityEngine;
using UnityEngine.UI;
namespace Unity.Netcode
{
    public class NetworkManagerUI : MonoBehaviour
    {
        [SerializeField] private Button ServerButton;
        [SerializeField] private Button HostButton;
        [SerializeField] private Button ClientButton;
        [SerializeField] private Transform SpawnGameObject;
        private void Awake()
        {
            ServerButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartServer();    
            });
            HostButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartHost();
            });
            ClientButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartClient();
            });
        }

        void Start()
        {
            Transform SpawnedGameObejct = Instantiate(SpawnGameObject);
            SpawnedGameObejct.GetComponent<NetworkObject>().Spawn(true);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
