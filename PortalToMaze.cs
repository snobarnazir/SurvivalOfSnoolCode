
using UnityEngine;
using Unity.Netcode;

public class PortalToMaze : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || !pm.IsOwner) return;

        pm.RequestSceneChange(
            "MazeLevel1",
            "SpawnFromCity"
        );
    }
}

