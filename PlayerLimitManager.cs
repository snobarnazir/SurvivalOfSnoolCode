using UnityEngine;
using Unity.Netcode;

public class PlayerLimitManager : MonoBehaviour
{
    public static PlayerLimitManager Instance;

    private int maxPlayers = 4;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        }
    }

    public void SetMaxPlayers(int count)
    {
        maxPlayers = Mathf.Clamp(count, 1, 4);
        ChatUI.MultiplayerMode = true;

        Debug.Log("Maximum players set to: " + maxPlayers);
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int currentCount =
            NetworkManager.Singleton.ConnectedClientsList.Count;

        if (currentCount < maxPlayers)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
        }
        else
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "Game is full";
        }

        response.Pending = false;
    }
}