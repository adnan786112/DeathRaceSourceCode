using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : NetworkBehaviour
{
    public void StartNewScene(string sceneName)
    {
       
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        
    }
}
