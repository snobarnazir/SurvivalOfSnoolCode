using UnityEngine;
using Unity.Netcode;

public class SaveNotificationTrigger : MonoBehaviour
{
    [Header("Notification Settings")]
    [TextArea(1, 2)]
    public string saveMessage = "WAVE CLEARED. GAME PROGRESS SAVED.";
    public float displayDuration = 3.0f;

    /// <summary>
    /// Call this method when a wave finishes.
    /// Safe to trigger anywhere — automatically ignores multiplayer sessions.
    /// </summary>
    public void TriggerWaveSaveNotification()
    {
        // Guard: Do nothing if running inside an active multiplayer network session
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            return;
        }

        // Single-player execution: locate local player's BriefingBox UI
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            BriefingBox briefing = player.GetComponentInChildren<BriefingBox>(true);
            if (briefing != null)
            {
                briefing.PlayCustomMessage(saveMessage, displayDuration);
            }
        }
    }
}