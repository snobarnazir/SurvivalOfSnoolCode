

using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkTestUI : MonoBehaviour
{
    public void HostGame()
    {
        bool started = NetworkManager.Singleton.StartHost();

        if (started)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("City", LoadSceneMode.Additive);
        }
    }

    public void JoinGame()
    {
        NetworkManager.Singleton.StartClient();
    }
}