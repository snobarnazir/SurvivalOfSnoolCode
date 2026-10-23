using UnityEngine;

public class UpperBodyAim : MonoBehaviour
{
    [Header("Upper Body Aim")]
    [SerializeField] private float maxUpRotation = 10f;
    [SerializeField] private float maxDownRotation = 10f;
    [SerializeField] private float aimStrength = 0.5f;

    private Transform spine2;
    private Camera playerCamera;

    private void Start()
    {
        // Find the camera belonging to this player.
        playerCamera =
            GetComponentInChildren<Camera>(true);

        // Find the Upper Chest / Spine2 bone.
        Transform[] allBones =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform bone in allBones)
        {
            if (bone.name == "mixamorig:Spine2")
            {
                spine2 = bone;
                break;
            }
        }

        if (spine2 == null)
        {
            Debug.LogWarning(
                "UpperBodyAim: mixamorig:Spine2 was not found."
            );
        }

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "UpperBodyAim: Player camera was not found."
            );
        }
    }

    private void LateUpdate()
    {
        // Only the local player controls their own aiming.
        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement == null ||
            !playerMovement.IsOwner)
        {
            return;
        }

        if (spine2 == null ||
            playerCamera == null)
        {
            return;
        }

        // Get camera pitch.
        float cameraPitch =
            playerCamera.transform.localEulerAngles.x;

        // Convert Unity's 0-360 rotation to -180 to +180.
        if (cameraPitch > 180f)
        {
            cameraPitch -= 360f;
        }

        // Apply only a small portion of the camera pitch.
        float upperBodyPitch =
            cameraPitch * aimStrength;

        // Clamp the upper body so it can never flip around.
        upperBodyPitch =
            Mathf.Clamp(
                upperBodyPitch,
                -maxDownRotation,
                maxUpRotation
            );

        /*
         * Animator has already applied the normal animation
         * by the time LateUpdate runs.
         *
         * We add our small pitch adjustment on top of
         * the existing animated Spine2 rotation.
         */
        spine2.localRotation =
            Quaternion.Euler(
                upperBodyPitch,
                0f,
                0f
            ) *
            spine2.localRotation;
    }
}

