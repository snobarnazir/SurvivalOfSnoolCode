
using Unity.Netcode;
using UnityEngine;

public class MouseMovement : NetworkBehaviour
{
    public float mouseSensitivity = 100f;

    public float topClamp = -20f;
    public float bottomClamp = 20f;

    private float xRotation = 0f;

    private Camera playerCamera;

    private Vector3 cameraStartingEuler;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        playerCamera =
            GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            // IMPORTANT:
            // Save the camera's existing rotation.
            // We do NOT replace it with 0,0,0.
            cameraStartingEuler =
                playerCamera.transform.localEulerAngles;
        }
        else
        {
            Debug.LogWarning(
                "MouseMovement: Main Camera not found under Player."
            );
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (ChatUI.IsChatOpen)
        {
            return;
        }

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;


        // =====================================================
        // LEFT / RIGHT
        // Only rotate the PLAYER.
        // =====================================================

        transform.Rotate(
            0f,
            mouseX,
            0f
        );


        // =====================================================
        // UP / DOWN
        // Only rotate the CAMERA.
        // =====================================================

        xRotation -= mouseY;

        xRotation =
            Mathf.Clamp(
                xRotation,
                topClamp,
                bottomClamp
            );

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation =
                Quaternion.Euler(
                    cameraStartingEuler.x + xRotation,
                    cameraStartingEuler.y,
                    cameraStartingEuler.z
                );
        }
    }
}