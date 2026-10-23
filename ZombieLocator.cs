using UnityEngine;
using Unity.Netcode;

public class ZombieLocator : NetworkBehaviour
{
    [Header("Hold this key to show zombie markers")]
    public KeyCode locateKey = KeyCode.Tab;

    private void Update()
    {
        // Only local player
        if (!IsOwner) return;

        bool show = Input.GetKey(locateKey);
        UpdateMarkers(show);
    }

    private void UpdateMarkers(bool show)
    {
        ZombieMarker[] markers = FindObjectsByType<ZombieMarker>(FindObjectsSortMode.None);

        foreach (ZombieMarker marker in markers)
        {
            if (marker == null) continue;

            Enemy enemy = marker.GetComponent<Enemy>();
            if (enemy == null || enemy.isDead)
            {
                marker.ShowMarker(false);
                continue;
            }

            marker.ShowMarker(show);
        }
    }

    private void OnDisable()
    {
        // Hide all when this player object disables
        UpdateMarkers(false);
    }
}