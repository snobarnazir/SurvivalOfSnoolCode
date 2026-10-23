using Unity.Netcode;
using UnityEngine;

public class PlayerChat : NetworkBehaviour
{
    // Fixed names by clientId
    private static readonly string[] PlayerNames =
    {
        "Snobar",   // clientId 0 (host)
        "Noor",     // clientId 1
        "Laraib",   // clientId 2
        "Rabonas"   // clientId 3
    };

    // Colors for each name
    private static readonly Color[] PlayerColors =
    {
        new Color(1f, 0.85f, 0.2f),  // Snobar  - gold
        new Color(0.3f, 0.8f, 1f),   // Noor    - cyan
        new Color(1f, 0.5f, 0.8f),   // Laraib  - pink
        new Color(0.4f, 1f, 0.4f)    // Rabonas - green
    };

    public static string GetName(ulong clientId)
    {
        int index = (int)(clientId % (ulong)PlayerNames.Length);
        return PlayerNames[index];
    }

    public static Color GetColor(ulong clientId)
    {
        int index = (int)(clientId % (ulong)PlayerColors.Length);
        return PlayerColors[index];
    }

    // Called by the local ChatUI when the player presses Enter
    public void SendChatMessage(string message)
    {
        if (!IsOwner) return;
        if (string.IsNullOrEmpty(message)) return;

        SendChatMessageServerRpc(message);
    }

    [ServerRpc]
    private void SendChatMessageServerRpc(
        string message,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Broadcast to everyone (including sender)
        BroadcastChatMessageClientRpc(senderId, message);
    }

    [ClientRpc]
    private void BroadcastChatMessageClientRpc(
        ulong senderId,
        string message)
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClient == null) return;

        var localPlayer =
            NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayer == null) return;

        ChatUI localChat =
            localPlayer.GetComponentInChildren<ChatUI>(true);

        if (localChat != null)
        {
            localChat.AddMessage(senderId, message);
        }
    }
}