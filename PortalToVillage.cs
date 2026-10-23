
using UnityEngine;
using Unity.Netcode;

public class PortalToVillage : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || !pm.IsOwner) return;

        pm.RequestSceneChange(
            "Village",
            "SpawnFromJungle2"
        );
    }
}

