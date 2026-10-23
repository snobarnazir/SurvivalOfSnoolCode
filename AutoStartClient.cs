using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class AutoStartClient : MonoBehaviour
{
    [Header("Retry Settings")]
    public float retryDelay = 1.0f;
    public int maxRetries = 10;

    private void Start()
    {
#if !UNITY_EDITOR
        StartCoroutine(StartClientWithRetry());
#endif
    }

    private IEnumerator StartClientWithRetry()
    {


        Debug.Log("AUTO CLIENT: Waiting to join Host...");
        ChatUI.MultiplayerMode = true;

        int tries = 0;

        while (tries < maxRetries)
        {
            tries++;

            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("AUTO CLIENT: NetworkManager is null. Retrying...");
                yield return new WaitForSeconds(retryDelay);
                continue;
            }

            // Already connected
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("AUTO CLIENT: Already connected.");
                yield break;
            }

            // Do not start client if this build somehow became host
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.Log("AUTO CLIENT: This instance is Host/Server. Stopping auto-client.");
                yield break;
            }

            bool started = NetworkManager.Singleton.StartClient();
            Debug.Log($"AUTO CLIENT: StartClient try {tries} = {started}");

            yield return new WaitForSeconds(retryDelay);

            if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsClient)
            {
                Debug.Log("AUTO CLIENT: Successfully connected to Host.");
                yield break;
            }
        }

        Debug.LogError("AUTO CLIENT: Failed to connect after all retries. Is the Host running?");
    }
}

