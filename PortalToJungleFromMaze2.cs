
using UnityEngine;
using Unity.Netcode;

public class PortalToJungleFromMaze2 : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || !pm.IsOwner) return;

        pm.RequestSceneChange(
            "Jungle",
            "SpawnFromMaze"
        );
    }
}

